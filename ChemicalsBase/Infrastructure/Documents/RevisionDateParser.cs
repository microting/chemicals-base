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
/// Finds an SDS revision date: the latest plausible date starting within <see cref="Window"/> characters after any
/// revision keyword (ruling (b): issue and superseded dates are never later than the revision).
/// <list type="bullet">
/// <item>A keyword is a whole word ("version", not "versionsnummer"). Its window ends early at a print-date label
/// (<see cref="PrintDateLabels"/>): "Revisionsdato: 19.02.2026 Trykdato: 30.03.2026" gives 2026-02-19.</item>
/// <item>Plausible: from 1990-01-01 to tomorrow (UTC), so a "Næste revision" date in the future never wins.</item>
/// <item>Keywords are the language's <c>RevisionKeywords</c> from the headings file; when those find no date, the
/// da and en keywords are tried (F38).</item>
/// </list> Formats: <c>dd-MM-yyyy</c>, <c>dd.MM.yyyy</c>,
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

    private static readonly DateTime Earliest = new(1990, 1, 1);

    /// <summary>
    /// Print-date labels that end a keyword's window. A print date is stamped on every download and is later than the
    /// revision, so under the latest-date rule it would always win. Kept here, not in the verbatim Annex II JSON.
    /// </summary>
    internal static readonly IReadOnlyList<string> PrintDateLabels =
    [
        "trykdato", "udskriftsdato", "udskrivningsdato", "printdato", // da
        "print date", "printing date", "date of printing", "date printed", "printed", // en
        "druckdatum", "gedruckt am", // de
        "utskriftsdatum", // sv
        "utskriftsdato", // no
        "date d'impression", "date d’impression", "imprimé le", // fr
        "data di stampa", // it
        "fecha de impresión", // es
        "afdrukdatum", // nl
        "tulostuspäivä", // fi
        "data wydruku", // pl
    ];

    // Column layout: labels of one header row are at most this far apart, with no digit between them.
    private const int MaxHeaderGap = 30;

    // How far after a header row its value row is read.
    private const int MaxValueRow = 200;

    private static readonly Regex ValueToken =
        new(@"\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant, UntrustedText.MatchTimeout);

    private static readonly Lazy<Regex> PrintDateLabel = new(() => new Regex(
        $@"(?<![\p{{L}}\p{{M}}])(?:{Alternation(PrintDateLabels)})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant, UntrustedText.MatchTimeout));

    private static readonly Lazy<IReadOnlyDictionary<string, Rules>> ByLanguage = new(() => SdsHeadings.All.Keys
        .ToDictionary(code => code, code => Rules.For([code])));

    private static readonly Lazy<Rules> Fallback = new(() => Rules.For(FallbackLanguages));

    /// <summary>The revision date in <paramref name="text"/> for <paramref name="language"/> (null: da/en).</summary>
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
                new Regex($@"(?<![\p{{L}}\p{{M}}])(?:{Alternation(keywords)})(?![\p{{L}}\p{{M}}])",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant, UntrustedText.MatchTimeout),
                new Regex(
                    $@"(?<![0-9])(?:(?<iy>[0-9]{{4}})-(?<im>[0-9]{{1,2}})-(?<id>[0-9]{{1,2}})" +
                    $@"|(?<nd>[0-9]{{1,2}})(?<sep>[-./])(?<nm>[0-9]{{1,2}})\k<sep>(?<ny>[0-9]{{4}})" +
                    $@"|(?<md>[0-9]{{1,2}})\.?\s?(?<mn>{Alternation(months.Keys)})(?![\p{{L}}\p{{M}}])" +
                    $@"\s?(?<my>[0-9]{{4}}))(?![0-9])",
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
                var printValues = PrintColumnValues(text);
                for (var keyword = keywords.Match(text); keyword.Success; keyword = keyword.NextMatch())
                {
                    var end = keyword.Index + keyword.Length;
                    var length = Math.Min(Window + MaxDateLength, text.Length - end);
                    var printLabel = PrintDateLabel.Value.Match(text, end, length);
                    if (printLabel.Success)
                    {
                        length = printLabel.Index - end;
                    }

                    for (var date = dates.Match(text, end, length); date.Success && date.Index - end <= Window;
                         date = date.NextMatch())
                    {
                        if (!printValues.Any(v => date.Index >= v.Start && date.Index < v.End)
                            && ToDate(date) is { } found && (latest == null || found > latest))
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

        /// <summary>
        /// Column layout ("Revisionsdato Trykdato Version" then "01-03-2024 05-05-2025 1.0"): in a header row (labels
        /// at most <see cref="MaxHeaderGap"/> apart, no digit between them) the value in a print label's column is a
        /// print date, so it is excluded from every keyword window. In a row layout ("Trykdato: 30.03.2026") the
        /// header row is the print label alone, and its value is the date after it. Linear: two passes of matches.
        /// </summary>
        private List<(int Start, int End)> PrintColumnValues(string text)
        {
            var labels = keywords.Matches(text).Select(m => (m.Index, End: m.Index + m.Length, Print: false))
                .Concat(PrintDateLabel.Value.Matches(text).Select(m => (m.Index, End: m.Index + m.Length, Print: true)))
                .OrderBy(label => label.Index)
                .ToList();

            var excluded = new List<(int Start, int End)>();
            var run = new List<(int Index, int End, bool Print)>();
            foreach (var label in labels.Append((Index: int.MaxValue, End: int.MaxValue, Print: false)))
            {
                if (run.Count > 0 && label.Index < run[^1].End)
                {
                    continue; // overlapping label
                }

                var sameRow = run.Count > 0 && label.Index != int.MaxValue && label.Index - run[^1].End <= MaxHeaderGap
                              && !text.AsSpan(run[^1].End, label.Index - run[^1].End).ContainsAnyInRange('0', '9');
                if (!sameRow && run.Count > 0)
                {
                    ExcludePrintColumns(text, run, excluded);
                    run.Clear();
                }

                run.Add(label);
            }

            return excluded;
        }

        private static void ExcludePrintColumns(string text, List<(int Index, int End, bool Print)> run,
            List<(int Start, int End)> excluded)
        {
            if (!run.Any(label => label.Print))
            {
                return;
            }

            var rowStart = run[^1].End;
            var column = 0;
            for (var token = ValueToken.Match(text, rowStart, Math.Min(MaxValueRow, text.Length - rowStart));
                 token.Success && column < run.Count;
                 token = token.NextMatch())
            {
                if (!token.Value.Any(char.IsLetterOrDigit))
                {
                    continue; // ":" between a label and its value
                }

                if (run[column].Print)
                {
                    excluded.Add((token.Index, token.Index + token.Length));
                }

                column++;
            }
        }

        private DateTime? ToDate(Match m)
        {
            var (year, month, day) =
                m.Groups["iy"].Success ? (Int(m, "iy"), Int(m, "im"), Int(m, "id"))
                : m.Groups["ny"].Success ? (Int(m, "ny"), Int(m, "nm"), Int(m, "nd"))
                : (Int(m, "my"), months[m.Groups["mn"].Value], Int(m, "md"));

            if (year is < 1 or > 9999 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                return null;
            }

            var date = new DateTime(year, month, day);
            return date >= Earliest && date <= DateTime.UtcNow.Date.AddDays(1) ? date : null;
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
