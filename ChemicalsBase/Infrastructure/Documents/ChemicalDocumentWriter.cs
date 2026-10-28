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

/// <summary><see cref="Document"/> is null only for a dry run of an unknown document.</summary>
public sealed record WriteResult(WriteOutcome Outcome, ChemicalDocument? Document, string ContentMd5);

/// <summary>
/// The single write path for chemical documents (labels, SDS, minor-use instructions): BMD label import,
/// SDS harvester, staff uploads and the producer-upload inbox. Dedupes on (ChemicalId, Type, ContentMd5),
/// stores each blob once under "&lt;md5&gt;.pdf", and writes through PnBase.Create/Update so every change has a
/// version row.
/// <para>
/// Nothing goes live automatically: <see cref="ChemicalDocumentStatus.Active"/> is only written for a staff
/// path (Source=Staff or a named reviewer), and automation never revives a soft-deleted Active row.
/// A known document keeps its Status, Source and StaffLocked, so a staff rejection is sticky.
/// </para>
/// </summary>
public sealed class ChemicalDocumentWriter(ChemicalsDbContext db, IChemicalBlobStore blobs, TimeProvider clock)
{
    public const long MaxPdfBytes = 20L * 1024 * 1024;

    private static ReadOnlySpan<byte> PdfMagic => "%PDF-"u8;

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<WriteResult> WriteAsync(NewChemicalDocument doc, bool dryRun = false)
    {
        ArgumentNullException.ThrowIfNull(doc);
        EnsurePdf(doc.Pdf);
        if (doc.Status == ChemicalDocumentStatus.Active && !doc.IsStaffPath)
        {
            throw new ArgumentException(
                "Only a staff path (Source=Staff or a named ReviewedBy) may write an Active document.", nameof(doc));
        }

        var md5 = DocumentHashes.Md5Hex(doc.Pdf);

        // No WorkflowState filter: the unique index covers removed rows too, so they are revived, never re-created.
        var existing = await db.ChemicalDocuments.SingleOrDefaultAsync(d =>
            d.ChemicalId == doc.ChemicalId && d.Type == doc.Type && d.ContentMd5 == md5);

        if (dryRun)
        {
            return new WriteResult(WriteOutcome.DryRun, existing, md5);
        }

        await EnsureBlobAsync(doc.Pdf, md5);

        if (existing != null)
        {
            MergeKnown(existing, doc);
            await TouchAsync(existing);
            return new WriteResult(WriteOutcome.AlreadyKnown, existing, md5);
        }

        var now = Now;
        var reviewedBy = string.IsNullOrWhiteSpace(doc.ReviewedBy) ? null : doc.ReviewedBy;
        var created = new ChemicalDocument
        {
            ChemicalId = doc.ChemicalId,
            Type = doc.Type,
            Source = doc.Source,
            Status = doc.Status,
            Language = SdsLanguages.Normalize(doc.Language),
            ContentMd5 = md5,
            ContentSha256 = DocumentHashes.Sha256Hex(doc.Pdf),
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
            StaffLocked = doc.IsStaffPath,
            ReviewedBy = reviewedBy,
            ReviewedAt = reviewedBy == null ? null : now,
        };
        await created.Create(db);
        return new WriteResult(WriteOutcome.Created, created, md5);
    }

    /// <summary>
    /// Sets the status. A named <paramref name="reviewedBy"/> is a staff action: it stamps the review fields and
    /// sets StaffLocked. Without a reviewer (automation) the status may not become Active and a Staff or
    /// StaffLocked row may not be changed.
    /// </summary>
    public async Task SetStatusAsync(ChemicalDocument doc, ChemicalDocumentStatus status, string? reviewedBy, string? note)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var byStaff = !string.IsNullOrWhiteSpace(reviewedBy);
        if (status == ChemicalDocumentStatus.Active && !byStaff)
        {
            throw new ArgumentException("Only a named staff reviewer may set a document Active.", nameof(reviewedBy));
        }

        if (!byStaff && IsHandsOff(doc))
        {
            throw new InvalidOperationException(
                $"ChemicalDocument {doc.Id} is a Staff or StaffLocked row; automation may not change its status.");
        }

        Track(doc);
        doc.Status = status;
        if (byStaff)
        {
            doc.ReviewedBy = reviewedBy;
            doc.ReviewedAt = Now;
            doc.StaffLocked = true;
        }

        if (note != null)
        {
            doc.ReviewNote = note;
        }

        await doc.Update(db);
    }

    /// <summary>Bookkeeping only (allowed on staff rows): LastSeenAt, and LastCheckedAt when checked, = now.</summary>
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

    private static void EnsurePdf(byte[]? pdf)
    {
        if (pdf == null || pdf.Length == 0)
        {
            throw new ArgumentException("The document is empty.", nameof(pdf));
        }

        if (pdf.LongLength > MaxPdfBytes)
        {
            throw new ArgumentException($"The document is larger than {MaxPdfBytes} bytes.", nameof(pdf));
        }

        if (!pdf.AsSpan().StartsWith(PdfMagic))
        {
            throw new ArgumentException("The document is not a PDF (no %PDF- header).", nameof(pdf));
        }
    }

    private async Task EnsureBlobAsync(byte[] pdf, string md5)
    {
        var key = DocumentHashes.BlobKey(md5);
        if (await blobs.ExistsAsync(key))
        {
            return;
        }

        using var content = new MemoryStream(pdf, writable: false);
        await blobs.PutAsync(content, key);
    }

    /// <summary>
    /// Fills missing provenance and revives a removed row. Never touches Status, Source or StaffLocked; the language
    /// is filled only when unknown and the row is not hands-off; a removed Active row is revived only by a staff path.
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

        if (!IsHandsOff(existing))
        {
            existing.Language ??= SdsLanguages.Normalize(doc.Language);
        }

        if (existing.WorkflowState == WorkflowStates.Removed
            && (existing.Status != ChemicalDocumentStatus.Active || doc.IsStaffPath))
        {
            existing.WorkflowState = WorkflowStates.Created;
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
