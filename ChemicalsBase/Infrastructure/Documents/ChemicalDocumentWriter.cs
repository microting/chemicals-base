/*
The MIT License (MIT)

Copyright (c) 2007 - 2026 Microting A/S

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ChemicalsBase.Infrastructure.Data.Entities;
using ChemicalsBase.Infrastructure.Data.Enums;
using Microsoft.EntityFrameworkCore;
using WorkflowStates = ChemicalsBase.Infrastructure.Constants.Constants.WorkflowStates;

namespace ChemicalsBase.Infrastructure.Documents;

public enum WriteOutcome
{
    Created,
    AlreadyKnown,
    DryRun,
}

/// <summary>
/// <see cref="Document"/> is null only for a dry run of an unknown document. An <see cref="WriteOutcome.AlreadyKnown"/>
/// document can still be soft-deleted (a hands-off row that only a staff path revives): check
/// <see cref="IsRemoved"/> before treating it as live.
/// </summary>
public sealed record WriteResult(WriteOutcome Outcome, ChemicalDocument? Document, string ContentMd5)
{
    public bool IsRemoved => Document?.WorkflowState == WorkflowStates.Removed;
}

/// <summary>The md5 matches a stored document but the SHA-256 does not: different bytes, never merged.</summary>
public sealed class ChemicalDocumentHashCollisionException : InvalidOperationException
{
    public ChemicalDocumentHashCollisionException(int documentId, string md5)
        : base($"ChemicalDocument {documentId} has md5 {md5} but a different SHA-256 (hash collision).")
    {
    }

    /// <summary>The blob under the md5 key holds other bytes, and no row says so.</summary>
    public ChemicalDocumentHashCollisionException(string md5)
        : base($"The blob {DocumentHashes.BlobKey(md5)} holds bytes with a different SHA-256 (hash collision).")
    {
    }
}

/// <summary>
/// The single write path for chemical documents (labels, SDS, minor-use instructions): BMD label import,
/// SDS harvester, staff uploads and the producer-upload inbox. Dedupes on (ChemicalId, Type, ContentMd5),
/// stores each blob once under "&lt;md5&gt;.pdf", and writes through PnBase.Create/Update so every change has a
/// version row.
/// <para>
/// Nothing goes live automatically: <see cref="ChemicalDocumentStatus.Active"/> needs a named reviewer, and
/// automation never revives a soft-deleted hands-off (Staff or StaffLocked) row. A known document keeps its Status,
/// Source and StaffLocked, so a staff rejection is sticky.
/// </para>
/// <para>
/// Input is validated before the DbContext is touched, and a lost insert race is detached, so a refused write never
/// leaves the shared context unusable. PnBase.Create/Update save every pending change in the context: callers must
/// not leave unrelated tracked edits pending. Pass the tracked instance of a document; attaching an untracked copy
/// of an already tracked row throws.
/// </para>
/// </summary>
public sealed class ChemicalDocumentWriter(ChemicalsDbContext db, IChemicalBlobStore blobs, TimeProvider clock)
{
    public const long MaxPdfBytes = 20L * 1024 * 1024;

    private static ReadOnlySpan<byte> PdfMagic => "%PDF-"u8;

    /// <summary>[StringLength] of every ChemicalDocument column, so the checks cannot drift from the schema.</summary>
    private static readonly Dictionary<string, int> MaxLengths = typeof(ChemicalDocument).GetProperties()
        .Select(p => (p.Name, Max: p.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength))
        .Where(c => c.Max != null)
        .ToDictionary(c => c.Name, c => c.Max!.Value);

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<WriteResult> WriteAsync(NewChemicalDocument doc, bool dryRun = false)
    {
        ArgumentNullException.ThrowIfNull(doc);
        EnsurePdf(doc.Pdf);
        EnsureFits(doc);
        Demand(doc.Reviewer, row: null, doc.Status);

        var md5 = DocumentHashes.Md5Hex(doc.Pdf);
        var sha256 = DocumentHashes.Sha256Hex(doc.Pdf);
        var existing = await FindAsync(doc, md5);
        if (existing != null)
        {
            EnsureSameContent(existing, sha256);
        }

        // Blob keys are global (md5 only), so other bytes under the same md5 for any chemical or type collide too.
        var knownContent = await EnsureNoOtherContentAsync(md5, sha256);

        if (dryRun)
        {
            return new WriteResult(WriteOutcome.DryRun, existing, md5);
        }

        await EnsureBlobAsync(doc.Pdf, md5, sha256, knownContent);

        if (existing != null)
        {
            return await KnownAsync(existing, doc, md5);
        }

        var now = Now;
        var reviewed = doc.Reviewer != null && doc.Status != ChemicalDocumentStatus.PendingReview;
        var created = new ChemicalDocument
        {
            ChemicalId = doc.ChemicalId,
            Type = doc.Type,
            Source = doc.Source,
            Status = doc.Status,
            Language = SdsLanguages.Normalize(doc.Language),
            ContentMd5 = md5,
            ContentSha256 = sha256,
            SizeBytes = doc.Pdf.LongLength,
            SourceUrl = doc.SourceUrl,
            SourceFileId = doc.SourceFileId,
            SourceFileName = doc.SourceFileName,
            SourcePackaging = doc.SourcePackaging,
            SourceProductId = doc.SourceProductId,
            SourceAdapter = doc.SourceAdapter,
            SourceRevisionDate = doc.SourceRevisionDate,
            SourceRevision = doc.SourceRevision,
            RetrievedAt = now,
            LastSeenAt = now,
            ValidationScore = doc.ValidationScore,
            ValidationNotes = doc.ValidationNotes,
            StaffLocked = reviewed,
            ReviewedBy = reviewed ? doc.Reviewer : null,
            ReviewedAt = reviewed ? now : null,
        };

        try
        {
            await created.Create(db);
        }
        catch (DbUpdateException)
        {
            // Detach everything that failed, or every later SaveChanges on this context retries it: the row (when its
            // insert failed) and the version row PnBase.Create adds after the row saved.
            var rowSaved = created.Id != 0;
            db.Entry(created).State = EntityState.Detached;
            foreach (var version in db.ChangeTracker.Entries<ChemicalDocumentVersion>()
                         .Where(e => e.State == EntityState.Added)
                         .ToList())
            {
                version.State = EntityState.Detached;
            }

            if (rowSaved)
            {
                // The row is ours and committed; only its version row failed (a Galera certification or deadlock
                // error). Not a race, so not AlreadyKnown.
                throw;
            }

            // Most likely a concurrent writer inserted the same (ChemicalId, Type, md5) after our lookup.
            var winner = await FindAsync(doc, md5);
            if (winner == null)
            {
                throw;
            }

            EnsureSameContent(winner, sha256);
            return await KnownAsync(winner, doc, md5);
        }

        return new WriteResult(WriteOutcome.Created, created, md5);
    }

    /// <summary>
    /// Sets the status. A named <paramref name="reviewedBy"/> is a staff action: it stamps the review fields, replaces
    /// ReviewNote with <paramref name="note"/> (null clears the previous decision's note) and sets StaffLocked.
    /// Without a reviewer (automation) the status may not become Active, a Staff or StaffLocked row may not be
    /// changed, and a null note keeps the existing one.
    /// </summary>
    public async Task SetStatusAsync(ChemicalDocument doc, ChemicalDocumentStatus status, string? reviewedBy, string? note)
    {
        ArgumentNullException.ThrowIfNull(doc);
        EnsureFits(reviewedBy, nameof(ChemicalDocument.ReviewedBy));
        EnsureFits(note, nameof(ChemicalDocument.ReviewNote));
        var reviewer = ReviewerOf(reviewedBy);
        Demand(reviewer, doc, status);

        Track(doc);
        doc.Status = status;
        if (reviewer != null)
        {
            doc.ReviewedBy = reviewer;
            doc.ReviewedAt = Now;
            doc.StaffLocked = true;
            doc.ReviewNote = note;
        }
        else if (note != null)
        {
            doc.ReviewNote = note;
        }

        await doc.Update(db);
    }

    /// <summary>
    /// Bookkeeping only: LastSeenAt, and LastCheckedAt when checked, = now. Deliberately outside the gate, because
    /// these are the fields F27 permits automation to change on hands-off rows.
    /// </summary>
    public async Task TouchAsync(ChemicalDocument doc, bool @checked = false)
    {
        ArgumentNullException.ThrowIfNull(doc);
        Track(doc);
        var now = Now;
        doc.LastSeenAt = now;
        if (@checked)
        {
            doc.LastCheckedAt = now;
        }

        await doc.Update(db);
    }

    /// <summary>No WorkflowState filter: the unique index covers removed rows too, so they are revived, never re-created.</summary>
    private Task<ChemicalDocument?> FindAsync(NewChemicalDocument doc, string md5) =>
        db.ChemicalDocuments.SingleOrDefaultAsync(d =>
            d.ChemicalId == doc.ChemicalId && d.Type == doc.Type && d.ContentMd5 == md5);

    private async Task<WriteResult> KnownAsync(ChemicalDocument existing, NewChemicalDocument doc, string md5)
    {
        MergeKnown(existing, doc);
        await TouchAsync(existing);
        return new WriteResult(WriteOutcome.AlreadyKnown, existing, md5);
    }

    private static void EnsurePdf(byte[]? pdf)
    {
        const string param = nameof(NewChemicalDocument.Pdf);
        if (pdf == null || pdf.Length == 0)
        {
            throw new ArgumentException("The document is empty.", param);
        }

        if (pdf.LongLength > MaxPdfBytes)
        {
            throw new ArgumentException($"The document is larger than {MaxPdfBytes} bytes.", param);
        }

        if (!pdf.AsSpan().StartsWith(PdfMagic))
        {
            throw new ArgumentException("The document is not a PDF (no %PDF- header).", param);
        }
    }

    private static void EnsureFits(NewChemicalDocument doc)
    {
        EnsureFits(doc.SourceUrl, nameof(ChemicalDocument.SourceUrl));
        EnsureFits(doc.SourceFileId, nameof(ChemicalDocument.SourceFileId));
        EnsureFits(doc.SourceFileName, nameof(ChemicalDocument.SourceFileName));
        EnsureFits(doc.SourcePackaging, nameof(ChemicalDocument.SourcePackaging));
        EnsureFits(doc.SourceAdapter, nameof(ChemicalDocument.SourceAdapter));
        EnsureFits(doc.SourceRevision, nameof(ChemicalDocument.SourceRevision));
        EnsureFits(doc.ValidationNotes, nameof(ChemicalDocument.ValidationNotes));
        EnsureFits(doc.ReviewedBy, nameof(ChemicalDocument.ReviewedBy));
    }

    private static void EnsureFits(string? value, string column)
    {
        if (value != null && value.Length > MaxLengths[column])
        {
            throw new ArgumentException(
                $"{column} is {value.Length} characters; the column holds {MaxLengths[column]}.", column);
        }
    }

    private static void EnsureSameContent(ChemicalDocument existing, string sha256)
    {
        // Rows written outside the writer may lack a SHA-256; then the md5 is all we can compare.
        if (existing.ContentSha256.Length > 0 && existing.ContentSha256 != sha256)
        {
            throw new ChemicalDocumentHashCollisionException(existing.Id, existing.ContentMd5);
        }
    }

    /// <summary>
    /// Throws when any row (any chemical, type or state) has this md5 with another SHA-256. True when a row already
    /// vouches for these exact bytes, so the stored blob need not be downloaded to check it.
    /// </summary>
    private async Task<bool> EnsureNoOtherContentAsync(string md5, string sha256)
    {
        var rows = await db.ChemicalDocuments.AsNoTracking()
            .Where(d => d.ContentMd5 == md5 && d.ContentSha256 != "")
            .Select(d => new { d.Id, d.ContentSha256 })
            .ToListAsync();
        var other = rows.FirstOrDefault(row => row.ContentSha256 != sha256);
        if (other != null)
        {
            throw new ChemicalDocumentHashCollisionException(other.Id, md5);
        }

        return rows.Count > 0;
    }

    /// <summary>
    /// Stores the blob once and never overwrites it. An existing blob that no row vouches for is downloaded and must
    /// hold our bytes.
    /// </summary>
    private async Task EnsureBlobAsync(byte[] pdf, string md5, string sha256, bool knownContent)
    {
        var key = DocumentHashes.BlobKey(md5);
        if (await blobs.ExistsAsync(key))
        {
            if (!knownContent && await blobs.GetAsync(key) is { } stored && DocumentHashes.Sha256Hex(stored) != sha256)
            {
                throw new ChemicalDocumentHashCollisionException(md5);
            }

            return;
        }

        using var content = new MemoryStream(pdf, writable: false);
        await blobs.PutAsync(content, key);
    }

    /// <summary>
    /// Fills missing provenance (not a hands-off field) and, when the gate allows it, a missing language and the
    /// revival of a removed row. Never touches Status, Source or StaffLocked. So automation never undoes a staff
    /// decision and never revives an Active row into view.
    /// </summary>
    private static void MergeKnown(ChemicalDocument existing, NewChemicalDocument doc)
    {
        existing.SourceUrl ??= doc.SourceUrl;
        existing.SourceFileId ??= doc.SourceFileId;
        existing.SourceFileName ??= doc.SourceFileName;
        existing.SourcePackaging ??= doc.SourcePackaging;
        existing.SourceProductId ??= doc.SourceProductId;
        existing.SourceAdapter ??= doc.SourceAdapter;
        existing.SourceRevisionDate ??= doc.SourceRevisionDate;
        existing.SourceRevision ??= doc.SourceRevision;

        // Language and revival are changes to the row as it stands, so they pass the same gate as a status change.
        if (Authorize(doc.Reviewer, existing, existing.Status) != Denial.None)
        {
            return;
        }

        existing.Language ??= SdsLanguages.Normalize(doc.Language);
        if (existing.WorkflowState == WorkflowStates.Removed)
        {
            existing.WorkflowState = WorkflowStates.Created;
        }
    }

    internal static string? ReviewerOf(string? reviewedBy) => string.IsNullOrWhiteSpace(reviewedBy) ? null : reviewedBy;

    private enum Denial
    {
        None,
        ActiveNeedsReviewer,
        HandsOff,
    }

    /// <summary>
    /// The one authorization gate for every change that could put a document live or alter a staff decision:
    /// creating a row, setting a status, and (on AlreadyKnown) filling the language or reviving a removed row.
    /// A staff action is one with a named reviewer, nothing else (Source=Staff alone is not).
    /// <list type="bullet">
    /// <item>The resulting status may be Active only with a named reviewer.</item>
    /// <item>Without a reviewer, a hands-off row (Source=Staff or StaffLocked) may not be changed.</item>
    /// </list>
    /// </summary>
    private static Denial Authorize(string? reviewer, ChemicalDocument? row, ChemicalDocumentStatus resultingStatus)
    {
        if (resultingStatus == ChemicalDocumentStatus.Active && reviewer == null)
        {
            return Denial.ActiveNeedsReviewer;
        }

        return reviewer == null && row != null && IsHandsOff(row) ? Denial.HandsOff : Denial.None;
    }

    private static void Demand(string? reviewer, ChemicalDocument? row, ChemicalDocumentStatus resultingStatus)
    {
        switch (Authorize(reviewer, row, resultingStatus))
        {
            case Denial.ActiveNeedsReviewer:
                throw new ArgumentException("Only a named staff reviewer may make a document Active.", nameof(NewChemicalDocument.ReviewedBy));
            case Denial.HandsOff:
                throw new InvalidOperationException(
                    $"ChemicalDocument {row!.Id} is a Staff or StaffLocked row; automation may not change it.");
        }
    }

    private static bool IsHandsOff(ChemicalDocument doc) =>
        doc.Source == ChemicalDocumentSource.Staff || doc.StaffLocked;

    private void Track(ChemicalDocument doc)
    {
        if (db.Entry(doc).State == EntityState.Detached)
        {
            db.Attach(doc);
        }
    }
}
