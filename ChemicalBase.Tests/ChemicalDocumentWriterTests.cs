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

using System.Security.Cryptography;
using System.Text;
using ChemicalsBase.Infrastructure.Data.Entities;
using ChemicalsBase.Infrastructure.Data.Enums;
using ChemicalsBase.Infrastructure.Data.Factories;
using ChemicalsBase.Infrastructure.Documents;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowStates = ChemicalsBase.Infrastructure.Constants.Constants.WorkflowStates;

namespace ChemicalBase.Tests;

[TestFixture]
public class ChemicalDocumentWriterTests : DbTestFixture
{
    private const int ChemA = 101;
    private const int ChemB = 102;
    private static readonly DateTime T0 = new(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc);

    private InMemoryBlobStore _blobs = null!;
    private ManualClock _clock = null!;
    private ChemicalDocumentWriter _writer = null!;

    [SetUp]
    public void CreateWriter()
    {
        _blobs = new InMemoryBlobStore();
        _clock = new ManualClock(T0);
        _writer = new ChemicalDocumentWriter(DbContext, _blobs, _clock);
    }

    private static NewChemicalDocument BmdLabel(byte[] pdf, int chemicalId = ChemA, string? language = null,
        string? sourceUrl = null, string? sourceFileId = null) =>
        new(chemicalId, ChemicalDocumentType.Label, ChemicalDocumentSource.Bmd, ChemicalDocumentStatus.PendingReview, pdf,
            SourceUrl: sourceUrl, SourceFileId: sourceFileId, SourceAdapter: "bmd", Language: language);

    private static string Md5Of(byte[] bytes) => Convert.ToHexStringLower(MD5.HashData(bytes));

    private Task<ChemicalDocument> Stored(int id) =>
        DbContext.ChemicalDocuments.AsNoTracking().SingleAsync(d => d.Id == id);

    [Test]
    public async Task Rejects_non_pdf_empty_and_oversize()
    {
        var oversize = new byte[ChemicalDocumentWriter.MaxPdfBytes + 1];
        "%PDF-1.7"u8.CopyTo(oversize);

        var cases = new (string Name, byte[] Bytes)[]
        {
            ("empty", Array.Empty<byte>()),
            ("not a pdf", "<html>not a pdf</html>"u8.ToArray()),
            ("over 20 MB", oversize),
        };
        foreach (var (name, bytes) in cases)
        {
            await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(), () => _writer.WriteAsync(BmdLabel(bytes)), name);
        }

        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.Zero);
        Assert.That(_blobs.PutCount, Is.Zero);
    }

    [Test]
    public async Task Stores_blob_under_md5_key_once()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var md5 = Md5Of(pdf);

        var first = await _writer.WriteAsync(BmdLabel(pdf));
        var second = await _writer.WriteAsync(BmdLabel(pdf));

        Assert.That(first.ContentMd5, Is.EqualTo(md5));
        Assert.That(second.ContentMd5, Is.EqualTo(md5));
        Assert.That(_blobs.Blobs.Keys, Is.EqualTo(new[] { md5 + ".pdf" }));
        Assert.That(_blobs.Blobs[md5 + ".pdf"], Is.EqualTo(pdf));
        Assert.That(_blobs.PutCount, Is.EqualTo(1));

        var stored = await Stored(first.Document!.Id);
        Assert.That(stored.ContentMd5, Is.EqualTo(md5));
        Assert.That(stored.ContentSha256, Is.EqualTo(Convert.ToHexStringLower(SHA256.HashData(pdf))));
        Assert.That(stored.SizeBytes, Is.EqualTo(pdf.Length));

        // A blob that is already in the store (e.g. the same SDS uploaded before) is never uploaded again.
        var other = TestPdf.Bytes("Roundup etiket");
        var seeded = new InMemoryBlobStore();
        seeded.Blobs[Md5Of(other) + ".pdf"] = other;
        var created = await new ChemicalDocumentWriter(DbContext, seeded, _clock).WriteAsync(BmdLabel(other));
        Assert.That(created.Outcome, Is.EqualTo(WriteOutcome.Created));
        Assert.That(seeded.PutCount, Is.Zero);
    }

    [Test]
    public async Task Same_content_same_chemical_returns_AlreadyKnown_and_touches()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var first = await _writer.WriteAsync(BmdLabel(pdf));

        _clock.Advance(TimeSpan.FromHours(1));
        var again = await _writer.WriteAsync(BmdLabel(pdf, sourceUrl: "https://bmd.example/a", sourceFileId: "guid-1"));

        _clock.Advance(TimeSpan.FromHours(1));
        var third = await _writer.WriteAsync(BmdLabel(pdf, sourceUrl: "https://bmd.example/b", sourceFileId: "guid-2"));

        Assert.That(first.Outcome, Is.EqualTo(WriteOutcome.Created));
        Assert.That((again.Outcome, third.Outcome), Is.EqualTo((WriteOutcome.AlreadyKnown, WriteOutcome.AlreadyKnown)));
        Assert.That(again.Document!.Id, Is.EqualTo(first.Document!.Id));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(1));

        var stored = await Stored(first.Document.Id);
        Assert.That(stored.RetrievedAt, Is.EqualTo(T0));
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0.AddHours(2)));
        Assert.That(stored.LastCheckedAt, Is.Null);
        // Missing provenance is filled in once; known provenance is never overwritten.
        Assert.That(stored.SourceUrl, Is.EqualTo("https://bmd.example/a"));
        Assert.That(stored.SourceFileId, Is.EqualTo("guid-1"));
    }

    [Test]
    public async Task Same_content_on_two_chemicals_gives_two_rows_one_blob()
    {
        var blobs = new InMemoryBlobStore();
        var writer = new ChemicalDocumentWriter(DbContext, blobs, TimeProvider.System);
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var a = await writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff, ChemicalDocumentStatus.Active, pdf, ReviewedBy: "Anne Staff"));
        var b = await writer.WriteAsync(new(ChemB, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff, ChemicalDocumentStatus.Active, pdf, ReviewedBy: "Anne Staff"));
        Assert.That((a.Outcome, b.Outcome), Is.EqualTo((WriteOutcome.Created, WriteOutcome.Created)));
        Assert.That(a.Document!.Id, Is.Not.EqualTo(b.Document!.Id));
        Assert.That(blobs.PutCount, Is.EqualTo(1));
    }

    [Test]
    public async Task AlreadyKnown_keeps_status_source_and_lock()
    {
        // A staff upload: Source=Staff, Active.
        var staffPdf = TestPdf.Bytes("Staff etiket");
        var staff = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff,
            ChemicalDocumentStatus.Active, staffPdf, ReviewedBy: "Anne Staff"));

        // A BMD label that staff approved: Source=Bmd, Active, StaffLocked.
        var bmdPdf = TestPdf.Bytes("BMD etiket");
        var bmd = await _writer.WriteAsync(BmdLabel(bmdPdf));
        await _writer.SetStatusAsync(bmd.Document!, ChemicalDocumentStatus.Active, "Anne Staff", "ok");

        _clock.Advance(TimeSpan.FromHours(1));
        var seenStaff = await _writer.WriteAsync(BmdLabel(staffPdf));
        var seenBmd = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Supplier,
            ChemicalDocumentStatus.PendingReview, bmdPdf, SourceAdapter: "site:1:sitemap"));

        Assert.That((seenStaff.Outcome, seenBmd.Outcome), Is.EqualTo((WriteOutcome.AlreadyKnown, WriteOutcome.AlreadyKnown)));

        var storedStaff = await Stored(staff.Document!.Id);
        Assert.That(storedStaff.Source, Is.EqualTo(ChemicalDocumentSource.Staff));
        Assert.That(storedStaff.Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(storedStaff.StaffLocked, Is.True);
        Assert.That(storedStaff.LastSeenAt, Is.EqualTo(T0.AddHours(1)));

        var storedBmd = await Stored(bmd.Document.Id);
        Assert.That(storedBmd.Source, Is.EqualTo(ChemicalDocumentSource.Bmd));
        Assert.That(storedBmd.Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(storedBmd.StaffLocked, Is.True);
        Assert.That(storedBmd.ReviewedBy, Is.EqualTo("Anne Staff"));
        Assert.That(storedBmd.ReviewedAt, Is.EqualTo(T0));
        Assert.That(storedBmd.ReviewNote, Is.EqualTo("ok"));
        Assert.That(storedBmd.SourceAdapter, Is.EqualTo("bmd"));
        Assert.That(storedBmd.LastSeenAt, Is.EqualTo(T0.AddHours(1)));
    }

    [Test]
    public async Task Rejected_stays_rejected_when_seen_again()
    {
        var pdf = TestPdf.Bytes("Forkert produkt");
        var first = await _writer.WriteAsync(BmdLabel(pdf));
        await _writer.SetStatusAsync(first.Document!, ChemicalDocumentStatus.Rejected, "Anne Staff", "wrong product");

        _clock.Advance(TimeSpan.FromDays(7));
        var again = await _writer.WriteAsync(BmdLabel(pdf));

        Assert.That(again.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(1));
        var stored = await Stored(first.Document.Id);
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.ReviewNote, Is.EqualTo("wrong product"));
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0.AddDays(7)));
    }

    [Test]
    public async Task DryRun_writes_nothing()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");

        var dry = await _writer.WriteAsync(BmdLabel(pdf), dryRun: true);

        Assert.That(dry.Outcome, Is.EqualTo(WriteOutcome.DryRun));
        Assert.That(dry.Document, Is.Null);
        Assert.That(dry.ContentMd5, Is.EqualTo(Md5Of(pdf)));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.Zero);
        Assert.That(await DbContext.ChemicalDocumentVersions.CountAsync(), Is.Zero);
        Assert.That(_blobs.PutCount, Is.Zero);

        // A dry run over a known document does not touch it either.
        var real = await _writer.WriteAsync(BmdLabel(pdf));
        _clock.Advance(TimeSpan.FromHours(1));
        var dryAgain = await _writer.WriteAsync(BmdLabel(pdf, sourceUrl: "https://bmd.example/a"), dryRun: true);

        Assert.That(dryAgain.Outcome, Is.EqualTo(WriteOutcome.DryRun));
        Assert.That(dryAgain.Document!.Id, Is.EqualTo(real.Document!.Id));
        var stored = await Stored(real.Document.Id);
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0));
        Assert.That(stored.SourceUrl, Is.Null);
        Assert.That(stored.Version, Is.EqualTo(1));
        Assert.That(await DbContext.ChemicalDocumentVersions.CountAsync(), Is.EqualTo(1));
        Assert.That(_blobs.PutCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Every_write_has_a_version_row()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var created = await _writer.WriteAsync(BmdLabel(pdf));
        var doc = created.Document!;

        _clock.Advance(TimeSpan.FromHours(1));
        await _writer.WriteAsync(BmdLabel(pdf));
        _clock.Advance(TimeSpan.FromHours(1));
        await _writer.TouchAsync(doc, @checked: true);
        _clock.Advance(TimeSpan.FromHours(1));
        await _writer.SetStatusAsync(doc, ChemicalDocumentStatus.Superseded, null, "withdrawn-by-bmd");

        var versions = await DbContext.ChemicalDocumentVersions.AsNoTracking()
            .Where(v => v.ChemicalDocumentId == doc.Id).OrderBy(v => v.Version).ToListAsync();
        Assert.That(versions.Select(v => v.Version), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        Assert.That(versions[0].LastSeenAt, Is.EqualTo(T0));
        Assert.That(versions[1].LastSeenAt, Is.EqualTo(T0.AddHours(1)));
        Assert.That(versions[2].LastCheckedAt, Is.EqualTo(T0.AddHours(2)));
        Assert.That(versions[3].Status, Is.EqualTo(ChemicalDocumentStatus.Superseded));
        Assert.That(versions[3].ReviewNote, Is.EqualTo("withdrawn-by-bmd"));

        var stored = await Stored(doc.Id);
        Assert.That(stored.Version, Is.EqualTo(4));
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0.AddHours(2)));
        Assert.That(stored.LastCheckedAt, Is.EqualTo(T0.AddHours(2)));
    }

    [Test]
    public async Task Language_normalised_and_staff_value_never_overwritten()
    {
        var upper = await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Dansk etiket"), language: "DA"));
        Assert.That((await Stored(upper.Document!.Id)).Language, Is.EqualTo("da"));

        var unknown = await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Ukendt sprog"), language: "xx"));
        Assert.That((await Stored(unknown.Document!.Id)).Language, Is.Null);

        // A null stored language is filled in (normalised) when the document is seen again ...
        var pdf = TestPdf.Bytes("Safety data sheet");
        var first = await _writer.WriteAsync(BmdLabel(pdf));
        await _writer.WriteAsync(BmdLabel(pdf, language: "en-GB"));
        Assert.That((await Stored(first.Document!.Id)).Language, Is.EqualTo("en"));

        // ... but a stored language (possibly a staff correction) is never overwritten.
        await _writer.WriteAsync(BmdLabel(pdf, language: "da"));
        Assert.That((await Stored(first.Document.Id)).Language, Is.EqualTo("en"));

        // Hands off staff rows: even a null language on a Staff/StaffLocked row is left to staff.
        var staffPdf = TestPdf.Bytes("Staff upload");
        var staff = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff,
            ChemicalDocumentStatus.Active, staffPdf, ReviewedBy: "Anne Staff"));
        await _writer.WriteAsync(BmdLabel(staffPdf, language: "da"));
        Assert.That((await Stored(staff.Document!.Id)).Language, Is.Null);
    }

    [Test]
    public async Task Removed_row_is_revived_not_recreated()
    {
        // Automation rejected it (not hands-off), then a rollback soft-deleted it (F6).
        var pdf = TestPdf.Bytes("Forkert produkt");
        var first = await _writer.WriteAsync(BmdLabel(pdf));
        await _writer.SetStatusAsync(first.Document!, ChemicalDocumentStatus.Rejected, null, "regno-mismatch=page1");
        await first.Document!.Delete(DbContext);

        _clock.Advance(TimeSpan.FromDays(1));
        var again = await _writer.WriteAsync(BmdLabel(pdf));

        Assert.That(again.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        Assert.That(again.Document!.Id, Is.EqualTo(first.Document.Id));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(1));
        var stored = await Stored(first.Document.Id);
        Assert.That(stored.WorkflowState, Is.EqualTo(WorkflowStates.Created));
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0.AddDays(1)));
        Assert.That(_blobs.PutCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Active_write_needs_a_named_reviewer_even_from_staff()
    {
        // Owner rule + ruling: every Active row carries a ReviewedBy (Task 22: "zero Active rows without ReviewedBy").
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(), () => _writer.WriteAsync(
            new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Bmd, ChemicalDocumentStatus.Active, pdf)), "bmd");
        await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(), () => _writer.WriteAsync(
            new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff, ChemicalDocumentStatus.Active, pdf,
                ReviewedBy: "  ")), "staff without reviewer");

        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.Zero);
        Assert.That(_blobs.PutCount, Is.Zero);
    }

    [Test]
    public async Task Automation_cannot_set_active()
    {
        var pending = await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Bumper 25 EC etiket")));
        Assert.That(pending.Document!.Status, Is.EqualTo(ChemicalDocumentStatus.PendingReview));
        Assert.That(pending.Document.StaffLocked, Is.False);

        await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(),
            () => _writer.SetStatusAsync(pending.Document, ChemicalDocumentStatus.Active, null, null));

        Assert.That((await Stored(pending.Document.Id)).Status, Is.EqualTo(ChemicalDocumentStatus.PendingReview));
    }

    [Test]
    public async Task Reviewer_stamps_and_locks_only_reviewed_rows()
    {
        // A named reviewer writing an Active document (e.g. a staff backfill): stamped and locked.
        var active = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Bmd,
            ChemicalDocumentStatus.Active, TestPdf.Bytes("Godkendt etiket"), ReviewedBy: "Anne Staff"));
        var storedActive = await Stored(active.Document!.Id);
        Assert.That(storedActive.Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(storedActive.StaffLocked, Is.True);
        Assert.That(storedActive.ReviewedBy, Is.EqualTo("Anne Staff"));
        Assert.That(storedActive.ReviewedAt, Is.EqualTo(T0));

        // Staff assigning an inbox file creates a PendingReview row: nobody has reviewed it yet.
        var pending = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Sds, ChemicalDocumentSource.Supplier,
            ChemicalDocumentStatus.PendingReview, TestPdf.Bytes("Producer SDS"), SourceAdapter: "producer-upload:3",
            ReviewedBy: "Anne Staff"));
        var storedPending = await Stored(pending.Document!.Id);
        Assert.That(storedPending.StaffLocked, Is.False);
        Assert.That(storedPending.ReviewedBy, Is.Null);
        Assert.That(storedPending.ReviewedAt, Is.Null);
    }

    [Test]
    public async Task Automation_cannot_change_status_of_staff_locked_row()
    {
        var reviewed = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Bmd,
            ChemicalDocumentStatus.Active, TestPdf.Bytes("Godkendt etiket"), ReviewedBy: "Anne Staff"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _writer.SetStatusAsync(reviewed.Document!, ChemicalDocumentStatus.Superseded, null, "withdrawn-by-bmd"));

        Assert.That((await Stored(reviewed.Document!.Id)).Status, Is.EqualTo(ChemicalDocumentStatus.Active));
    }

    [Test]
    public async Task Staff_status_change_replaces_review_note()
    {
        var doc = (await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Bumper 25 EC etiket")))).Document!;
        await _writer.SetStatusAsync(doc, ChemicalDocumentStatus.Rejected, "Anne Staff", "wrong product");

        _clock.Advance(TimeSpan.FromHours(1));
        await _writer.SetStatusAsync(doc, ChemicalDocumentStatus.Active, "Bo Staff", null);

        var stored = await Stored(doc.Id);
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(stored.ReviewedBy, Is.EqualTo("Bo Staff"));
        Assert.That(stored.ReviewedAt, Is.EqualTo(T0.AddHours(1)));
        Assert.That(stored.ReviewNote, Is.Null, "a staff decision without a note clears the previous decision's note");
    }

    [Test]
    public async Task Removed_active_row_is_revived_only_by_staff()
    {
        var pdf = TestPdf.Bytes("Godkendt etiket");
        var reviewed = await _writer.WriteAsync(new(ChemB, ChemicalDocumentType.Label, ChemicalDocumentSource.Bmd,
            ChemicalDocumentStatus.Active, pdf, ReviewedBy: "Anne Staff"));
        await reviewed.Document!.Delete(DbContext);

        // Automation seeing it again must not put it live again: touched only.
        _clock.Advance(TimeSpan.FromHours(1));
        var seen = await _writer.WriteAsync(BmdLabel(pdf, chemicalId: ChemB));
        Assert.That(seen.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        Assert.That(seen.IsRemoved, Is.True);
        var stillRemoved = await Stored(reviewed.Document.Id);
        Assert.That(stillRemoved.WorkflowState, Is.EqualTo(WorkflowStates.Removed));
        Assert.That(stillRemoved.Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(stillRemoved.LastSeenAt, Is.EqualTo(T0.AddHours(1)));

        var restored = await _writer.WriteAsync(new(ChemB, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff,
            ChemicalDocumentStatus.Active, pdf, ReviewedBy: "Bo Staff"));
        Assert.That(restored.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        Assert.That(restored.IsRemoved, Is.False);
        Assert.That((await Stored(reviewed.Document.Id)).WorkflowState, Is.EqualTo(WorkflowStates.Created));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Hands_off_removed_row_is_revived_only_by_staff()
    {
        // Ruling: automation never undoes a staff decision, so a staff-rejected row that was removed stays removed.
        var pdf = TestPdf.Bytes("Forkert produkt");
        var first = await _writer.WriteAsync(BmdLabel(pdf));
        await _writer.SetStatusAsync(first.Document!, ChemicalDocumentStatus.Rejected, "Anne Staff", "wrong product");
        await first.Document!.Delete(DbContext);

        _clock.Advance(TimeSpan.FromHours(1));
        var seen = await _writer.WriteAsync(BmdLabel(pdf));
        Assert.That(seen.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        var stored = await Stored(first.Document.Id);
        Assert.That(stored.WorkflowState, Is.EqualTo(WorkflowStates.Removed));
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0.AddHours(1)));

        // A staff-path write revives it, and it is still Rejected.
        var staff = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff,
            ChemicalDocumentStatus.PendingReview, pdf, ReviewedBy: "Bo Staff"));
        Assert.That(staff.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        var revived = await Stored(first.Document.Id);
        Assert.That(revived.WorkflowState, Is.EqualTo(WorkflowStates.Created));
        Assert.That(revived.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Concurrent_insert_returns_AlreadyKnown_and_context_stays_usable()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        await using var otherDb = new ChemicalsContextFactory().CreateDbContext([ConnectionString]);
        var otherWriter = new ChemicalDocumentWriter(otherDb, new InMemoryBlobStore(), _clock);
        WriteResult? winner = null;

        // Between this writer's lookup and its insert (the blob upload sits in between), another writer inserts.
        _blobs.BeforeNextPut = async () =>
            winner = await otherWriter.WriteAsync(BmdLabel(pdf, sourceUrl: "https://holder.example/sds.pdf"));

        var loser = await _writer.WriteAsync(BmdLabel(pdf, sourceFileId: "guid-1"));

        Assert.That(winner!.Outcome, Is.EqualTo(WriteOutcome.Created));
        Assert.That(loser.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        Assert.That(loser.Document!.Id, Is.EqualTo(winner.Document!.Id));
        var stored = await Stored(winner.Document.Id);
        Assert.That(stored.SourceUrl, Is.EqualTo("https://holder.example/sds.pdf"));
        Assert.That(stored.SourceFileId, Is.EqualTo("guid-1"));

        // The failed insert must not poison the shared context: later writes in it still save.
        var next = await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Roundup etiket")));
        Assert.That(next.Outcome, Is.EqualTo(WriteOutcome.Created));
        _clock.Advance(TimeSpan.FromHours(1));
        await _writer.TouchAsync(loser.Document, @checked: true);
        Assert.That((await Stored(winner.Document.Id)).LastCheckedAt, Is.EqualTo(T0.AddHours(1)));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Failed_version_row_rolls_back_the_row_and_a_retry_creates_both()
    {
        // PnBase.Create saves the row, then the version row. The writer wraps both in one transaction: when the
        // second save fails (a Galera certification or deadlock error) neither row persists, nothing failed is
        // retried by a later save, and a retry creates the document with its first version (not AlreadyKnown).
        var fault = new FailNextVersionInsert();
        await using var faultyDb = PlainContext(fault);
        var writer = new ChemicalDocumentWriter(faultyDb, new InMemoryBlobStore(), _clock);
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");

        await Assert.ThrowsAsync<DbUpdateException>(() => writer.WriteAsync(BmdLabel(pdf)));
        Assert.That(fault.Failed, Is.True, "the injected fault fired");
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.Zero, "the row was rolled back");

        var unrelated = await writer.WriteAsync(BmdLabel(TestPdf.Bytes("Roundup etiket")));
        var retry = await writer.WriteAsync(BmdLabel(pdf));

        Assert.That(unrelated.Outcome, Is.EqualTo(WriteOutcome.Created));
        Assert.That(retry.Outcome, Is.EqualTo(WriteOutcome.Created));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(2));
        foreach (var id in new[] { unrelated.Document!.Id, retry.Document!.Id })
        {
            Assert.That(await DbContext.ChemicalDocumentVersions.CountAsync(v => v.ChemicalDocumentId == id), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Create_enlists_in_the_callers_transaction()
    {
        await using var plainDb = PlainContext();
        var writer = new ChemicalDocumentWriter(plainDb, new InMemoryBlobStore(), _clock);

        await using (var transaction = await plainDb.Database.BeginTransactionAsync())
        {
            var result = await writer.WriteAsync(BmdLabel(TestPdf.Bytes("Bumper 25 EC etiket")));
            Assert.That(result.Outcome, Is.EqualTo(WriteOutcome.Created));
            await transaction.RollbackAsync(); // the caller owns commit and rollback
        }

        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.Zero);
        Assert.That(await DbContext.ChemicalDocumentVersions.CountAsync(), Is.Zero);
    }

    // ---- stale automation vs a staff decision made in between (Copilot r4397879800) ----

    [Test]
    public async Task Stale_automation_status_change_does_not_overwrite_a_staff_decision()
    {
        var doc = (await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Bumper 25 EC etiket")))).Document!;
        await StaffRejectsElsewhere(doc.Id);

        // The automation still holds the unlocked PendingReview row it loaded before.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _writer.SetStatusAsync(doc, ChemicalDocumentStatus.Superseded, null, "withdrawn-by-bmd"));

        var stored = await Stored(doc.Id);
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.ReviewNote, Is.EqualTo("wrong product"));
        Assert.That(stored.StaffLocked, Is.True);
    }

    [Test]
    public async Task Stale_automation_merge_does_not_fill_a_staff_locked_row()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var doc = (await _writer.WriteAsync(BmdLabel(pdf))).Document!;
        await StaffRejectsElsewhere(doc.Id);

        var again = await _writer.WriteAsync(BmdLabel(pdf, language: "da"));
        Assert.That(again.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));

        var stored = await Stored(doc.Id);
        Assert.That(stored.Language, Is.Null, "the gate re-ran on the fresh, staff-locked row");
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0.AddHours(1)), "bookkeeping still applied");
    }

    [Test]
    public async Task Stale_touch_keeps_the_staff_decision_and_still_touches()
    {
        var doc = (await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Bumper 25 EC etiket")))).Document!;
        await StaffRejectsElsewhere(doc.Id);

        await _writer.TouchAsync(doc, @checked: true);

        var stored = await Stored(doc.Id);
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.LastCheckedAt, Is.EqualTo(T0.AddHours(1)));
    }

    [Test]
    public async Task Stale_staff_decision_surfaces_the_conflict()
    {
        var doc = (await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Bumper 25 EC etiket")))).Document!;
        await StaffRejectsElsewhere(doc.Id);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => _writer.SetStatusAsync(doc, ChemicalDocumentStatus.Active, "Anne Staff", "looks fine"));

        var stored = await Stored(doc.Id);
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.ReviewedBy, Is.EqualTo("Bo Staff"));

        // The context stays usable: the conflicted row was reloaded.
        await _writer.TouchAsync(doc);
        Assert.That((await Stored(doc.Id)).Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
    }

    /// <summary>Staff rejects the row in another context (another request), one hour later.</summary>
    private async Task StaffRejectsElsewhere(int id)
    {
        _clock.Advance(TimeSpan.FromHours(1));
        await using var staffDb = new ChemicalsContextFactory().CreateDbContext([ConnectionString]);
        var row = await staffDb.ChemicalDocuments.SingleAsync(d => d.Id == id);
        await new ChemicalDocumentWriter(staffDb, new InMemoryBlobStore(), _clock)
            .SetStatusAsync(row, ChemicalDocumentStatus.Rejected, "Bo Staff", "wrong product");
    }

    /// <summary>A context without the retrying execution strategy, so a test may own a transaction.</summary>
    private ChemicalsBase.Infrastructure.ChemicalsDbContext PlainContext(
        params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<ChemicalsBase.Infrastructure.ChemicalsDbContext>()
            .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString))
            .AddInterceptors(interceptors)
            .Options);

    /// <summary>Fails the first SaveChanges that inserts a ChemicalDocumentVersion, once.</summary>
    private sealed class FailNextVersionInsert : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public bool Failed { get; private set; }

        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!Failed && eventData.Context!.ChangeTracker.Entries<ChemicalDocumentVersion>()
                    .Any(e => e.State == EntityState.Added))
            {
                Failed = true;
                throw new DbUpdateException("injected: version row insert failed");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    [Test]
    public async Task Overlong_text_is_rejected_before_the_database()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var longUrl = "https://holder.example/" + new string('a', 1100);

        await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(), () => _writer.WriteAsync(BmdLabel(pdf, sourceUrl: longUrl)));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.Zero);
        Assert.That(_blobs.PutCount, Is.Zero);

        var doc = (await _writer.WriteAsync(BmdLabel(pdf))).Document!;
        await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(),
            () => _writer.SetStatusAsync(doc, ChemicalDocumentStatus.Rejected, "Anne Staff", new string('n', 256)));

        // The context is still usable after both refusals.
        _clock.Advance(TimeSpan.FromHours(1));
        await _writer.TouchAsync(doc);
        var stored = await Stored(doc.Id);
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.PendingReview));
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0.AddHours(1)));
    }

    [Test]
    public async Task Md5_collision_with_another_chemical_is_a_collision_and_keeps_the_blob()
    {
        // Copilot #175: blob keys are global (md5), so the SHA-256 check must be too.
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var first = (await _writer.WriteAsync(BmdLabel(pdf))).Document!;
        first.ContentSha256 = new string('b', 64); // stands in for different bytes with the same md5
        await DbContext.SaveChangesAsync();
        var blobBefore = _blobs.Blobs[$"{Md5Of(pdf)}.pdf"];

        await Assert.ThrowsAsync(Is.InstanceOf<InvalidOperationException>(),
            () => _writer.WriteAsync(BmdLabel(pdf, chemicalId: ChemB)));

        Assert.That(_blobs.Blobs[$"{Md5Of(pdf)}.pdf"], Is.SameAs(blobBefore));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Existing_blob_with_other_bytes_under_the_md5_key_is_a_collision()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var other = TestPdf.Bytes("Roundup etiket"); // stands in for different bytes with the same md5
        _blobs.Blobs[$"{Md5Of(pdf)}.pdf"] = other;

        await Assert.ThrowsAsync(Is.InstanceOf<InvalidOperationException>(), () => _writer.WriteAsync(BmdLabel(pdf)));

        Assert.That(_blobs.Blobs[$"{Md5Of(pdf)}.pdf"], Is.SameAs(other), "never overwritten");
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Sha256_mismatch_on_md5_hit_is_a_collision()
    {
        var pdf = TestPdf.Bytes("Bumper 25 EC etiket");
        var first = (await _writer.WriteAsync(BmdLabel(pdf))).Document!;
        // Stands in for different bytes with the same md5.
        first.ContentSha256 = new string('b', 64);
        await DbContext.SaveChangesAsync();

        _clock.Advance(TimeSpan.FromHours(1));
        var ex = await Assert.ThrowsAsync(Is.InstanceOf<InvalidOperationException>(),
            () => _writer.WriteAsync(BmdLabel(pdf, sourceUrl: "https://holder.example/other.pdf")));
        Assert.That(ex!.Message, Does.Contain("SHA-256"));
        await Assert.ThrowsAsync(Is.InstanceOf<InvalidOperationException>(), () => _writer.WriteAsync(BmdLabel(pdf), dryRun: true),
            "dry run");

        var stored = await Stored(first.Id);
        Assert.That(stored.SourceUrl, Is.Null);
        Assert.That(stored.LastSeenAt, Is.EqualTo(T0));
        Assert.That(await DbContext.ChemicalDocuments.CountAsync(), Is.EqualTo(1));
    }

    // One authorization gate, one test per public entry point: automation (no named reviewer) never makes a row
    // Active and never changes a hands-off (Staff/StaffLocked) row beyond the F27 bookkeeping.

    [Test]
    public async Task WriteAsync_gate_claimed_staff_source_without_reviewer_is_automation()
    {
        var pdf = TestPdf.Bytes("Forkert produkt");
        var first = (await _writer.WriteAsync(BmdLabel(pdf))).Document!;
        await _writer.SetStatusAsync(first, ChemicalDocumentStatus.Rejected, "Anne Staff", "wrong product");
        await first.Delete(DbContext);

        await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(), () => _writer.WriteAsync(new(ChemB,
            ChemicalDocumentType.Label, ChemicalDocumentSource.Staff, ChemicalDocumentStatus.Active, pdf)), "create Active");

        // Source=Staff alone is not a staff action: it neither revives a hands-off row nor fills its language.
        var seen = await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff,
            ChemicalDocumentStatus.PendingReview, pdf, Language: "da"));
        Assert.That(seen.Outcome, Is.EqualTo(WriteOutcome.AlreadyKnown));
        Assert.That(seen.IsRemoved, Is.True);
        var stored = await Stored(first.Id);
        Assert.That(stored.WorkflowState, Is.EqualTo(WorkflowStates.Removed));
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Rejected));
        Assert.That(stored.Language, Is.Null);
        Assert.That(stored.Source, Is.EqualTo(ChemicalDocumentSource.Bmd));
    }

    [Test]
    public async Task SetStatusAsync_gate_blank_reviewer_is_automation()
    {
        var pending = (await _writer.WriteAsync(BmdLabel(TestPdf.Bytes("Afventer")))).Document!;
        var locked = (await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Bmd,
            ChemicalDocumentStatus.Active, TestPdf.Bytes("Godkendt"), ReviewedBy: "Anne Staff"))).Document!;

        await Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(),
            () => _writer.SetStatusAsync(pending, ChemicalDocumentStatus.Active, "   ", null), "Active");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _writer.SetStatusAsync(locked, ChemicalDocumentStatus.Rejected, "", "no"), "hands-off");

        Assert.That((await Stored(pending.Id)).Status, Is.EqualTo(ChemicalDocumentStatus.PendingReview));
        var storedLocked = await Stored(locked.Id);
        Assert.That(storedLocked.Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(storedLocked.ReviewedBy, Is.EqualTo("Anne Staff"));
        Assert.That(storedLocked.ReviewNote, Is.Null);
    }

    [Test]
    public async Task TouchAsync_gate_changes_only_bookkeeping_on_hands_off_row()
    {
        var locked = (await _writer.WriteAsync(new(ChemA, ChemicalDocumentType.Label, ChemicalDocumentSource.Staff,
            ChemicalDocumentStatus.Active, TestPdf.Bytes("Staff etiket"), ReviewedBy: "Anne Staff"))).Document!;
        var before = await Stored(locked.Id);

        _clock.Advance(TimeSpan.FromHours(1));
        await _writer.TouchAsync(locked, @checked: true);

        var after = await Stored(locked.Id);
        Assert.That(after.LastSeenAt, Is.EqualTo(T0.AddHours(1)));
        Assert.That(after.LastCheckedAt, Is.EqualTo(T0.AddHours(1)));
        Assert.That((after.Status, after.Source, after.Type, after.StaffLocked, after.Language, after.ProductId),
            Is.EqualTo((before.Status, before.Source, before.Type, before.StaffLocked, before.Language, before.ProductId)));
        Assert.That((after.ReviewedBy, after.ReviewedAt, after.ReviewNote, after.WorkflowState),
            Is.EqualTo((before.ReviewedBy, before.ReviewedAt, before.ReviewNote, before.WorkflowState)));
    }

    private sealed class ManualClock(DateTime start) : TimeProvider
    {
        private DateTimeOffset _now = new(start, TimeSpan.Zero);

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
