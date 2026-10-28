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
/// Finds an SDS revision date: the latest valid date starting within <see cref="Window"/> characters after any
/// revision keyword (ruling (b): issue and superseded dates are never later than the revision). Keywords are the language's <c>RevisionKeywords</c> from the headings file; when
/// those find no date, the da and en keywords are tried (F38). Formats: <c>dd-MM-yyyy</c>, <c>dd.MM.yyyy</c>,
/// <c>dd/MM/yyyy</c> (day and month may have one digit), <c>yyyy-MM-dd</c>, and <c>d. &lt;month&gt; yyyy</c> (the dot
/// optional) with the language's <c>Months</c> or <c>MonthsGenitive</c>, or a da/en month name (F68).
/// </summary>
public static class RevisionDateParser
{
    /// <summary>How far after a keyword its date may start.</summary>
    internal const int Window = 100;

    // The longest date text: "dd. <month name> yyyy" with the longest month name stays well under this.
    private const int MaxDateLength = 40;

    /// <summary>Only the first this-many characters of the text are scanned: pages 1–2 of an SDS.</summary>
    public const int MaxScanLength = UntrustedText.MaxScanLength;

    private static readonly string[] FallbackLanguages = ["da", "en"];

    private static readonly Lazy<IReadOnlyDictionary<string, Rules>> ByLanguage = new(() => SdsHeadings.All.Keys
        .ToDictionary(code => code, code => Rules.For([code])));

    private static readonly Lazy<Rules> Fallback = new(() => Rules.For(FallbackLanguages));

    /// <summary>The revision date in <paramref name="text"/> for <paramref name="language"/> (null: da/en only).</summary>
    public static DateTime? Find(string text, string? language)
    {
        ArgumentNullException.ThrowIfNull(text);
        return FindNormalized(SdsLanguageDetector.Normalize(UntrustedText.Cap(text)), language);
    }

    internal static DateTime? FindNormalized(string normalizedText, string? language)
    {
        normalizedText = UntrustedText.Cap(normalizedText);
        var code = SdsLanguages.Normalize(language);
        var own = code != null && ByLanguage.Value.TryGetValue(code, out var rules)
            ? rules.Find(normalizedText)
            : null;
        return own ?? Fallback.Value.Find(normalizedText);
    }

    private sealed class Rules(Regex keywords, Regex dates, IReadOnlyDictionary<string, int> months)
    {
        internal static Rules For(IReadOnlyList<string> languages)
        {
            var terms = languages.Select(code => SdsHeadings.All[code]).ToList();
            var keywords = terms.SelectMany(t => t.RevisionKeywords);

            // The month names of these languages plus da and en ("1. March 2024" in a Danish SDS), first one wins.
            var months = new Dictionary<string, int>();
            foreach (var t in terms.Concat(FallbackLanguages.Select(code => SdsHeadings.All[code])))
            {
                foreach (var list in new[] { t.Months, t.MonthsGenitive ?? [] })
                {
                    for (var i = 0; i < list.Count && i < 12; i++)
                    {
                        months.TryAdd(SdsLanguageDetector.Normalize(list[i]), i + 1);
                    }
                }
            }

            return new Rules(
                new Regex($@"(?<![\p{{L}}\p{{M}}])(?:{Alternation(keywords)})", RegexOptions.Compiled | RegexOptions.CultureInvariant, UntrustedText.MatchTimeout),
                new Regex(
                    $@"(?<![0-9])(?:(?<iy>[0-9]{{4}})-(?<im>[0-9]{{1,2}})-(?<id>[0-9]{{1,2}})" +
                    $@"|(?<nd>[0-9]{{1,2}})(?<sep>[-./])(?<nm>[0-9]{{1,2}})\k<sep>(?<ny>[0-9]{{4}})" +
                    $@"|(?<md>[0-9]{{1,2}})\.?\s?(?<mn>{Alternation(months.Keys)})(?![\p{{L}}\p{{M}}])\s?(?<my>[0-9]{{4}}))(?![0-9])",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant, UntrustedText.MatchTimeout),
                months);
        }

        /// <summary>
        /// The latest valid date in any keyword's window (ruling (b)): an issue date or a superseded version's date is
        /// never later than the revision, in row and in column layout. Linear: each keyword searches only its window.
        /// </summary>
        internal DateTime? Find(string text)
        {
            DateTime? latest = null;
            try
            {
                for (var keyword = keywords.Match(text); keyword.Success; keyword = keyword.NextMatch())
                {
                    var end = keyword.Index + keyword.Length;
                    var length = Math.Min(Window + MaxDateLength, text.Length - end);
                    for (var date = dates.Match(text, end, length); date.Success && date.Index - end <= Window;
                         date = date.NextMatch())
                    {
                        if (ToDate(date) is { } found && (latest == null || found > latest))
                        {
                            latest = found;
                        }
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return null; // hostile or pathological text: no date rather than a stalled harvester
            }

            return latest;
        }

        private DateTime? ToDate(Match m)
        {
            var (year, month, day) =
                m.Groups["iy"].Success ? (Int(m, "iy"), Int(m, "im"), Int(m, "id"))
                : m.Groups["ny"].Success ? (Int(m, "ny"), Int(m, "nm"), Int(m, "nd"))
                : (Int(m, "my"), months[m.Groups["mn"].Value], Int(m, "md"));

            return year is >= 1900 and <= 2099 && month is >= 1 and <= 12 && day >= 1
                   && day <= DateTime.DaysInMonth(year, month)
                ? new DateTime(year, month, day)
                : null;
        }

        private static int Int(Match m, string group) => int.Parse(m.Groups[group].Value);
    }

    private static string Alternation(IEnumerable<string> words) => string.Join("|", words
        .Select(SdsLanguageDetector.Normalize)
        .Where(word => word != "")
        .Distinct()
        .OrderByDescending(word => word.Length)
        .Select(Regex.Escape));
}
