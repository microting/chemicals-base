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

/// <param name="RegistrationNumbersFound">Reg.nr.-shaped tokens on page 1 ("706-12", "882-2-11-2"), dates
/// excluded, distinct, in order of appearance.</param>
public sealed record Classification(PageOneKind Kind, IReadOnlyList<string> RegistrationNumbersFound);

/// <summary>
/// Tells an SDS from a label/brugsanvisning by its first page. The one shared copy: the BMD label importer, the SDS
/// harvester and the staff bulk-upload inbox all call it (F22). Page 1 is normalised as in
/// <see cref="SdsLanguageDetector"/> (lower case, whitespace collapsed, spaces around "/" removed). Rules, for every
/// language in <see cref="SdsHeadings.All"/> that has headings (F36, F68):
/// <list type="number">
/// <item>Fewer than 20 letters → <see cref="PageOneKind.NoText"/>.</item>
/// <item>The language's section-1 heading (its HeadingPattern with n = 1, word-first "punkt 1", "rubrique 1" or
/// number-first "1. szakasz", "1 skirsnis", whatever separator follows; not "punkt 10" or "section 1.4"), or its
/// Annex II section-1 title → <see cref="PageOneKind.Sds"/>.</item>
/// <item>A whole-word SDS term of any language ("sikkerhedsdatablad", "säkerhetsdatablad", "sds", …) and none of the
/// Danish label markers ("læs nærmere i det eventuelt lovpligtige sikkerhedsdatablad", "brugsanvisning",
/// "må kun anvendes") → <see cref="PageOneKind.Sds"/>.</item>
/// <item>Otherwise <see cref="PageOneKind.LabelOrInstructions"/>.</item>
/// </list>
/// Icelandic ("is") has no Annex II headings in the file, so only its SDS term can match.
/// </summary>
public static class DocumentClassifier
{
    private const int MinLetters = 20;

    private const string NotLetterBefore = @"(?<![\p{L}\p{M}])";
    private const string NotLetterAfter = @"(?![\p{L}\p{M}])";

    private static readonly IReadOnlyList<string> DanishLabelMarkers =
    [
        SdsLanguageDetector.Normalize("læs nærmere i det eventuelt lovpligtige sikkerhedsdatablad"),
        SdsLanguageDetector.Normalize("brugsanvisning"),
        SdsLanguageDetector.Normalize("må kun anvendes"),
    ];

    // ECMAScript: \d is [0-9] and \b is an ASCII word boundary.
    private static readonly Regex RegistrationNumber =
        new(@"\b\d{1,4}(-\d{1,4}){1,4}\b", RegexOptions.Compiled | RegexOptions.ECMAScript);

    private static readonly Lazy<Rules> Loaded = new(BuildRules);

    /// <summary>Classifies page 1 of <paramref name="pdf"/>; an unreadable PDF is <see cref="PageOneKind.NoText"/>.</summary>
    public static Classification Classify(byte[] pdf) => ClassifyText(PdfText.Pages(pdf, 1));

    /// <summary>Classifies already extracted page-1 text (same rules as <see cref="Classify"/>).</summary>
    public static Classification ClassifyText(string pageOne)
    {
        ArgumentNullException.ThrowIfNull(pageOne);

        var registrationNumbers = RegistrationNumbers(pageOne);
        if (pageOne.Count(char.IsLetter) < MinLetters)
        {
            return new Classification(PageOneKind.NoText, registrationNumbers);
        }

        var text = SdsLanguageDetector.Normalize(pageOne);
        var rules = Loaded.Value;
        var isSds = rules.Section1Headings.Any(heading => heading.IsMatch(text))
                    || rules.Section1Titles.Any(title => text.Contains(title, StringComparison.Ordinal))
                    || (rules.SdsTerm.IsMatch(text)
                        && !DanishLabelMarkers.Any(marker => text.Contains(marker, StringComparison.Ordinal)));

        return new Classification(isSds ? PageOneKind.Sds : PageOneKind.LabelOrInstructions, registrationNumbers);
    }

    private static IReadOnlyList<string> RegistrationNumbers(string text) =>
        RegistrationNumber.Matches(text)
            .Select(match => match.Value)
            .Where(value => !IsDateLike(value))
            .Distinct()
            .ToList();

    /// <summary>"28-11-2024", "2024-11-28", "11-2024": a day/month with a 4-digit year (19xx/20xx).</summary>
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

    private static Rules BuildRules()
    {
        var withHeadings = SdsHeadings.All.Values.Where(terms => terms.HasHeadings).ToList();

        var headings = withHeadings
            .Select(Section1Heading)
            .Distinct() // sv and no share "AVSNITT {n}: "
            .Select(pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant))
            .ToList();

        var titles = withHeadings
            .Select(terms => SdsLanguageDetector.Normalize(terms.Section1Title!))
            .Distinct()
            .ToList();

        var sdsTerms = SdsHeadings.All.Values
            .SelectMany(terms => terms.SdsTerms)
            .Select(SdsLanguageDetector.Normalize)
            .Where(term => term != "")
            .Distinct()
            .OrderByDescending(term => term.Length)
            .Select(Regex.Escape);
        var sdsTerm = new Regex($"{NotLetterBefore}(?:{string.Join("|", sdsTerms)}){NotLetterAfter}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        return new Rules(headings, titles, sdsTerm);
    }

    /// <summary>
    /// The section-1 heading of one language, any separator: "PUNKT {n}: " → "punkt 1" not followed by another
    /// digit or by ".4"/",5"; "{n}. SZAKASZ: " → "1. szakasz", "1.szakasz", "1 szakasz".
    /// </summary>
    private static string Section1Heading(SdsLanguageTerms terms)
    {
        var word = Regex.Escape(SdsLanguageDetector.Normalize(terms.SectionWord!));
        var numberFirst = terms.HeadingPattern!.TrimStart().StartsWith("{n}", StringComparison.Ordinal);
        return numberFirst
            ? $@"(?<![\p{{L}}\p{{M}}\d.,])1\s?\.?\s?{word}{NotLetterAfter}"
            : $@"{NotLetterBefore}{word}\s?1(?!\d)(?!\s?[.,]\s?\d)";
    }

    private sealed record Rules(IReadOnlyList<Regex> Section1Headings, IReadOnlyList<string> Section1Titles, Regex SdsTerm);
}
