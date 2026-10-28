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
using System.Text.RegularExpressions;
using ChemicalsBase.Infrastructure.Documents;
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

        await Assert.ThrowsAsync<DbUpdateException>(async () => await duplicate.Create(DbContext));
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
        await Assert.ThrowsAsync<DbUpdateException>(async () => await duplicate.Create(DbContext));
    }

    [Test]
    public async Task Soft_deleted_document_still_blocks_a_second_create_with_same_content()
    {
        var document = BmdLabel();
        await document.Create(DbContext);
        await document.Delete(DbContext);

        var stored = await DbContext.ChemicalDocuments.AsNoTracking().SingleAsync();
        Assert.That(stored.WorkflowState, Is.EqualTo(ChemicalsBase.Infrastructure.Constants.Constants.WorkflowStates.Removed));

        // The unique index is not filtered on WorkflowState: a removed row is revived by Update, never re-created.
        await Assert.ThrowsAsync<DbUpdateException>(async () => await BmdLabel().Create(DbContext));
    }

    [Test]
    public async Task Language_roundtrips_and_is_versioned()
    {
        var document = BmdLabel();
        document.Type = ChemicalDocumentType.Sds;
        document.Language = "da";
        await document.Create(DbContext);

        document.Language = "en";
        await document.Update(DbContext);

        var stored = await DbContext.ChemicalDocuments.AsNoTracking().SingleAsync();
        Assert.That(stored.Language, Is.EqualTo("en"));

        var versions = await DbContext.ChemicalDocumentVersions.AsNoTracking()
            .Where(v => v.ChemicalDocumentId == stored.Id).OrderBy(v => v.Version).ToListAsync();
        Assert.That(versions.Select(v => v.Language), Is.EqualTo(new[] { "da", "en" }));

        // null = unknown language.
        var unknown = BmdLabel(md5: "fedcba9876543210fedcba9876543210");
        await unknown.Create(DbContext);
        var storedUnknown = await DbContext.ChemicalDocuments.AsNoTracking().SingleAsync(d => d.Id == unknown.Id);
        Assert.That(storedUnknown.Language, Is.Null);

        var index = DbContext.Model.FindEntityType(typeof(ChemicalDocument))!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "ChemicalId", "Type", "Language", "Status" }));
        Assert.That(index, Is.Not.Null, "index (ChemicalId, Type, Language, Status)");
    }

    [Test]
    public async Task SdsDocuments_not_mapped()
    {
        var entityType = DbContext.Model.FindEntityType(typeof(Chemical))!;
        Assert.That(entityType.FindProperty("SdsDocuments"), Is.Null);
        Assert.That(entityType.FindNavigation("SdsDocuments"), Is.Null);

        var chemical = NewChemical();
        chemical.SdsDocuments = new List<ChemicalSdsFeedItem>
        {
            new()
            {
                Language = "da",
                FileName = Md5,
                RevisionDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                ProductName = "Testmiddel 5 L",
            },
            new() { Language = "en", FileName = "fedcba9876543210fedcba9876543210" },
        };
        await chemical.Create(DbContext);

        var versions = await DbContext.ChemicalVersions.AsNoTracking()
            .Where(v => v.ChemicalId == chemical.Id).ToListAsync();
        Assert.That(versions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SdsHarvestAttempt_language_roundtrips_and_is_indexed()
    {
        DbContext.SdsHarvestAttempts.Add(new SdsHarvestAttempt
        {
            ChemicalId = 42,
            Language = "de",
            AttemptedAt = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc),
            Outcome = "no-hit",
        });
        await DbContext.SaveChangesAsync();

        var stored = await DbContext.SdsHarvestAttempts.AsNoTracking().SingleAsync();
        Assert.That(stored.Language, Is.EqualTo("de"));

        var indexes = DbContext.Model.FindEntityType(typeof(SdsHarvestAttempt))!.GetIndexes()
            .Select(i => string.Join(",", i.Properties.Select(p => p.Name))).ToList();
        Assert.That(indexes, Does.Contain("ChemicalId,Language,AttemptedAt"));
        Assert.That(indexes, Does.Not.Contain("ChemicalId,AttemptedAt"));
    }

    [Test]
    public async Task HolderSdsSite_languages_roundtrip_and_are_versioned()
    {
        var site = new HolderSdsSite
        {
            AuthorisationHolderId = 5,
            Domain = "cidlines.com",
            LanguagesOffered = "da,en",
            LanguageUrlPattern = "/{lang}/",
        };
        await site.Create(DbContext);

        site.LanguagesOffered = "da,en,de";
        await site.Update(DbContext);

        var stored = await DbContext.HolderSdsSites.AsNoTracking().SingleAsync();
        Assert.That(stored.LanguagesOffered, Is.EqualTo("da,en,de"));
        Assert.That(stored.LanguageUrlPattern, Is.EqualTo("/{lang}/"));

        var versions = await DbContext.HolderSdsSiteVersions.AsNoTracking()
            .Where(v => v.HolderSdsSiteId == stored.Id).OrderBy(v => v.Version).ToListAsync();
        Assert.That(versions.Select(v => v.LanguagesOffered), Is.EqualTo(new[] { "da,en", "da,en,de" }));
        Assert.That(versions.Select(v => v.LanguageUrlPattern), Is.All.EqualTo("/{lang}/"));
    }

    [Test]
    public async Task DocumentInboxItem_roundtrips_and_is_versioned()
    {
        var item = new DocumentInboxItem
        {
            BatchId = "6f1c2a9e-3b4d-4e5f-8a7b-9c0d1e2f3a4b",
            OriginalFileName = "Rodalon_SDS_DK.pdf",
            ContentMd5 = Md5,
            SizeBytes = 204800,
            DetectedType = ChemicalDocumentType.Sds,
            DetectedLanguage = "da",
            LanguageConfidence = 0.92,
            MatchedChemicalId = 42,
            ChemicalDocumentId = 77,
            MatchScore = 85,
            MatchNotes = "regno=yes; name=yes; sugg=12,57,903",
            Status = DocumentInboxStatus.Unmatched,
            UploadedBy = "producer@example.com",
            Note = "Nye SDS for 2026",
        };
        await item.Create(DbContext);

        item.Status = DocumentInboxStatus.Matched;
        item.ResolvedBy = "Anne Staff";
        item.ResolvedAt = new DateTime(2026, 10, 27, 14, 0, 0, DateTimeKind.Utc);
        item.ResolutionNote = "matched on reg. no.";
        await item.Update(DbContext);

        var stored = await DbContext.DocumentInboxItems.AsNoTracking().SingleAsync();
        Assert.That(stored.BatchId, Is.EqualTo("6f1c2a9e-3b4d-4e5f-8a7b-9c0d1e2f3a4b"));
        Assert.That(stored.OriginalFileName, Is.EqualTo("Rodalon_SDS_DK.pdf"));
        Assert.That(stored.ContentMd5, Is.EqualTo(Md5));
        Assert.That(stored.SizeBytes, Is.EqualTo(204800));
        Assert.That(stored.DetectedType, Is.EqualTo(ChemicalDocumentType.Sds));
        Assert.That(stored.DetectedLanguage, Is.EqualTo("da"));
        Assert.That(stored.LanguageConfidence, Is.EqualTo(0.92).Within(1e-9));
        Assert.That(stored.MatchedChemicalId, Is.EqualTo(42));
        Assert.That(stored.ChemicalDocumentId, Is.EqualTo(77));
        Assert.That(stored.MatchScore, Is.EqualTo(85));
        Assert.That(stored.MatchNotes, Is.EqualTo("regno=yes; name=yes; sugg=12,57,903"));
        Assert.That(stored.Status, Is.EqualTo(DocumentInboxStatus.Matched));
        Assert.That(stored.UploadedBy, Is.EqualTo("producer@example.com"));
        Assert.That(stored.ResolvedBy, Is.EqualTo("Anne Staff"));
        Assert.That(stored.ResolvedAt, Is.EqualTo(new DateTime(2026, 10, 27, 14, 0, 0)));
        Assert.That(stored.Note, Is.EqualTo("Nye SDS for 2026"));
        Assert.That(stored.ResolutionNote, Is.EqualTo("matched on reg. no."));
        Assert.That(stored.DuplicateOfInboxItemId, Is.Null);

        var versions = await DbContext.DocumentInboxItemVersions.AsNoTracking()
            .Where(v => v.DocumentInboxItemId == stored.Id).OrderBy(v => v.Version).ToListAsync();
        Assert.That(versions.Select(v => v.Status),
            Is.EqualTo(new[] { DocumentInboxStatus.Unmatched, DocumentInboxStatus.Matched }));
        Assert.That(versions.Select(v => v.ContentMd5), Is.All.EqualTo(Md5));
        Assert.That(versions[1].ResolvedBy, Is.EqualTo("Anne Staff"));
        Assert.That(versions.Select(v => v.Note), Is.All.EqualTo("Nye SDS for 2026"));
        Assert.That(versions.Select(v => v.ResolutionNote), Is.EqualTo(new[] { null, "matched on reg. no." }));

        // A second upload of the same file links back to the earlier inbox row.
        var duplicate = new DocumentInboxItem
        {
            BatchId = "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
            OriginalFileName = "Rodalon_SDS_DK (1).pdf",
            ContentMd5 = Md5,
            SizeBytes = 204800,
            Status = DocumentInboxStatus.Duplicate,
            DuplicateOfInboxItemId = item.Id,
            UploadedBy = "producer@example.com",
        };
        await duplicate.Create(DbContext);
        var storedDuplicate = await DbContext.DocumentInboxItems.AsNoTracking().SingleAsync(i => i.Id == duplicate.Id);
        Assert.That(storedDuplicate.DuplicateOfInboxItemId, Is.EqualTo(item.Id));
        var duplicateVersion = await DbContext.DocumentInboxItemVersions.AsNoTracking()
            .SingleAsync(v => v.DocumentInboxItemId == duplicate.Id);
        Assert.That(duplicateVersion.DuplicateOfInboxItemId, Is.EqualTo(item.Id));

        // A NotPdf entry has no blob: empty md5, no detection.
        var notPdf = new DocumentInboxItem
        {
            BatchId = item.BatchId,
            OriginalFileName = "readme.txt",
            Status = DocumentInboxStatus.NotPdf,
            UploadedBy = "producer@example.com",
        };
        await notPdf.Create(DbContext);
        var storedNotPdf = await DbContext.DocumentInboxItems.AsNoTracking().SingleAsync(i => i.Id == notPdf.Id);
        Assert.That(storedNotPdf.ContentMd5, Is.EqualTo(""));
        Assert.That(storedNotPdf.DetectedType, Is.Null);

        var indexes = DbContext.Model.FindEntityType(typeof(DocumentInboxItem))!.GetIndexes()
            .Select(i => string.Join(",", i.Properties.Select(p => p.Name))).ToList();
        Assert.That(indexes, Does.Contain("Status,BatchId"));
        Assert.That(indexes, Does.Contain("ContentMd5"));
    }

    [Test]
    public void DocumentInboxStatus_values_are_pinned()
    {
        Assert.That((int)DocumentInboxStatus.Unmatched, Is.EqualTo(1));
        Assert.That((int)DocumentInboxStatus.Matched, Is.EqualTo(2));
        Assert.That((int)DocumentInboxStatus.Duplicate, Is.EqualTo(3));
        Assert.That((int)DocumentInboxStatus.Rejected, Is.EqualTo(4));
        Assert.That((int)DocumentInboxStatus.NotPdf, Is.EqualTo(5));
    }
}

/// <summary>Pure logic (no database): language codes, Annex II headings, detection, SDS file names.</summary>
[TestFixture]
public class SdsLanguageTests
{
    private const string DanishProse =
        "Produktet skal opbevares utilgængeligt for børn. Undgå kontakt med øjne og hud. Ved indånding: flyt personen ud i frisk luft. " +
        "Ved indtagelse: skyl munden med vand og kontakt omgående en læge. Brug beskyttelseshandsker og øjenbeskyttelse. " +
        "Blandingen indeholder ingen stoffer, der er nødvendigt at angive. Oplysninger om fysiske og kemiske egenskaber findes i punkt 9. " +
        "Opbevares i original emballage, uden adgang for uvedkommende, mellem 5 og 25 grader. Nogle af indholdsstofferne kan være skadelige for sundhed. " +
        "Virksomheden er ansvarlig for oplysninger i dette sikkerhedsdatablad. Hvad skal man gøre ved uheld? Skyl gennem længere tid med vand. " +
        "Hænder vaskes efter brug. Beskyt mod frost.";

    private const string EnglishProse =
        "Keep out of the reach of children. Avoid contact with eyes and skin. If inhaled, remove the person to fresh air. " +
        "If swallowed, rinse mouth with water and call a doctor. Wear protective gloves. The mixture does not contain any substances " +
        "which are required to be listed. This product should be stored in the original container and has been classified by the supplier. " +
        "Wash hands after handling. Other information may be found in section 16. Storage: keep away from heat. " +
        "Such handling of the product is not recommended.";

    private const string IcelandicProse =
        "Geymist þar sem börn ná ekki til. Forðist snertingu við augu og húð. Við innöndun: flytjið viðkomandi í ferskt loft. " +
        "Ef efnið er gleypt skal skola munninn með vatni og hafa samband við lækni. Notið hlífðarhanska. " +
        "Blöndu þessa skal geyma í upprunalegum umbúðum. Upplýsingar um hættu eru fyrir hendi í þessu öryggisblað. " +
        "Fyrirtækið hefur samsetning efnisins skráða. Notkun: aðeins fyrir fagmenn. Það getur verið hættulegt að anda að sér gufum frá efninu. " +
        "Eftir notkun skal þvo hendur. Varúðarráðstafanir eru taldar upp þegar efnið er flutt eða meðhöndlað.";

    [TestCase("DA", "da")]
    [TestCase("da", "da")]
    [TestCase(" en ", "en")]
    [TestCase("nb", "no")]
    [TestCase("nn", "no")]
    [TestCase("nb-NO", "no")]
    [TestCase("pt-BR", "pt")]
    [TestCase("de_AT", "de")]
    [TestCase("xx", null)]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void Normalize_cases(string? code, string? expected)
    {
        Assert.That(SdsLanguages.Normalize(code), Is.EqualTo(expected));
    }

    [Test]
    public void Language_lists_and_attempt_default_are_pinned()
    {
        Assert.That(SdsLanguages.Phase1, Is.EqualTo(new[] { "da", "en" }));
        Assert.That(SdsLanguages.DefaultPhase2, Is.EqualTo(new[]
        {
            "bg", "cs", "de", "el", "es", "et", "fi", "fr", "hr", "hu", "is", "it", "lt", "lv", "nl", "no", "pl", "pt", "ro", "sk", "sl", "sv",
        }));
        Assert.That(SdsLanguages.DefaultAttemptsBeforeExhausted, Is.EqualTo(3));
    }

    [Test]
    public void Headings_resource_loads_all_phase_languages()
    {
        var wanted = SdsLanguages.Phase1.Concat(SdsLanguages.DefaultPhase2).ToList();
        var missing = wanted.Where(code => SdsHeadings.For(code) == null).ToList();
        Assert.That(missing, Is.Empty, $"languages missing from annex-ii-headings.json: {string.Join(", ", missing)}");
        Assert.That(SdsHeadings.All.Keys, Is.SubsetOf(wanted));

        // Icelandic has no quotable Annex II text: it loads, but is listed as headings-unavailable.
        Assert.That(SdsHeadings.HeadingsUnavailable, Is.EqualTo(new[] { "is" }));
        var icelandic = SdsHeadings.For("is")!;
        Assert.That(icelandic.HasHeadings, Is.False);
        Assert.That(icelandic.SectionWord, Is.Null);
        Assert.That(icelandic.HeadingPattern, Is.Null);
        Assert.That(icelandic.Section1Word, Is.Null);
        Assert.That(icelandic.Section1Title, Is.Null);
        Assert.That(icelandic.SdsTerms, Does.Contain("Öryggisblað"));

        Assert.Multiple(() =>
        {
            foreach (var (code, terms) in SdsHeadings.All)
            {
                Assert.That(terms.SdsTerms, Is.Not.Empty, $"{code}: sds terms");
                Assert.That(terms.RevisionKeywords, Is.Not.Empty, $"{code}: revision keywords");
                Assert.That(terms.Months, Has.Count.EqualTo(12), $"{code}: months");
                if (SdsHeadings.HeadingsUnavailable.Contains(code))
                {
                    continue;
                }
                Assert.That(terms.HasHeadings, Is.True, $"{code}: has headings");
                Assert.That(terms.SectionWord, Is.Not.Empty, $"{code}: section word");
                Assert.That(terms.HeadingPattern, Does.Contain("{n}"), $"{code}: heading pattern");
                Assert.That(terms.Section1Title, Is.Not.Empty, $"{code}: section 1 title");
                Assert.That(terms.Section1Word, Does.Contain("1").And.Contain(terms.SectionWord!), $"{code}: section 1 word");
                Assert.That(terms.Section2Word, Does.Contain("2"), $"{code}: section 2 word");
            }
        });

        var cs = SdsHeadings.For("cs")!;
        Assert.That(cs.MonthsGenitive, Has.Count.EqualTo(12), "cs: genitive months");
        Assert.That(SdsHeadings.For("da")!.MonthsGenitive, Is.Null, "da: no genitive months");
        Assert.That(SdsHeadings.For("hu")!.HeadingPattern, Is.EqualTo("{n}. SZAKASZ: "));
        Assert.That(SdsHeadings.For("hu")!.SectionWord, Is.EqualTo("SZAKASZ"));

        var da = SdsHeadings.For("DA")!;
        Assert.That(da.Section1Word, Is.EqualTo("PUNKT 1"));
        Assert.That(da.Section2Word, Is.EqualTo("PUNKT 2"));
        Assert.That(da.Section1Title, Is.EqualTo("Identifikation af stoffet/blandingen og af selskabet/virksomheden"));
        Assert.That(da.SdsTerms, Does.Contain("Sikkerhedsdatablad"));
        Assert.That(da.Months, Does.Contain("marts"));
        Assert.That(SdsHeadings.For("hu")!.Section1Word, Is.EqualTo("1. SZAKASZ"));
        Assert.That(SdsHeadings.For("sv")!.Section1Word, Is.EqualTo("AVSNITT 1"));
        Assert.That(SdsHeadings.For("no")!.Section1Word, Is.EqualTo("AVSNITT 1"));
        Assert.That(SdsHeadings.For("xx"), Is.Null);
    }

    [Test]
    public void Language_detection_cases()
    {
        Assert.Multiple(() =>
        {
            // Headings: every language with Annex II headings in the resource is recognised from its section-1 heading alone
            // ("is" is skipped: it is headings-unavailable, see Headings_resource_loads_all_phase_languages).
            Assert.That(SdsHeadings.All.Keys.Where(code => !SdsHeadings.All[code].HasHeadings), Is.EqualTo(new[] { "is" }));
            foreach (var (code, terms) in SdsHeadings.All.Where(kv => kv.Value.HasHeadings))
            {
                var heading = terms.HeadingPattern!.Replace("{n}", "1") + terms.Section1Title;
                foreach (var text in new[] { heading, heading.ToUpperInvariant(), $"Side 1 af 9\n  {heading.Replace(" ", "\n ")}" })
                {
                    var detected = SdsLanguageDetector.Detect(text);
                    Assert.That(detected.Language, Is.EqualTo(code), $"{code}: {text}");
                    Assert.That(detected.Confidence, Is.EqualTo(1.0), $"{code}: confidence");
                    Assert.That(detected.Uncertain, Is.False, $"{code}: uncertain");
                }
            }

            // sv and no share "AVSNITT"; the title words after it tell them apart, also for supplier variants of the title.
            Assert.That(SdsLanguageDetector.Detect("AVSNITT 1: Namnet på ämnet/blandningen och företaget").Language, Is.EqualTo("sv"));
            Assert.That(SdsLanguageDetector.Detect("AVSNITT 1: Identifikasjon av stoffet/stoffblandingen og av leverandøren").Language, Is.EqualTo("no"));
            Assert.That(SdsLanguageDetector.Detect("AVSNITT 1 Namnet på ämnet/blandningen och bolaget/företaget").Language, Is.EqualTo("sv"));
            Assert.That(SdsLanguageDetector.Detect("AVSNITT 1 Identifikasjon av stoffet/stoffblandingen og av selskapet/foretaket").Language, Is.EqualTo("no"));

            // No headings: stop-word profiles.
            var danish = SdsLanguageDetector.Detect(DanishProse);
            Assert.That(danish.Language, Is.EqualTo("da"));
            Assert.That(danish.Confidence, Is.GreaterThanOrEqualTo(0.6));
            Assert.That(danish.Uncertain, Is.False);

            var english = SdsLanguageDetector.Detect(EnglishProse);
            Assert.That(english.Language, Is.EqualTo("en"));
            Assert.That(english.Uncertain, Is.False);

            // Bilingual da/en: the top language is still reported, but flagged.
            var bilingual = SdsLanguageDetector.Detect(DanishProse + " " + EnglishProse);
            Assert.That(bilingual.Language, Is.AnyOf("da", "en"));
            Assert.That(bilingual.Uncertain, Is.True, "bilingual");

            // A label-like short text has too few profile hits.
            var label = SdsLanguageDetector.Detect(
                "Rodalon Indendørs 1 L. Indeholder benzalkoniumchlorid. Fare: Kan forårsage øjenirritation. Reg.nr. 1-234.");
            Assert.That(label.Language, Is.Null);
            Assert.That(label.Confidence, Is.EqualTo(0));
            Assert.That(label.Uncertain, Is.True);

            Assert.That(SdsLanguageDetector.Detect("").Language, Is.Null);
        });
    }

    [Test]
    public void Icelandic_has_no_headings_so_detection_falls_back_to_profile_and_is_uncertain()
    {
        var detected = SdsLanguageDetector.Detect(IcelandicProse);
        Assert.That(detected.Language, Is.EqualTo("is"));
        Assert.That(detected.Confidence, Is.LessThan(0.6));
        Assert.That(detected.Uncertain, Is.True);
    }

    [TestCase("PUNKT 1: Identifikation af stoffet/ blandingen og af selskabet/ virksomheden", "da")]
    [TestCase("PUNKT 1: Identifikation af stoffet / præparatet og af selskabet/virksomheden", "da")]
    [TestCase("AVSNITT 1: Identifikasjon av stoffet/ blandingen og av selskapet", "no")]
    [TestCase("AVSNITT 1: Namnet på ämnet/ beredningen och bolaget", "sv")]
    [TestCase("SECTION 1: Identification of the substance/ preparation and of the company/ undertaking", "en")]
    public void Heading_rule_tolerates_spacing_around_slash_and_older_titles(string text, string language)
    {
        var detected = SdsLanguageDetector.Detect(text);
        Assert.That(detected.Language, Is.EqualTo(language));
        Assert.That(detected.Confidence, Is.EqualTo(1.0));
        Assert.That(detected.Uncertain, Is.False);
    }

    // Real page 1-2 text (Fixtures/SdsText/SOURCES.txt says where each comes from).
    [TestCase("da-7_1.txt", "da")]
    [TestCase("da-52_1.txt", "da")]
    [TestCase("de-1_1.txt", "de")]
    [TestCase("en-1_2.txt", "en")]
    public void Real_sds_text_is_detected_by_its_heading(string file, string language)
    {
        var detected = SdsLanguageDetector.Detect(Fixture(file));
        Assert.That(detected.Language, Is.EqualTo(language));
        Assert.That(detected.Confidence, Is.EqualTo(1.0), "1.0 only comes from the heading rule");
        Assert.That(detected.Uncertain, Is.False);
    }

    // Without the section-1 heading line the profiles must carry it alone, with no other language's words on the page.
    [TestCase("da-7_1.txt", "da")]
    [TestCase("de-1_1.txt", "de")]
    [TestCase("en-1_2.txt", "en")]
    public void Real_sds_text_without_heading_is_detected_by_profile(string file, string language)
    {
        var detected = SdsLanguageDetector.Detect(WithoutSection1Heading(Fixture(file), language));
        Assert.That(detected.Language, Is.EqualTo(language));
        Assert.That(detected.Confidence, Is.EqualTo(1.0), "no other language's profile words on these pages");
        Assert.That(detected.Uncertain, Is.False);
    }

    [Test]
    public void Real_short_upper_case_page_without_heading_is_unknown()
    {
        // 52_1's first pages hold fewer than 20 profile words: without its heading the language is unknown (safe).
        var detected = SdsLanguageDetector.Detect(WithoutSection1Heading(Fixture("da-52_1.txt"), "da"));
        Assert.That(detected.Language, Is.Null);
        Assert.That(detected.Confidence, Is.EqualTo(0));
        Assert.That(detected.Uncertain, Is.True);
    }

    [Test]
    public void Every_profile_word_belongs_to_one_language()
    {
        Assert.That(SdsLanguageDetector.ProfileWords.Keys,
            Is.EquivalentTo(SdsLanguages.Phase1.Concat(SdsLanguages.DefaultPhase2)));

        var shared = SdsLanguageDetector.ProfileWords
            .SelectMany(kv => kv.Value.Select(word => (word, code: kv.Key)))
            .GroupBy(x => x.word)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join("/", g.Select(x => x.code))}")
            .ToList();
        Assert.That(shared, Is.Empty, "words in two profiles");

        Assert.Multiple(() =>
        {
            foreach (var (code, words) in SdsLanguageDetector.ProfileWords)
            {
                Assert.That(words, Has.Count.InRange(24, 40), $"{code}: about 30 words");
                Assert.That(words, Is.Unique, $"{code}: no repeats");
            }
        });
    }

    [TestCase("da-7_1.txt", "da")]
    [TestCase("da-52_1.txt", "da")]
    [TestCase("de-1_1.txt", "de")]
    [TestCase("en-1_2.txt", "en")]
    public void Real_sds_pages_hit_no_other_language_profile(string file, string language)
    {
        var hits = SdsLanguageDetector.ProfileHits(Fixture(file));
        Assert.That(hits[language], Is.GreaterThan(0));
        Assert.That(hits.Where(kv => kv.Key != language && kv.Value > 0).Select(kv => $"{kv.Key}={kv.Value}"),
            Is.Empty, "foreign profile hits (e.g. fr 'des' on German, it 'alle' / fi 'kun' on Danish)");
    }

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "SdsText", name));

    private static string WithoutSection1Heading(string text, string language)
    {
        var heading = new Regex($@"^\s*{Regex.Escape(SdsHeadings.For(language)!.Section1Word!)}(?!\d)", RegexOptions.IgnoreCase);
        var lines = text.Split('\n');
        var kept = lines.Where(line => !heading.IsMatch(line)).ToList();
        Assert.That(kept, Has.Count.EqualTo(lines.Length - 1), "exactly one section-1 heading line removed");
        return string.Join("\n", kept);
    }

    [TestCase(null, true)]
    [TestCase("", true)]
    [TestCase("  ", true)]
    [TestCase("d41d8cd98f00b204e9800998ecf8427e", true)]
    [TestCase("0cc175b9c0f1b6a831c399e269772661", false)]
    public void SdsFileNames_is_missing_cases(string? fileName, bool missing)
    {
        Assert.That(SdsFileNames.IsMissing(fileName), Is.EqualTo(missing));
        Assert.That(SdsFileNames.EmptyContentMd5, Is.EqualTo("d41d8cd98f00b204e9800998ecf8427e"));
    }
}
