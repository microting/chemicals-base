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
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>
/// Scores a candidate PDF against an <see cref="SdsQuery"/>. The one shared copy: the SDS harvester (service) and the
/// staff bulk-upload matcher (plugin, Task 12j) both call it. It only scores; keeping or dropping is the caller's.
/// Rules, on the text of pages 1–2:
/// <list type="bullet">
/// <item><b>IsSds:</b> <see cref="DocumentClassifier"/> says Sds and the text has a section-1 heading of some language
/// (F36).</item>
/// <item><b>Language:</b> <see cref="SdsLanguageDetector.Detect"/>; note token <c>lang-uncertain</c> when Uncertain.</item>
/// <item><b>IdentityMatch</b>, in the section-1 slice (the detected language's section 1 up to its section-2
/// heading, else 1500 characters from section 1, else the first 1500 characters): the exact reg.nr. (any hyphen or
/// dash, spaces around it allowed) → <c>regno</c>; every token of the name (case-folded, ™®© and punctuation
/// dropped) in the slice → <c>name</c>; the same for a trade name → <c>trade:&lt;trade name&gt;</c>; a reg.nr. with
/// the same first two segments ("364-73-1-1" for "364-73-2-2", "734-8" for "734-8-18-2") → <c>regno-family</c>,
/// never an exact match (spike §2).</item>
/// <item><b>RevisionDate:</b> <see cref="RevisionDateParser"/>.</item>
/// <item><b>Score:</b> IsSds 30 + round(25 × confidence) when the language is the target + regno 35 / name 25 /
/// trade 20 / regno-family 15 + revision date 10, at most 100 (F37). No text layer → 0.</item>
/// </list>
/// </summary>
public static class SdsValidator
{
    internal const int MaxPages = 2;
    internal const int FallbackSliceLength = 1500;

    private const int SdsPoints = 30;
    private const int LanguagePoints = 25;
    private const int RevisionPoints = 10;

    // Any hyphen or dash between two digits, optionally spaced: "706–12", "706 - 12" → "706-12".
    private static readonly Regex DashBetweenDigits =
        new(@"(?<=[0-9])\s?[\-‐‑‒–—−]\s?(?=[0-9])", RegexOptions.Compiled,
            RevisionDateParser.MatchTimeout);

    private static readonly Regex Token = new(@"[\p{L}\p{M}\p{Nd}]+", RegexOptions.Compiled,
        RevisionDateParser.MatchTimeout);

    public static SdsValidation Validate(byte[] pdf, SdsQuery q) => ValidateText(PdfText.Pages(pdf, MaxPages), q);

    /// <summary>As <see cref="Validate"/>, on already extracted text of pages 1–2.</summary>
    public static SdsValidation ValidateText(string text, SdsQuery q)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(q);
        text = RevisionDateParser.Cap(text); // pages 1–2; PDF text is untrusted, capped before any regex runs

        var classification = DocumentClassifier.ClassifyText(text);
        if (classification.Kind == PageOneKind.NoText)
        {
            return new SdsValidation(false, null, 0, false, null, null, 0, "sds=no; text=none");
        }

        var normalized = SdsLanguageDetector.Normalize(text);
        var isSds = classification.Kind == PageOneKind.Sds
                    && SdsSectionHeading.All.Any(heading => heading.HasHeading(normalized));
        var detected = SdsLanguageDetector.Detect(text);
        var matchesTarget = detected.Language != null && detected.Language == SdsLanguages.Normalize(q.TargetLanguage);
        var (identity, identityPoints) = Identity(Section1Slice(normalized, detected.Language), q);
        var revision = RevisionDateParser.FindNormalized(normalized, detected.Language);

        var score = (isSds ? SdsPoints : 0)
                    + (matchesTarget ? (int)Math.Round(LanguagePoints * detected.Confidence, MidpointRounding.AwayFromZero) : 0)
                    + identityPoints
                    + (revision != null ? RevisionPoints : 0);

        return new SdsValidation(isSds, detected.Language, detected.Confidence, matchesTarget, identity, revision,
            Math.Min(score, 100), Notes(isSds, detected, identity, revision));
    }

    private static string Section1Slice(string normalized, string? language)
    {
        var heading = SdsSectionHeading.For(language);
        var start = heading?.FindSection1(normalized) ?? -1;
        if (start < 0)
        {
            return normalized[..Math.Min(FallbackSliceLength, normalized.Length)];
        }

        var end = heading!.FindSection2(normalized, start);
        return end > start
            ? normalized[start..end]
            : normalized.Substring(start, Math.Min(FallbackSliceLength, normalized.Length - start));
    }

    private static (string? Match, int Points) Identity(string slice, SdsQuery q)
    {
        try
        {
            return IdentityOrThrow(slice, q);
        }
        catch (RegexMatchTimeoutException)
        {
            return (null, 0); // pathological text: no identity rather than a stalled caller
        }
    }

    private static (string? Match, int Points) IdentityOrThrow(string slice, SdsQuery q)
    {
        var expected = string.IsNullOrWhiteSpace(q.RegistrationNo)
            ? null
            : DashBetweenDigits.Replace(q.RegistrationNo.Trim(), "-");
        // A superset (CAS/EC numbers, fragments, 2-digit dates): only "expected ∈ found" counts.
        var found = DocumentClassifier.RegistrationNumbers(DashBetweenDigits.Replace(slice, "-"));
        if (expected != null && found.Contains(expected))
        {
            return ("regno", 35);
        }

        var tokens = Tokens(slice);
        if (NameIn(q.Name, tokens))
        {
            return ("name", 25);
        }

        var trade = (q.TradeNames ?? []).FirstOrDefault(name => NameIn(name, tokens));
        if (trade != null)
        {
            return ($"trade:{trade.Trim()}", 20);
        }

        var family = expected != null ? Family(expected) : null;
        return family != null && found.Any(number => Family(number) == family) ? ("regno-family", 15) : (null, 0);
    }

    /// <summary>The first two segments: "364-73-1-1" → "364-73"; null for a number without a hyphen.</summary>
    private static string? Family(string registrationNo)
    {
        var parts = registrationNo.Split('-');
        return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : null;
    }

    private static bool NameIn(string? name, HashSet<string> sliceTokens)
    {
        var tokens = string.IsNullOrWhiteSpace(name) ? [] : Tokens(SdsLanguageDetector.Normalize(name));
        return tokens.Count > 0 && tokens.All(sliceTokens.Contains);
    }

    private static HashSet<string> Tokens(string normalized) =>
        Token.Matches(normalized).Select(match => match.Value).ToHashSet();

    private static string Notes(bool isSds, DetectedLanguage detected, string? identity, DateTime? revision)
    {
        var language = detected.Language != null
            ? $"lang={detected.Language}({detected.Confidence.ToString("0.00", CultureInfo.InvariantCulture)})"
            : "lang=none";
        return string.Join("; ", new[]
        {
            $"sds={(isSds ? "yes" : "no")}",
            language,
            detected.Uncertain ? "lang-uncertain" : null,
            $"id={identity ?? "none"}",
            $"rev={revision?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "none"}",
        }.Where(part => part != null));
    }
}
