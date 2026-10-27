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
using NUnit.Framework;

namespace ChemicalBase.Tests;

[TestFixture]
public class ChemicalDocumentTests : DbTestFixture
{
    private const string Md5 = "0123456789abcdef0123456789abcdef";

    private static ChemicalDocument BmdLabel(int chemicalId = 42, string md5 = Md5) => new()
    {
        ChemicalId = chemicalId,
        ProductId = 7,
        Type = ChemicalDocumentType.Label,
        Source = ChemicalDocumentSource.Bmd,
        Status = ChemicalDocumentStatus.Active,
        ContentMd5 = md5,
        ContentSha256 = new string('a', 64),
        SizeBytes = 123456,
        SourceUrl = "https://bmd.example/DownloadExternal?id=1",
        SourceFileId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
        SourceFileName = "etiket.pdf",
        SourcePackaging = "5 L, 4x5 L & 10 L",
        SourceProductId = 98765,
        SourceAdapter = "bmd",
        SourceRevisionDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        SourceRevision = "III",
        RetrievedAt = new DateTime(2026, 10, 26, 8, 0, 0, DateTimeKind.Utc),
        LastSeenAt = new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc),
        LastCheckedAt = new DateTime(2026, 10, 26, 10, 0, 0, DateTimeKind.Utc),
        ValidationScore = 87,
        ValidationNotes = "sds=yes; lang=da(0.92); id=regno; rev=2024-03-01",
        StaffLocked = true,
        ReviewedBy = "Anne Staff",
        ReviewedAt = new DateTime(2026, 10, 26, 11, 0, 0, DateTimeKind.Utc),
        ReviewNote = "ok",
    };

    private static Chemical NewChemical() => new()
    {
        Name = "Testmiddel",
        RegistrationNo = "1-234",
        RemoteId = "remote-1",
        BiocideUser = new List<int>(),
        PesticideProductGroup = new List<int>(),
        BiocideProductType = new List<int>(),
        PesticidePossibleUse = new List<int>(),
        BiocidePossibleUse = new List<int>(),
        BiocideSpecialUse = new List<int>(),
        BarcodeValue = new List<string>(),
        ClassificationAndLabeling = new ClassificationAndLabeling
        {
            CLP = new CLP(),
            DPD = new DPD { RiskPhrases = new List<int>() },
        },
    };

    [Test]
    public async Task Create_writes_row_and_version_with_enums_and_provenance()
    {
        await BmdLabel().Create(DbContext);

        var stored = await DbContext.ChemicalDocuments.AsNoTracking().SingleAsync();
        Assert.That(stored.ChemicalId, Is.EqualTo(42));
        Assert.That(stored.ProductId, Is.EqualTo(7));
        Assert.That(stored.Type, Is.EqualTo(ChemicalDocumentType.Label));
        Assert.That(stored.Source, Is.EqualTo(ChemicalDocumentSource.Bmd));
        Assert.That(stored.Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(stored.ContentMd5, Is.EqualTo(Md5));
        Assert.That(stored.ContentSha256, Is.EqualTo(new string('a', 64)));
        Assert.That(stored.SizeBytes, Is.EqualTo(123456));
        Assert.That(stored.SourceUrl, Is.EqualTo("https://bmd.example/DownloadExternal?id=1"));
        Assert.That(stored.SourceFileId, Is.EqualTo("3f2504e0-4f89-11d3-9a0c-0305e82c3301"));
        Assert.That(stored.SourceFileName, Is.EqualTo("etiket.pdf"));
        Assert.That(stored.SourcePackaging, Is.EqualTo("5 L, 4x5 L & 10 L"));
        Assert.That(stored.SourceProductId, Is.EqualTo(98765));
        Assert.That(stored.SourceAdapter, Is.EqualTo("bmd"));
        Assert.That(stored.SourceRevisionDate, Is.EqualTo(new DateTime(2024, 3, 1)));
        Assert.That(stored.SourceRevision, Is.EqualTo("III"));
        Assert.That(stored.RetrievedAt, Is.EqualTo(new DateTime(2026, 10, 26, 8, 0, 0)));
        Assert.That(stored.LastSeenAt, Is.EqualTo(new DateTime(2026, 10, 26, 9, 0, 0)));
        Assert.That(stored.LastCheckedAt, Is.EqualTo(new DateTime(2026, 10, 26, 10, 0, 0)));
        Assert.That(stored.ValidationScore, Is.EqualTo(87));
        Assert.That(stored.ValidationNotes, Is.EqualTo("sds=yes; lang=da(0.92); id=regno; rev=2024-03-01"));
        Assert.That(stored.StaffLocked, Is.True);
        Assert.That(stored.ReviewedBy, Is.EqualTo("Anne Staff"));
        Assert.That(stored.ReviewedAt, Is.EqualTo(new DateTime(2026, 10, 26, 11, 0, 0)));
        Assert.That(stored.ReviewNote, Is.EqualTo("ok"));
        Assert.That(stored.Version, Is.EqualTo(1));

        var versions = await DbContext.ChemicalDocumentVersions.AsNoTracking()
            .Where(v => v.ChemicalDocumentId == stored.Id).ToListAsync();
        Assert.That(versions, Has.Count.EqualTo(1));
        Assert.That(versions[0].Type, Is.EqualTo(ChemicalDocumentType.Label));
        Assert.That(versions[0].Status, Is.EqualTo(ChemicalDocumentStatus.Active));
        Assert.That(versions[0].Source, Is.EqualTo(ChemicalDocumentSource.Bmd));
        Assert.That(versions[0].SourceFileId, Is.EqualTo("3f2504e0-4f89-11d3-9a0c-0305e82c3301"));
        Assert.That(versions[0].ContentMd5, Is.EqualTo(Md5));
    }

    [Test]
    public async Task Unique_index_rejects_same_content_twice_for_same_chemical_and_type()
    {
        await BmdLabel().Create(DbContext);

        var duplicate = BmdLabel();
        duplicate.Source = ChemicalDocumentSource.Staff;
        duplicate.Status = ChemicalDocumentStatus.PendingReview;

        Assert.ThrowsAsync<DbUpdateException>(async () => await duplicate.Create(DbContext));
    }

    [Test]
    public async Task Chemical_labels_are_not_mapped()
    {
        var entityType = DbContext.Model.FindEntityType(typeof(Chemical))!;
        Assert.That(entityType.FindProperty("Labels"), Is.Null);
        Assert.That(entityType.FindNavigation("Labels"), Is.Null);

        var chemical = NewChemical();
        chemical.Labels = new List<ChemicalLabelFeedItem>
        {
            new()
            {
                Type = ChemicalDocumentType.Label,
                FileName = Md5,
                Packaging = "1 L",
                RevisionDate = new DateTime(2024, 3, 1),
                Revision = "II",
            },
        };
        await chemical.Create(DbContext);

        var versions = await DbContext.ChemicalVersions.AsNoTracking()
            .Where(v => v.ChemicalId == chemical.Id).ToListAsync();
        Assert.That(versions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AdditionalTradeNames_roundtrips_and_is_versioned()
    {
        var chemical = NewChemical();
        chemical.AdditionalTradeNames = "Foo Plus; Foo Extra";
        await chemical.Create(DbContext);

        chemical.AdditionalTradeNames = "Foo Plus; Foo Extra; Foo Pro";
        await chemical.Update(DbContext);

        var stored = await DbContext.Chemicals.AsNoTracking().SingleAsync(c => c.Id == chemical.Id);
        Assert.That(stored.AdditionalTradeNames, Is.EqualTo("Foo Plus; Foo Extra; Foo Pro"));

        var versions = await DbContext.ChemicalVersions.AsNoTracking()
            .Where(v => v.ChemicalId == chemical.Id).OrderBy(v => v.Version).ToListAsync();
        Assert.That(versions.Select(v => v.AdditionalTradeNames),
            Is.EqualTo(new[] { "Foo Plus; Foo Extra", "Foo Plus; Foo Extra; Foo Pro" }));
    }

    [Test]
    public async Task SdsHarvestAttempt_roundtrips()
    {
        var attemptedAt = new DateTime(2026, 10, 26, 12, 30, 0, DateTimeKind.Utc);
        DbContext.SdsHarvestAttempts.Add(new SdsHarvestAttempt
        {
            ChemicalId = 42,
            AttemptedAt = attemptedAt,
            Outcome = "candidates",
            Candidates = 3,
        });
        await DbContext.SaveChangesAsync();

        var stored = await DbContext.SdsHarvestAttempts.AsNoTracking().SingleAsync();
        Assert.That(stored.Id, Is.GreaterThan(0));
        Assert.That(stored.ChemicalId, Is.EqualTo(42));
        Assert.That(stored.AttemptedAt, Is.EqualTo(new DateTime(2026, 10, 26, 12, 30, 0)));
        Assert.That(stored.Outcome, Is.EqualTo("candidates"));
        Assert.That(stored.Candidates, Is.EqualTo(3));
    }

    [Test]
    public async Task HolderSdsSite_unique_per_holder_domain_and_versioned()
    {
        var site = new HolderSdsSite
        {
            AuthorisationHolderId = 5,
            Domain = "cidlines.com",
            State = HolderSiteState.Discovered,
            Strategies = HolderSiteStrategy.Sitemap | HolderSiteStrategy.SiteSearch,
            ListingPageUrl = "https://cidlines.com/sds",
            ProductPageUrlTemplate = "https://cidlines.com/produkt/{slug}",
            SiteSearchUrlTemplate = "https://cidlines.com/search?q={q}",
            LinkSelector = "a.sds[href$=\".pdf\"]",
            LastCrawledAt = new DateTime(2026, 10, 26, 13, 0, 0, DateTimeKind.Utc),
            CandidatesFound = 4,
            Note = "found via sitemap",
        };
        await site.Create(DbContext);

        site.State = HolderSiteState.StaffConfirmed;
        await site.Update(DbContext);

        var stored = await DbContext.HolderSdsSites.AsNoTracking().SingleAsync();
        Assert.That(stored.State, Is.EqualTo(HolderSiteState.StaffConfirmed));
        Assert.That(stored.Strategies, Is.EqualTo(HolderSiteStrategy.Sitemap | HolderSiteStrategy.SiteSearch));
        Assert.That(stored.ProductPageUrlTemplate, Is.EqualTo("https://cidlines.com/produkt/{slug}"));
        Assert.That(stored.LinkSelector, Is.EqualTo("a.sds[href$=\".pdf\"]"));
        Assert.That(stored.CandidatesFound, Is.EqualTo(4));

        var versions = await DbContext.HolderSdsSiteVersions.AsNoTracking()
            .Where(v => v.HolderSdsSiteId == stored.Id).OrderBy(v => v.Version).ToListAsync();
        Assert.That(versions.Select(v => v.State),
            Is.EqualTo(new[] { HolderSiteState.Discovered, HolderSiteState.StaffConfirmed }));
        Assert.That(versions.Select(v => v.Strategies),
            Is.All.EqualTo(HolderSiteStrategy.Sitemap | HolderSiteStrategy.SiteSearch));
        Assert.That(versions[0].Domain, Is.EqualTo("cidlines.com"));

        // Same domain for another holder is allowed.
        await new HolderSdsSite { AuthorisationHolderId = 6, Domain = "cidlines.com" }.Create(DbContext);

        var duplicate = new HolderSdsSite { AuthorisationHolderId = 5, Domain = "cidlines.com" };
        Assert.ThrowsAsync<DbUpdateException>(async () => await duplicate.Create(DbContext));
    }
}
