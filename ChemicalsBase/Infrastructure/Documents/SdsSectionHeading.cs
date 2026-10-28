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

/// <summary>
/// The one section-1 heading rule (F22), shared by <see cref="SdsLanguageDetector"/> and
/// <see cref="DocumentClassifier"/>. Works on text normalised by <c>SdsLanguageDetector.Normalize</c>.
/// <list type="bullet">
/// <item><b>Heading:</b> the language's HeadingPattern with n = 1, any separator. Word-first "punkt 1", "punkt1",
/// "rubrique 1 —" (not "punkt 10", not "section 1.4"); number-first "1. szakasz", "1.szakasz", "1 skirsnis".</item>
/// <item><b>Section 1 (strong):</b> the full Annex II section-1 title anywhere, or the heading followed within
/// <see cref="TitleWindow"/> characters by the title's first three words ("/" counts as a word break, so
/// "identifikation af stoffet" also matches the older "stoffet/præparatet").</item>
/// </list>
/// Languages without headings ("is") are not in <see cref="All"/>.
/// </summary>
internal sealed class SdsSectionHeading
{
    internal const int TitleWindow = 200;

    private const string NotLetterBefore = @"(?<![\p{L}\p{M}])";
    private const string NotLetterAfter = @"(?![\p{L}\p{M}])";

    private static readonly Lazy<IReadOnlyList<SdsSectionHeading>> Loaded = new(() => SdsHeadings.All
        .Where(kv => kv.Value.HasHeadings)
        .Select(kv => new SdsSectionHeading(kv.Key, kv.Value))
        .ToList());

    private readonly Regex _heading;
    private readonly string _title;
    private readonly string _titleStart;

    private SdsSectionHeading(string language, SdsLanguageTerms terms)
    {
        Language = language;
        _heading = new Regex(HeadingRegex(terms), RegexOptions.Compiled | RegexOptions.CultureInvariant);
        _title = SdsLanguageDetector.Normalize(terms.Section1Title!);
        _titleStart = TitleStart(_title);
    }

    /// <summary>Every language with Annex II headings.</summary>
    internal static IReadOnlyList<SdsSectionHeading> All => Loaded.Value;

    internal string Language { get; }

    /// <summary>True when the text holds this language's section-1 heading (with or without its title).</summary>
    internal bool HasHeading(string normalizedText) => _heading.IsMatch(normalizedText);

    /// <summary>Position of the strong section-1 match (title, or heading + title start), or -1.</summary>
    internal int FindSection1(string normalizedText)
    {
        var position = normalizedText.IndexOf(_title, StringComparison.Ordinal);
        if (position >= 0)
        {
            return position;
        }

        for (var match = _heading.Match(normalizedText); match.Success; match = match.NextMatch())
        {
            var after = match.Index + match.Length;
            var window = normalizedText.Substring(after,
                Math.Min(TitleWindow + _titleStart.Length, normalizedText.Length - after));
            if (window.Contains(_titleStart, StringComparison.Ordinal))
            {
                return match.Index;
            }
        }

        return -1;
    }

    private static string HeadingRegex(SdsLanguageTerms terms)
    {
        var word = Regex.Escape(SdsLanguageDetector.Normalize(terms.SectionWord!));
        var numberFirst = terms.HeadingPattern!.TrimStart().StartsWith("{n}", StringComparison.Ordinal);
        return numberFirst
            ? $@"(?<![\p{{L}}\p{{M}}\d.,])1\s?\.?\s?{word}{NotLetterAfter}"
            : $@"{NotLetterBefore}{word}\s?1(?!\d)(?!\s?[.,]\s?\d)";
    }

    /// <summary>The title up to its third word break (space or "/"): "identifikation af stoffet".</summary>
    private static string TitleStart(string title)
    {
        var breaks = 0;
        for (var i = 0; i < title.Length; i++)
        {
            if ((title[i] == ' ' || title[i] == '/') && ++breaks == 3)
            {
                return title[..i];
            }
        }

        return title;
    }
}
