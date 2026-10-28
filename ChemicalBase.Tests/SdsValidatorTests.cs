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
                Assert.That(v.RevisionDate, Is.EqualTo(new DateTime(2025, 11, 10)), "latest date, column layout");
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
                     // Ruling (b): the latest date across all keyword windows, not the first one.
                     ("da", "Udstedelsesdato: 01-01-2019 Revisionsdato: 01-03-2024"),
                     ("da", "Revisionsdato: 01-03-2024 Erstatter: 01-01-2020"),
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
        Assert.That(SdsHeadings.For("de")!.RevisionKeywords, Does.Not.Contain("Druckdatum"),
            "a print date is not a revision date, and under the latest-date rule it would always win");
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
    public void Family_reg_no_beats_a_name_match()
    {
        // The concentrate's own name is in its section 1, but its reg.nr. is a sibling's: a family match, never "name".
        var v = SdsValidator.ValidateText(Fixture("da-algefri-43_1.txt"), Query("ECOstyle AlgeFri N", "364-73-2-2"));
        Assert.That(v.IdentityMatch, Is.EqualTo("regno-family"));
    }

    [Test]
    public void Semicolon_is_stripped_from_the_trade_name()
    {
        var v = SdsValidator.ValidateText(Fixture("da-7_1.txt"), Query("Ecolab Peroxide Spray", "999-99", "DrySan; Oxy"));
        Assert.Multiple(() =>
        {
            Assert.That(v.IdentityMatch, Is.EqualTo("trade:DrySan Oxy"));
            Assert.That(v.Notes, Does.Contain("; id=trade:DrySan Oxy; "));
        });
    }

    [Test]
    public void Classifier_and_detector_read_only_the_scan_cap()
    {
        var text = new string('x', RevisionDateParser.MaxScanLength) + " " + Sds("da", "Produkt Alpha");
        Assert.Multiple(() =>
        {
            Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
            Assert.That(SdsLanguageDetector.Detect(text).Language, Is.Null);
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

    // ---- print dates and implausible dates (PR-1 final review I-1) ----

    [TestCase("da-profound-53_1.txt", "2026-02-19")] // "Revisionsdato: 19.02.2026 Trykdato: 30.03.2026"
    [TestCase("da-roundup-57_1.txt", "2026-09-19")] // "Revisionsdato: 19.09.2026 Trykdato: 01.10.2026"
    [TestCase("da-algefri-43_1.txt", "2025-11-10")] // column layout
    [TestCase("da-kvitq-35_2.txt", "2022-09-20")]
    public void Print_date_is_not_the_revision_date_on_real_sds(string file, string expected)
    {
        Assert.That(SdsValidator.ValidateText(Fixture(file), Query("Produkt Alpha", "999-99")).RevisionDate,
            Is.EqualTo(DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)));
    }

    public static IEnumerable<TestCaseData> RevisionDateEdgeCases()
    {
        var future = DateTime.UtcNow.Date.AddYears(2).ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
        yield return new TestCaseData($"Revisionsdato: 01-03-2024 Næste revision: {future}", "2024-03-01")
            .SetName("a future 'next revision' date is ignored");
        yield return new TestCaseData("Revisionsdato: 01-03-2024 Trykdato: 05-05-2025", "2024-03-01")
            .SetName("Danish print date ends the window");
        yield return new TestCaseData("Revisionsdato: 01-03-2024 Udskriftsdato: 05-05-2025", "2024-03-01")
            .SetName("Danish Udskriftsdato ends the window");
        yield return new TestCaseData("Revision date: 01-03-2024 Print date: 05-05-2025", "2024-03-01")
            .SetName("English print date ends the window");
        yield return new TestCaseData("Versionsnummer: 01-03-2025 Revisionsdato: 01-03-2024", "2024-03-01")
            .SetName("a keyword must end at a word boundary");
        yield return new TestCaseData("Revisionsdato: 01-03-1985", null).SetName("before 1990 is not a revision date");
        // Copilot #175: column layout, header row then value row; the print-date column stays excluded.
        yield return new TestCaseData("Revisionsdato Trykdato Version\n01-03-2024 05-05-2025 1.0", "2024-03-01")
            .SetName("column layout, print date in the middle column");
        yield return new TestCaseData("Version Trykdato Revisionsdato\n3.0 05-05-2025 01-03-2024", "2024-03-01")
            .SetName("column layout, print date before the revision column");
        yield return new TestCaseData("Udstedelsesdato: Trykdato: Revisionsdato:\n01-01-2020 05-05-2025 01-03-2024", "2024-03-01")
            .SetName("column layout with colons");
        yield return new TestCaseData("Revisionsdato Trykdato Version\n1. marts 2024 5. maj 2025 1.0", "2024-03-01")
            .SetName("column layout, written-month dates count as one value each");
        yield return new TestCaseData("Revisionsdato Trykdato Version\n1 marts 2024 5 maj 2025 1.0", "2024-03-01")
            .SetName("column layout, written-month dates without a dot");
    }

    [TestCaseSource(nameof(RevisionDateEdgeCases))]
    public void Revision_date_edge_cases(string text, string? expected)
    {
        Assert.That(RevisionDateParser.Find(text, "da"),
            Is.EqualTo(expected == null ? null : DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)));
    }

    // ---- PDF byte cap (I-2) ----

    [Test]
    public void Pdf_over_the_writer_cap_reads_as_no_text()
    {
        var oversize = Oversize(TestPdf.Bytes(Section1("da") + " Produkt Alpha Reg.nr. 706-12"));
        Assert.That(oversize.LongLength, Is.GreaterThan(ChemicalDocumentWriter.MaxPdfBytes));

        Assert.Multiple(() =>
        {
            Assert.That(PdfText.Pages(oversize, 2), Is.EqualTo(""));
            Assert.That(DocumentClassifier.Classify(oversize).Kind, Is.EqualTo(PageOneKind.NoText));
            Assert.That(SdsValidator.Validate(oversize, Query("Produkt Alpha", "706-12")).Score, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// A valid PDF made larger than the cap: a comment of spaces appended after it, then its own startxref trailer
    /// again, so the xref offsets still hold and a PDF reader would open it.
    /// </summary>
    private static byte[] Oversize(byte[] pdf)
    {
        var text = System.Text.Encoding.Latin1.GetString(pdf);
        var tail = System.Text.Encoding.Latin1.GetBytes(text[text.LastIndexOf("startxref", StringComparison.Ordinal)..]);
        var padding = (int)(ChemicalDocumentWriter.MaxPdfBytes - pdf.Length) + 16;
        var result = new byte[pdf.Length + 2 + padding + 1 + tail.Length];
        pdf.CopyTo(result, 0);
        var at = pdf.Length;
        result[at++] = (byte)'\n';
        result[at++] = (byte)'%';
        Array.Fill(result, (byte)' ', at, padding);
        at += padding;
        result[at++] = (byte)'\n';
        tail.CopyTo(result, at);
        return result;
    }

    [Test]
    public void Scan_cap_never_splits_a_surrogate_pair()
    {
        // Copilot #175: a cut between the two halves of "😀" leaves a lone high surrogate, and Normalize throws.
        var text = Sds("da", "Produkt Alpha Revisionsdato: 01-03-2024") + " "
                   + new string('x', RevisionDateParser.MaxScanLength);
        text = text[..(RevisionDateParser.MaxScanLength - 1)] + "\U0001F600" + " mere tekst";
        Assert.That(char.IsHighSurrogate(text[RevisionDateParser.MaxScanLength - 1]), Is.True, "the cap falls inside the pair");

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => SdsLanguageDetector.Detect(text));
            Assert.DoesNotThrow(() => SdsLanguageDetector.ProfileHits(text));
            Assert.DoesNotThrow(() => DocumentClassifier.ClassifyText(text));
            Assert.That(RevisionDateParser.Find(text, "da"), Is.EqualTo(new DateTime(2024, 3, 1)));
            Assert.That(SdsValidator.ValidateText(text, Query("Produkt Alpha", "706-12")).Language, Is.EqualTo("da"));
        });
    }

    [Test]
    public void Profile_hits_read_only_the_scan_cap()
    {
        var text = new string('x', RevisionDateParser.MaxScanLength) + " af mod uden mellem gennem øjne vand brug";
        Assert.That(SdsLanguageDetector.ProfileHits(text)["da"], Is.Zero);
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

    // Each public entry point on its own clock, 1 s each (CI machines are slow; a quadratic scan takes far longer).
    [TestCaseSource(nameof(AdversarialTexts))]
    public void Adversarial_text_returns_quickly_without_a_date(string text)
    {
        var limit = TimeSpan.FromSeconds(1);
        // Warm up: the first call loads the headings file and compiles the regexes.
        SdsValidator.Validate(TestPdf.Bytes(Section1("da")), Query("Produkt Alpha", "706-12"));
        RevisionDateParser.Find("Revisionsdato: 01-03-2024", "da");
        var pdf = TestPdf.Pages(Section1("da"), text);
        var extraction = Time(() => PdfText.Pages(pdf, 2)); // PdfPig's own cost, not the validator's

        DateTime? date = null;
        SdsValidation? fromText = null, fromPdf = null;
        var find = Time(() => date = RevisionDateParser.Find(text, "da"));
        var validateText = Time(() => fromText = SdsValidator.ValidateText(Sds("da", text), Query("Produkt Alpha", "706-12")));
        var validate = Time(() => fromPdf = SdsValidator.Validate(pdf, Query("Produkt Alpha", "706-12")));

        Assert.Multiple(() =>
        {
            Assert.That(date, Is.Null);
            Assert.That(fromText!.RevisionDate, Is.Null);
            Assert.That(fromPdf!.RevisionDate, Is.Null);
            Assert.That(find, Is.LessThan(limit), "RevisionDateParser.Find");
            Assert.That(validateText, Is.LessThan(limit), "SdsValidator.ValidateText");
            Assert.That(validate - extraction, Is.LessThan(limit), "SdsValidator.Validate beyond text extraction");
        });
    }

    [Test]
    public void Text_is_capped_before_it_is_normalised()
    {
        // 100k spaces collapse to one when normalised; the cap applies to the raw text first, so the date is not read.
        var text = "Revisionsdato:" + new string(' ', 100000) + "01-03-2024";
        Assert.That(RevisionDateParser.Find(text, "da"), Is.Null);
    }

    [Test]
    public void Date_beyond_the_scan_cap_is_not_read()
    {
        var text = new string('x', RevisionDateParser.MaxScanLength) + " Revisionsdato: 01-03-2024";
        Assert.That(RevisionDateParser.Find(text, "da"), Is.Null);
    }

    // ---- helpers ----

    private static TimeSpan Time(Action action)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        action();
        return watch.Elapsed;
    }

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
