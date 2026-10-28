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

using System.Text.Json;

namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>
/// Per-language SDS vocabulary. Section words and titles are verbatim Annex II Part B headings
/// (Regulation (EU) 2020/878); a language without quotable headings ("is") has them as "".
/// </summary>
public sealed record SdsLanguageTerms(
    string Section1Word,
    string Section1Title,
    string Section2Word,
    IReadOnlyList<string> SdsTerms,
    IReadOnlyList<string> RevisionKeywords,
    IReadOnlyList<string> Months);

/// <summary>Loads the vendored annex-ii-headings.json (embedded resource) once.</summary>
public static class SdsHeadings
{
    private const string ResourceName = "ChemicalsBase.Infrastructure.Documents.annex-ii-headings.json";

    private static readonly Lazy<IReadOnlyDictionary<string, SdsLanguageTerms>> Loaded = new(Load);

    public static IReadOnlyDictionary<string, SdsLanguageTerms> All => Loaded.Value;

    /// <summary>Terms for a language code (normalised first, so "DA" and "nb" work); null when not in the file.</summary>
    public static SdsLanguageTerms? For(string language)
    {
        var code = SdsLanguages.Normalize(language);
        return code != null && All.TryGetValue(code, out var terms) ? terms : null;
    }

    private static IReadOnlyDictionary<string, SdsLanguageTerms> Load()
    {
        using var stream = typeof(SdsHeadings).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
        using var document = JsonDocument.Parse(stream);

        var result = new Dictionary<string, SdsLanguageTerms>();
        foreach (var language in document.RootElement.GetProperty("languages").EnumerateObject())
        {
            var entry = language.Value;
            var pattern = String(entry, "heading_pattern");
            result[language.Name] = new SdsLanguageTerms(
                SectionWord(pattern, 1),
                Section(entry, "1"),
                SectionWord(pattern, 2),
                Strings(entry, "title"),
                Strings(entry, "revision_terms"),
                // Genitive month forms (cs, el, fi, hr, lt, pl) are what dates in running text use.
                Strings(entry, "months").Concat(Strings(entry, "months_genitive")).Distinct().ToList());
        }

        return result;
    }

    /// <summary>"PUNKT {n}: " → "PUNKT 1"; "{n}. SZAKASZ: " → "1. SZAKASZ"; no pattern → "".</summary>
    private static string SectionWord(string pattern, int n) =>
        pattern.Replace("{n}", n.ToString()).Trim().TrimEnd(':', '.', '—', ' ');

    private static string Section(JsonElement entry, string number) =>
        entry.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Object
            && sections.TryGetProperty(number, out var title)
            ? title.GetString() ?? ""
            : "";

    private static string String(JsonElement entry, string name) =>
        entry.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : "";

    private static IReadOnlyList<string> Strings(JsonElement entry, string name) =>
        entry.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(v => v.GetString()!).ToList()
            : [];
}
