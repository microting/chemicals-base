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

using System.Text.RegularExpressions;

namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>What page 1 of a document is.</summary>
public enum PageOneKind
{
    /// <summary>A safety data sheet, in any language of the headings file.</summary>
    Sds,

    /// <summary>A label, a brugsanvisning or a minor-use instruction: text, but not an SDS.</summary>
    LabelOrInstructions,

    /// <summary>Fewer than 20 letters on page 1: a scan without a text layer, or not a readable PDF.</summary>
    NoText,
}

/// <param name="RegistrationNumbersFound">Reg.nr.-shaped candidates on page 1 ("706-12", "882-2-11-2"), distinct, in
/// order of appearance, dates with a 4-digit year excluded. A superset: it also holds CAS/EC numbers ("247-500-7"),
/// CAS fragments ("20-7"), phone extensions and dates with a 2-digit year ("28-11-24"). Consumers test
/// "expected reg.nr. ∈ found", never "found == [expected]" or "the first one found".</param>
public sealed record Classification(PageOneKind Kind, IReadOnlyList<string> RegistrationNumbersFound);

/// <summary>
/// Tells an SDS from a label/brugsanvisning by its first page. The one shared copy: the BMD label importer, the SDS
/// harvester and the staff bulk-upload inbox all call it (F22). Page 1 is normalised as in
/// <see cref="SdsLanguageDetector"/> (lower case, whitespace collapsed, spaces around "/" removed). Rules, for every
/// language in <see cref="SdsHeadings.All"/> that has headings (F36, F68):
/// <list type="number">
/// <item>Fewer than 20 letters → <see cref="PageOneKind.NoText"/>.</item>
/// <item>Section 1 as <see cref="SdsSectionHeading"/> defines it, the same rule the detector uses: the Annex II
/// section-1 title, or the section-1 heading ("punkt 1", "1.szakasz", any separator; not "punkt 10") followed within
/// 200 characters by the title's first three words → <see cref="PageOneKind.Sds"/>, whatever else the page says.</item>
/// <item>No Danish label marker ("læs nærmere i det eventuelt lovpligtige sikkerhedsdatablad", "brugsanvisning",
/// "må kun anvendes") and either a section-1 heading alone or a whole-word SDS term of any language
/// ("sikkerhedsdatablad", "säkerhetsdatablad", "sds", "fds", …) → <see cref="PageOneKind.Sds"/>. The marker gate keeps a
/// label's cross-reference "se punkt 1" or its "læs … sikkerhedsdatablad" sentence a label. An unmarked text such as
/// "SDS available on request" still counts as Sds.</item>
/// <item>Otherwise <see cref="PageOneKind.LabelOrInstructions"/>.</item>
/// </list>
/// Icelandic ("is") has no Annex II headings in the file, so only its SDS term can match.
/// </summary>
public static class DocumentClassifier
{
    private const int MinLetters = 20;

    private static readonly IReadOnlyList<string> DanishLabelMarkers =
    [
        SdsLanguageDetector.Normalize("læs nærmere i det eventuelt lovpligtige sikkerhedsdatablad"),
        SdsLanguageDetector.Normalize("brugsanvisning"),
        SdsLanguageDetector.Normalize("må kun anvendes"),
    ];

    // ECMAScript: \d is [0-9] and \b is an ASCII word boundary.
    private static readonly Regex RegistrationNumber =
        new(@"\b\d{1,4}(-\d{1,4}){1,4}\b", RegexOptions.Compiled | RegexOptions.ECMAScript);

    private static readonly Lazy<Regex> SdsTerm = new(BuildSdsTerm);

    /// <summary>Classifies page 1 of <paramref name="pdf"/>; an unreadable PDF is <see cref="PageOneKind.NoText"/>.</summary>
    public static Classification Classify(byte[] pdf) => ClassifyText(PdfText.Pages(pdf, 1));

    /// <summary>Classifies already extracted page-1 text (same rules as <see cref="Classify"/>).</summary>
    public static Classification ClassifyText(string pageOne)
    {
        ArgumentNullException.ThrowIfNull(pageOne);
        pageOne = UntrustedText.Cap(pageOne);

        var registrationNumbers = RegistrationNumbers(pageOne);
        if (pageOne.Count(char.IsLetter) < MinLetters)
        {
            return new Classification(PageOneKind.NoText, registrationNumbers);
        }

        var text = SdsLanguageDetector.Normalize(pageOne);
        var isSds = SdsSectionHeading.All.Any(heading => heading.FindSection1(text) >= 0)
                    || (!DanishLabelMarkers.Any(marker => text.Contains(marker, StringComparison.Ordinal))
                        && (SdsSectionHeading.All.Any(heading => heading.HasHeading(text)) || SdsTerm.Value.IsMatch(text)));

        return new Classification(isSds ? PageOneKind.Sds : PageOneKind.LabelOrInstructions, registrationNumbers);
    }

    /// <summary>The reg.nr. extractor (a superset, see <see cref="Classification.RegistrationNumbersFound"/>); shared
    /// with <see cref="SdsValidator"/>.</summary>
    internal static IReadOnlyList<string> RegistrationNumbers(string text) =>
        RegistrationNumber.Matches(text)
            .Select(match => match.Value)
            .Where(value => !IsDateLike(value))
            .Distinct()
            .ToList();

    /// <summary>
    /// "28-11-2024", "2024-11-28", "11-2024": a day/month with a 4-digit year (19xx/20xx). A 2-digit year ("28-11-24")
    /// is not excluded: it cannot be told from a three-part reg.nr.
    /// </summary>
    private static bool IsDateLike(string value)
    {
        var parts = value.Split('-');
        bool Year(int i) => parts[i].Length == 4 && int.Parse(parts[i]) is >= 1900 and <= 2099;
        bool Month(int i) => int.Parse(parts[i]) is >= 1 and <= 12;
        bool Day(int i) => int.Parse(parts[i]) is >= 1 and <= 31;

        return parts.Length switch
        {
            2 => Month(0) && Year(1),
            3 => (Day(0) && Month(1) && Year(2)) || (Year(0) && Month(1) && Day(2)),
            _ => false,
        };
    }

    /// <summary>Any language's SDS term as a whole word (not "sikkerhedsdatabladet").</summary>
    private static Regex BuildSdsTerm()
    {
        var terms = SdsHeadings.All.Values
            .SelectMany(terms => terms.SdsTerms)
            .Select(SdsLanguageDetector.Normalize)
            .Where(term => term != "")
            .Distinct()
            .OrderByDescending(term => term.Length)
            .Select(Regex.Escape);
        return new Regex($@"(?<![\p{{L}}\p{{M}}])(?:{string.Join("|", terms)})(?![\p{{L}}\p{{M}}])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
