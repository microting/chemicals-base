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
public class DocumentClassifierTests
{
    // Enough letters to pass the NoText threshold without adding any SDS word or label marker.
    private const string Filler = " Produkt Alpha Beta Gamma Delta";

    // ---- real documents ----

    [Test]
    public void Real_bmd_label_is_label()
    {
        var result = DocumentClassifier.Classify(FixtureBytes("label_706-12.pdf"));
        Assert.That(result.Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
        Assert.That(result.RegistrationNumbersFound, Does.Contain("706-12"));
    }

    [Test]
    public void Real_minor_use_instructions_are_label()
    {
        var result = DocumentClassifier.ClassifyText(FixtureText("LabelText", "minor-use-roundup_1.txt"));
        Assert.That(result.Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
        Assert.That(result.RegistrationNumbersFound, Is.EqualTo(new[] { "18-674", "18-687" }));
    }

    // Real page 1-2 text (Fixtures/SdsText/SOURCES.txt says where each comes from).
    [TestCase("da-7_1.txt")]
    [TestCase("da-52_1.txt")]
    [TestCase("de-1_1.txt")]
    [TestCase("en-1_2.txt")]
    [TestCase("sv-10_1.txt")]
    [TestCase("no-mapei_1.txt")]
    public void Real_sds_text_is_sds(string file)
    {
        Assert.That(DocumentClassifier.ClassifyText(FixtureText("SdsText", file)).Kind, Is.EqualTo(PageOneKind.Sds));
    }

    // ---- the heading rule, every language in the headings file (F36) ----

    [Test]
    public void Every_language_with_headings_has_its_section_1_heading_recognised()
    {
        var withHeadings = SdsHeadings.All.Where(kv => kv.Value.HasHeadings).ToList();
        Assert.That(SdsHeadings.All.Keys.Except(withHeadings.Select(kv => kv.Key)), Is.EquivalentTo(new[] { "is" }),
            "skipped: languages without Annex II headings");

        Assert.Multiple(() =>
        {
            foreach (var (code, terms) in withHeadings)
            {
                var headingOnly = terms.HeadingPattern!.Replace("{n}", "1") + Filler;
                Assert.That(DocumentClassifier.ClassifyText(headingOnly).Kind, Is.EqualTo(PageOneKind.Sds),
                    $"{code}: heading pattern, n = 1: \"{headingOnly}\"");

                var titleOnly = "1.0 " + terms.Section1Title + Filler;
                Assert.That(DocumentClassifier.ClassifyText(titleOnly).Kind, Is.EqualTo(PageOneKind.Sds),
                    $"{code}: section-1 title: \"{titleOnly}\"");
            }
        });
    }

    [TestCase("de")]
    [TestCase("fr")]
    public void German_and_french_sds_are_sds(string language)
    {
        var terms = SdsHeadings.For(language)!;
        var text = terms.SdsTerms[0] + "\n" + terms.HeadingPattern!.Replace("{n}", "1") + terms.Section1Title + "\n1.1" + Filler;
        Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.Sds));
    }

    [Test]
    public void German_sds_pdf_is_sds()
    {
        // Through a real PDF (ASCII only: the test font is a standard-14 Helvetica).
        var terms = SdsHeadings.For("de")!;
        var pdf = TestPdf.Bytes(terms.HeadingPattern!.Replace("{n}", "1") + terms.Section1Title);
        Assert.That(DocumentClassifier.Classify(pdf).Kind, Is.EqualTo(PageOneKind.Sds));
    }

    [TestCase("1. SZAKASZ: Termékazonosító, Kereskedelmi név")] // hu
    [TestCase("1.SZAKASZ Termékazonosító, Kereskedelmi név")]
    [TestCase("1 SKIRSNIS. Produkto identifikatorius, Prekinis pavadinimas")] // lt
    [TestCase("1. IEDAĻA. Produkta identifikators, Tirdzniecības nosaukums")] // lv
    [TestCase("1. JAGU. Tootetähis, Kaubanimi ja tootekood")] // et
    public void Hungarian_number_first_heading_is_sds(string text)
    {
        Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.Sds));
    }

    [TestCase("PUNKT 1. IDENTIFIKATION Produktnavn Alpha")]
    [TestCase("SECTION 1 - Identification, Product name Alpha")]
    [TestCase("Section 1 Identification, Product name Alpha")]
    [TestCase("RUBRIQUE 1 : Identification, Nom du produit Alpha")]
    [TestCase("RUBRIQUE 1 – Identification, Nom du produit Alpha")]
    [TestCase("ABSCHNITT 1 Bezeichnung, Handelsname Alpha")]
    public void Heading_separators_are_tolerated(string text)
    {
        Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.Sds));
    }

    [TestCase("Read section 12 before use and keep the product out of reach of children.")]
    [TestCase("See section 1.4 on the back for the emergency number of the supplier.")]
    [TestCase("Dosering: 10 ml pr. liter vand, 1 sektion pr. kvadratmeter overflade.")]
    public void Other_section_numbers_are_not_the_section_1_heading(string text)
    {
        Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
    }

    // ---- the SDS-term rule and the Danish label markers ----

    [Test]
    public void Classifier_flags_sds_like_text()
    {
        var pdf = TestPdf.Bytes("Sikkerhedsdatablad i henhold til forordning (EF) nr. 1907/2006 (REACH) for Produkt Alpha");
        Assert.That(DocumentClassifier.Classify(pdf).Kind, Is.EqualTo(PageOneKind.Sds));
    }

    [TestCase("Säkerhetsdatablad enligt förordning (EG) nr 1907/2006, NonStop Alpha")]
    [TestCase("Sikkerhetsdatablad ifølge forordning (EF) nr. 1907/2006, Produkt Alpha")]
    [TestCase("Biztonsági adatlap az 1907/2006/EK rendelet szerint, Termék Alpha")]
    [TestCase("SDS according to Regulation (EC) No 1907/2006, Product Alpha")]
    public void Any_language_sds_term_without_label_markers_is_sds(string text)
    {
        Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.Sds));
    }

    [TestCase("Myrelokkemiddel. Læs nærmere i det eventuelt lovpligtige sikkerhedsdatablad. Opbevares utilgængeligt for børn.")]
    [TestCase("LÆS NÆRMERE I DET EVENTUELT  LOVPLIGTIGE\nSIKKERHEDSDATABLAD. Opbevares utilgængeligt for børn.")]
    [TestCase("Se brugsanvisningen. Sikkerhedsdatablad kan rekvireres hos leverandøren af produktet.")]
    [TestCase("Må kun anvendes som insektmiddel. Safety data sheet available on request.")]
    public void Label_sentence_mentioning_sds_stays_label(string text)
    {
        Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
    }

    [Test]
    public void Sds_term_inside_a_longer_word_is_not_an_sds_term()
    {
        // "sikkerhedsdatabladet" is a label's reference to the SDS, not an SDS title.
        var text = "Opbevares utilgængeligt for børn. Se sikkerhedsdatabladet for yderligere oplysninger.";
        Assert.That(DocumentClassifier.ClassifyText(text).Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
    }

    // ---- NoText ----

    [Test]
    public void Imageonly_pdf_is_NoText()
    {
        var result = DocumentClassifier.Classify(TestPdf.ImageOnly());
        Assert.That(result.Kind, Is.EqualTo(PageOneKind.NoText));
        Assert.That(result.RegistrationNumbersFound, Is.Empty);
    }

    [Test]
    public void Unreadable_bytes_are_NoText()
    {
        Assert.That(DocumentClassifier.Classify([]).Kind, Is.EqualTo(PageOneKind.NoText));
        Assert.That(DocumentClassifier.Classify("<html>not a pdf</html>"u8.ToArray()).Kind, Is.EqualTo(PageOneKind.NoText));
        Assert.That(PdfText.Pages("%PDF-1.4 truncated"u8.ToArray(), 2), Is.EqualTo(""));
    }

    [TestCase("Side 1 af 2 · 706-12", 6)]
    [TestCase("abcdefghijklmnopqrs", 19)]
    public void Fewer_than_20_letters_is_NoText(string text, int letters)
    {
        Assert.That(text.Count(char.IsLetter), Is.EqualTo(letters));
        var result = DocumentClassifier.ClassifyText(text);
        Assert.That(result.Kind, Is.EqualTo(PageOneKind.NoText));
    }

    [Test]
    public void Twenty_letters_is_text()
    {
        Assert.That(DocumentClassifier.ClassifyText("abcdefghijklmnopqrst").Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
    }

    // ---- page 1 only, and the PDF text helper ----

    [Test]
    public void Only_page_1_is_classified()
    {
        var pdf = TestPdf.Pages(
            "Ukrudtsmiddel. Opbevares utilgaengeligt for boern. Reg.nr. 18-674",
            "SECTION 1: Identification of the substance/mixture and of the company/undertaking");
        var result = DocumentClassifier.Classify(pdf);
        Assert.That(result.Kind, Is.EqualTo(PageOneKind.LabelOrInstructions));
        Assert.That(result.RegistrationNumbersFound, Is.EqualTo(new[] { "18-674" }));
    }

    [Test]
    public void PdfText_reads_the_first_pages_only()
    {
        var pdf = TestPdf.Pages("First page Alpha", "Second page Beta", "Third page Gamma");
        var two = PdfText.Pages(pdf, 2);
        Assert.That(two, Does.Contain("Alpha").And.Contain("Beta"));
        Assert.That(two, Does.Not.Contain("Gamma"));
        Assert.That(PdfText.Pages(pdf, 10), Does.Contain("Gamma"));
        Assert.That(PdfText.Pages(TestPdf.ImageOnly(), 2).Trim(), Is.EqualTo(""));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfText.Pages(pdf, 0));
    }

    // ---- registration numbers ----

    [TestCase("Reg.nr. 706-12", new[] { "706-12" })]
    [TestCase("Godkendelsesnummer 734-8-18-2 og 882-2-11-2.", new[] { "734-8-18-2", "882-2-11-2" })]
    [TestCase("Roundup Dynamic (18-674) og igen 18-674", new[] { "18-674" })]
    [TestCase("Dato 28-11-2024, revideret 2024-11-28 og 1-2-2023: reg. 18-664", new[] { "18-664" })]
    [TestCase("Telefon 33 45 70 00, 1907/2006, H315, UFI MRMW-R0GR-0005-MPJM", new string[0])]
    [TestCase("Bestil nr. 12345-12 eller 706-123456", new string[0])]
    public void Registration_numbers_exclude_dates(string text, string[] expected)
    {
        Assert.That(DocumentClassifier.ClassifyText(text + Filler).RegistrationNumbersFound, Is.EqualTo(expected));
    }

    private static string FixturePath(params string[] parts) =>
        Path.Combine([TestContext.CurrentContext.TestDirectory, "Fixtures", .. parts]);

    private static byte[] FixtureBytes(string name) => File.ReadAllBytes(FixturePath(name));

    private static string FixtureText(string folder, string name) => File.ReadAllText(FixturePath(folder, name));
}
