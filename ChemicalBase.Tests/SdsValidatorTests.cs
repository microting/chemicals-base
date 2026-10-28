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
using ChemicalsBase.Infrastructure.Documents;
using NUnit.Framework;

namespace ChemicalBase.Tests;

[TestFixture]
public class SdsValidatorTests
{
    // ---- the brief's cases 1-8 ----

    public static IEnumerable<TestCaseData> ValidatorCases()
    {
        yield return Case("1 real Danish SDS with reg.nr. and section-1 title scores 100",
            () => SdsValidator.ValidateText(Fixture("da-algefri-43_1.txt"), Query("ECOstyle AlgeFri N", "364-73-1-1")),
            v =>
            {
                Assert.That(v.IsSds, Is.True);
                Assert.That(v.Language, Is.EqualTo("da"));
                Assert.That(v.LanguageConfidence, Is.EqualTo(1.0));
                Assert.That(v.MatchesTargetLanguage, Is.True);
                Assert.That(v.IdentityMatch, Is.EqualTo("regno"));
                Assert.That(v.RevisionDate, Is.Not.Null);
                Assert.That(v.Score, Is.EqualTo(100));
            });

        yield return Case("1 Danish SDS pdf scores 100, notes",
            () => SdsValidator.Validate(
                TestPdf.Pages(Section1("da"), "Handelsnavn: Produkt Alpha Reg.nr. 706-12 Revisionsdato: 01-03-2024",
                    Section2("da")),
                Query("Produkt Alpha", "706-12")),
            v =>
            {
                Assert.That(v.Score, Is.EqualTo(100));
                Assert.That(v.RevisionDate, Is.EqualTo(new DateTime(2024, 3, 1)));
                Assert.That(v.Notes, Is.EqualTo("sds=yes; lang=da(1.00); id=regno; rev=2024-03-01"));
            });

        yield return Case("2 English SDS for a da target stays an en candidate",
            () => SdsValidator.ValidateText(Fixture("en-1_2.txt"), Query("CETOL HLS PLUS (VOC2010) 006", "999-99")),
            v =>
            {
                Assert.That(v.IsSds, Is.True);
                Assert.That(v.Language, Is.EqualTo("en"));
                Assert.That(v.MatchesTargetLanguage, Is.False);
                Assert.That(v.IdentityMatch, Is.EqualTo("name"));
                Assert.That(v.RevisionDate, Is.EqualTo(new DateTime(2024, 1, 26)));
                Assert.That(v.Score, Is.EqualTo(30 + 25 + 10), "no language points");
                Assert.That(v.Notes, Does.Contain("lang=en(1.00)"));
            });

        yield return Case("3 label mentioning sikkerhedsdatablad is not an SDS",
            () => SdsValidator.Validate(FixtureBytes("label_706-12.pdf"), Query("Swirr Myrelokkemiddel", "706-12")),
            v =>
            {
                Assert.That(v.IsSds, Is.False);
                Assert.That(v.Notes, Does.StartWith("sds=no"));
            });

        yield return Case("3 brugsanvisning naming sikkerhedsdatablad is not an SDS",
            () => SdsValidator.Validate(
                TestPdf.Bytes("Brugsanvisning: se sikkerhedsdatablad for Produkt Alpha, reg.nr. 706-12"),
                Query("Produkt Alpha", "706-12")),
            v =>
            {
                Assert.That(v.IsSds, Is.False);
                Assert.That(v.IdentityMatch, Is.EqualTo("regno"), "the identity is still scored");
            });

        yield return Case("4 SDS for another product has no identity match",
            () => SdsValidator.ValidateText(Fixture("da-52_1.txt"), Query("Roundup Flex 500", "18-687")),
            v =>
            {
                Assert.That(v.IsSds, Is.True);
                Assert.That(v.IdentityMatch, Is.Null);
                Assert.That(v.Score, Is.EqualTo(30 + 25 + 10));
                Assert.That(v.Notes, Does.Contain("id=none"));
            });

        yield return Case("5 name with trademark sign matches the name",
            () => SdsValidator.ValidateText(Fixture("da-7_1.txt"), Query("DrySan® Oxy™", "999-99")),
            v => Assert.That(v.IdentityMatch, Is.EqualTo("name")));

        yield return Case("6 trade name match",
            () => SdsValidator.ValidateText(Fixture("da-7_1.txt"),
                Query("Ecolab Peroxide Spray", "999-99", "Oxy Foam Plus", "DrySan Oxy")),
            v =>
            {
                Assert.That(v.IdentityMatch, Is.EqualTo("trade:DrySan Oxy"));
                Assert.That(v.Score, Is.EqualTo(30 + 25 + 20 + 10));
            });

        // 7: the five formats, then a German and a French date, both from the JSON terms (F38), and a genitive month.
        foreach (var (code, line) in new[]
                 {
                     ("da", "Revisionsdato: 01-03-2024"),
                     ("da", "Revisionsdato: 01.03.2024"),
                     ("da", "Revisionsdato: 01/03/2024"),
                     ("da", "Revisionsdato: 2024-03-01"),
                     ("da", "Revisionsdato: 1. marts 2024"),
                     ("de", "Überarbeitet am 3. März 2024"),
                     ("fr", "Date de révision : 12 janvier 2024"),
                     ("pl", "Data aktualizacji: 5 marca 2024"),
                 })
        {
            var expected = code switch
            {
                "de" => new DateTime(2024, 3, 3),
                "fr" => new DateTime(2024, 1, 12),
                "pl" => new DateTime(2024, 3, 5),
                _ => new DateTime(2024, 3, 1),
            };
            yield return Case($"7 revision date {code} \"{line}\"",
                () => SdsValidator.ValidateText(Sds(code, "Produkt Alpha " + line), Query("Produkt Alpha", "706-12")),
                v =>
                {
                    Assert.That(v.Language, Is.EqualTo(code));
                    Assert.That(v.RevisionDate, Is.EqualTo(expected));
                });
        }

        yield return Case("8 no text layer scores 0",
            () => SdsValidator.Validate(TestPdf.ImageOnly(), Query("Produkt Alpha", "706-12")),
            v =>
            {
                Assert.That(v.IsSds, Is.False);
                Assert.That(v.Language, Is.Null);
                Assert.That(v.IdentityMatch, Is.Null);
                Assert.That(v.RevisionDate, Is.Null);
                Assert.That(v.Score, Is.EqualTo(0));
            });
    }

    [TestCaseSource(nameof(ValidatorCases))]
    public void Validator_cases(Func<SdsValidation> validate, Action<SdsValidation> check)
    {
        var validation = validate();
        Assert.Multiple(() => check(validation));
    }

    [Test]
    public void Revision_terms_of_case_7_come_from_the_json()
    {
        Assert.That(SdsHeadings.For("de")!.RevisionKeywords, Does.Contain("Überarbeitet am"));
        Assert.That(SdsHeadings.For("de")!.Months, Does.Contain("März"));
        Assert.That(SdsHeadings.For("fr")!.RevisionKeywords, Does.Contain("Date de révision"));
        Assert.That(SdsHeadings.For("fr")!.Months, Does.Contain("janvier"));
        Assert.That(SdsHeadings.For("pl")!.MonthsGenitive, Does.Contain("marca"));
    }

    // ---- 9-12 ----

    [Test]
    public void Validator_uses_detected_language()
    {
        var danish = SdsValidator.Validate(TestPdf.Pages(Section1("da"), "Produkt Alpha", Section2("da")),
            Query("Produkt Alpha", "706-12") with { TargetLanguage = "de" });
        var german = SdsValidator.Validate(TestPdf.Pages(Section1("de"), "Produkt Alpha", Section2("de")),
            Query("Produkt Alpha", "706-12") with { TargetLanguage = "de" });

        Assert.Multiple(() =>
        {
            Assert.That(danish.Language, Is.EqualTo("da"));
            Assert.That(danish.MatchesTargetLanguage, Is.False);
            Assert.That(german.Language, Is.EqualTo("de"));
            Assert.That(german.LanguageConfidence, Is.EqualTo(1.0));
            Assert.That(german.MatchesTargetLanguage, Is.True);
            Assert.That(german.Score - danish.Score, Is.EqualTo(25), "language points only for the target language");
        });
    }

    [Test]
    public void German_sds_is_sds()
    {
        var v = SdsValidator.ValidateText(Fixture("de-1_1.txt"),
            Query("CETOL HLS PLUS 000", "999-99") with { TargetLanguage = "de" });

        Assert.Multiple(() =>
        {
            Assert.That(v.IsSds, Is.True);
            Assert.That(v.Language, Is.EqualTo("de"));
            Assert.That(v.IdentityMatch, Is.EqualTo("name"));
            Assert.That(v.RevisionDate, Is.EqualTo(new DateTime(2025, 10, 7)));
            Assert.That(v.Score, Is.EqualTo(30 + 25 + 25 + 10));
        });
    }

    [Test]
    public void Bilingual_sds_gets_lang_uncertain_token()
    {
        var v = SdsValidator.ValidateText(Sds("da", "Produkt Alpha") + "\n" + Sds("en", "Product Alpha"),
            Query("Produkt Alpha", "706-12"));

        Assert.Multiple(() =>
        {
            Assert.That(v.Language, Is.EqualTo("da"));
            Assert.That(v.LanguageConfidence, Is.EqualTo(0.5));
            Assert.That(v.Notes, Does.Contain("lang=da(0.50)"));
            Assert.That(v.Notes, Does.Contain("lang-uncertain"));
            Assert.That(v.Score, Is.EqualTo(30 + 13 + 25), "round(25 × 0.5) language points");
        });
    }

    [Test]
    public void Family_prefix_regno_is_family_not_exact()
    {
        // Spike §2: ECOstyle AlgeFri N concentrate 364-73-1-1 offered for the ready-to-use 364-73-2-2.
        var algeFri = SdsValidator.ValidateText(Fixture("da-algefri-43_1.txt"),
            Query("AlgeFri N Klar-til-brug", "364-73-2-2"));
        // Kvit Q 579-14 offered for its sibling Kvit I 579-13: a different reg.nr., not a family member.
        var kvitForSibling = SdsValidator.ValidateText(Fixture("da-kvitq-35_2.txt"), Query("Kvit I Myrelokkedåse", "579-13"));
        var kvitOwn = SdsValidator.ValidateText(Fixture("da-kvitq-35_2.txt"), Query("Kvit Q Myrelokkedåse", "579-14"));
        var prefixOnly = SdsValidator.ValidateText(Sds("da", "Produkt Alpha Reg.nr. 734-8"),
            Query("Produkt Beta", "734-8-18-2"));

        Assert.Multiple(() =>
        {
            Assert.That(algeFri.IdentityMatch, Is.EqualTo("regno-family"));
            Assert.That(algeFri.Score, Is.EqualTo(30 + 25 + 15 + 10));
            Assert.That(algeFri.Notes, Does.Contain("id=regno-family"));
            Assert.That(kvitForSibling.IdentityMatch, Is.Null);
            Assert.That(kvitOwn.IdentityMatch, Is.EqualTo("regno"));
            Assert.That(kvitOwn.RevisionDate, Is.EqualTo(new DateTime(2022, 9, 20)));
            Assert.That(prefixOnly.IdentityMatch, Is.EqualTo("regno-family"));
        });
    }

    // ---- section-1 slice and reg.nr. variants ----

    [TestCase("706-12")]
    [TestCase("706–12")] // en dash
    [TestCase("706‑12")] // non-breaking hyphen
    [TestCase("706 - 12")]
    public void Hyphen_variants_match_the_regno_exactly(string written)
    {
        var v = SdsValidator.ValidateText(Sds("da", $"Produkt Beta Reg.nr. {written}"), Query("Produkt Alpha", "706-12"));
        Assert.That(v.IdentityMatch, Is.EqualTo("regno"));
    }

    [TestCase("706-12-1")]
    [TestCase("1706-12")]
    [TestCase("706-123")]
    public void A_longer_number_is_not_the_regno(string written)
    {
        var v = SdsValidator.ValidateText(Sds("da", $"Produkt Beta Reg.nr. {written}"), Query("Produkt Alpha", "706-12"));
        Assert.That(v.IdentityMatch, Is.Not.EqualTo("regno"));
    }

    [Test]
    public void Name_outside_section_1_does_not_match()
    {
        var v = SdsValidator.ValidateText(Sds("da", "Handelsnavn: Produkt Alpha") + " Indeholder Produkt Gamma Delta",
            Query("Produkt Gamma Delta", "999-99"));
        Assert.That(v.IdentityMatch, Is.Null);
    }

    [Test]
    public void Target_language_defaults_to_danish()
    {
        Assert.That(Query("Produkt Alpha", "706-12").TargetLanguage, Is.EqualTo("da"));
    }

    // ---- hostile input (PDF text is untrusted) ----

    public static IEnumerable<TestCaseData> AdversarialTexts()
    {
        // Many keywords, digits and separators, never a valid date: each keyword used to rescan the rest of the text.
        yield return new TestCaseData(string.Concat(Enumerable.Repeat("Revision 1-1-1 ", 7000))).SetName("keywords and short numbers");
        yield return new TestCaseData(string.Concat(Enumerable.Repeat("Version 12.12.12/", 7000))).SetName("keywords and mixed separators");
        yield return new TestCaseData("Revisionsdato " + string.Concat(Enumerable.Repeat("1-", 50000))).SetName("one keyword, 100k digit-dash run");
        yield return new TestCaseData(new string('7', 100000)).SetName("100k digits");
    }

    [TestCaseSource(nameof(AdversarialTexts))]
    public void Adversarial_text_returns_quickly_without_a_date(string text)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var date = RevisionDateParser.Find(text, "da");
        var v = SdsValidator.ValidateText(Sds("da", text), Query("Produkt Alpha", "706-12"));
        watch.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(date, Is.Null);
            Assert.That(v.RevisionDate, Is.Null);
            Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
        });
    }

    [Test]
    public void Date_beyond_the_scan_cap_is_not_read()
    {
        var text = new string('x', RevisionDateParser.MaxScanLength) + " Revisionsdato: 01-03-2024";
        Assert.That(RevisionDateParser.Find(text, "da"), Is.Null);
    }

    // ---- helpers ----

    private static SdsQuery Query(string name, string registrationNo, params string[] tradeNames) =>
        new(1, name, registrationNo, tradeNames, null);

    private static TestCaseData Case(string name, Func<SdsValidation> validate, Action<SdsValidation> check) =>
        new TestCaseData(validate, check).SetName(name);

    /// <summary>The section-1 heading and title of a language, from the headings file.</summary>
    private static string Section1(string code)
    {
        var terms = SdsHeadings.For(code)!;
        return terms.HeadingPattern!.Replace("{n}", "1") + terms.Section1Title;
    }

    private static string Section2(string code) => SdsHeadings.For(code)!.HeadingPattern!.Replace("{n}", "2") + "Hazards";

    /// <summary>A minimal SDS: section-1 heading and title, the body, the section-2 heading.</summary>
    private static string Sds(string code, string body) => $"{Section1(code)}\n{body}\n{Section2(code)}";

    private static string FixturePath(params string[] parts) =>
        Path.Combine([TestContext.CurrentContext.TestDirectory, "Fixtures", .. parts]);

    private static byte[] FixtureBytes(string name) => File.ReadAllBytes(FixturePath(name));

    private static string Fixture(string name) => File.ReadAllText(FixturePath("SdsText", name));
}
