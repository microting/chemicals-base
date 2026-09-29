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
public class BarcodeSuggestionTests : DbTestFixture
{
    private static BarcodeSuggestion Pending(int? chemicalId = 11, int? productId = 12, string? photo = null) => new()
    {
        Barcode = "5701234567892",
        ChemicalId = chemicalId,
        ProductId = productId,
        CustomerNo = "482117",
        UserName = "Mette Larsen",
        UserEmail = "mette@example.com",
        LabelPhotoFileName = photo,
        Status = BarcodeSuggestionStatus.Pending,
    };

    [Test]
    public void StatusValues_ArePinned()
    {
        // Stored as int; changing a value would silently re-label rows already in production.
        Assert.That((int)BarcodeSuggestionStatus.Pending, Is.EqualTo(0));
        Assert.That((int)BarcodeSuggestionStatus.Approved, Is.EqualTo(1));
        Assert.That((int)BarcodeSuggestionStatus.Rejected, Is.EqualTo(2));
    }

    [Test]
    public async Task Create_StoresAPendingSuggestion_AndItsFirstVersion()
    {
        await Pending(photo: "barcode-suggestions/abc.jpg").Create(DbContext);

        var stored = await DbContext.BarcodeSuggestions.AsNoTracking().SingleAsync();
        Assert.That(stored.Barcode, Is.EqualTo("5701234567892"));
        Assert.That(stored.ChemicalId, Is.EqualTo(11));
        Assert.That(stored.ProductId, Is.EqualTo(12));
        Assert.That(stored.CustomerNo, Is.EqualTo("482117"));
        Assert.That(stored.UserName, Is.EqualTo("Mette Larsen"));
        Assert.That(stored.UserEmail, Is.EqualTo("mette@example.com"));
        Assert.That(stored.LabelPhotoFileName, Is.EqualTo("barcode-suggestions/abc.jpg"));
        Assert.That(stored.Status, Is.EqualTo(BarcodeSuggestionStatus.Pending));
        Assert.That(stored.Version, Is.EqualTo(1));

        var versions = await DbContext.BarcodeSuggestionVersions.AsNoTracking()
            .Where(v => v.BarcodeSuggestionId == stored.Id).ToListAsync();
        Assert.That(versions, Has.Count.EqualTo(1));
        Assert.That(versions[0].Barcode, Is.EqualTo("5701234567892"));
        Assert.That(versions[0].Status, Is.EqualTo(BarcodeSuggestionStatus.Pending));
    }

    [Test]
    public async Task Approve_StoresReviewFields_AndWritesASecondVersion()
    {
        var suggestion = Pending();
        await suggestion.Create(DbContext);
        var reviewedAt = new DateTime(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc);

        suggestion.Status = BarcodeSuggestionStatus.Approved;
        suggestion.ApprovedChemicalId = 11;
        suggestion.ApprovedProductId = 99;
        suggestion.ReviewedBy = "Anne Staff";
        suggestion.ReviewedAt = reviewedAt;
        await suggestion.Update(DbContext);

        var stored = await DbContext.BarcodeSuggestions.AsNoTracking().SingleAsync();
        Assert.That(stored.Status, Is.EqualTo(BarcodeSuggestionStatus.Approved));
        Assert.That(stored.ApprovedProductId, Is.EqualTo(99));
        Assert.That(stored.ReviewedBy, Is.EqualTo("Anne Staff"));
        Assert.That(stored.ReviewedAt, Is.EqualTo(reviewedAt));
        var versions = await DbContext.BarcodeSuggestionVersions.AsNoTracking()
            .Where(v => v.BarcodeSuggestionId == stored.Id).OrderBy(v => v.Version).ToListAsync();
        Assert.That(versions.Select(v => v.Status),
            Is.EqualTo(new[] { BarcodeSuggestionStatus.Pending, BarcodeSuggestionStatus.Approved }));
    }

    [Test]
    public async Task Reject_StoresTheReasonCode()
    {
        var suggestion = Pending();
        await suggestion.Create(DbContext);

        suggestion.Status = BarcodeSuggestionStatus.Rejected;
        suggestion.RejectReason = "WrongProduct";
        await suggestion.Update(DbContext);

        var stored = await DbContext.BarcodeSuggestions.AsNoTracking().SingleAsync();
        Assert.That(stored.RejectReason, Is.EqualTo("WrongProduct"));
    }

    [Test]
    public async Task PhotoOnlySuggestion_HasNoChemicalOrProduct()
    {
        await Pending(chemicalId: null, productId: null, photo: "barcode-suggestions/def.heic").Create(DbContext);

        var stored = await DbContext.BarcodeSuggestions.AsNoTracking().SingleAsync();
        Assert.That(stored.ChemicalId, Is.Null);
        Assert.That(stored.ProductId, Is.Null);
        Assert.That(stored.LabelPhotoFileName, Is.EqualTo("barcode-suggestions/def.heic"));
    }
}
