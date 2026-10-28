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

namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>The SDS languages the register harvests, and how language codes are normalised.</summary>
public static class SdsLanguages
{
    public static readonly IReadOnlyList<string> Phase1 = ["da", "en"];

    // EU/EEA languages the flutter-chemistry app ships (lib/l10n): no tr/uk, no ga/mt (not in the app).
    public static readonly IReadOnlyList<string> DefaultPhase2 =
        ["bg", "cs", "de", "el", "es", "et", "fi", "fr", "hr", "hu", "is", "it", "lt", "lv", "nl", "no", "pl", "pt", "ro", "sk", "sl", "sv"];

    /// <summary>Harvest attempts per (chemical, language) before it counts as exhausted.</summary>
    public const int DefaultAttemptsBeforeExhausted = 3;

    private static readonly HashSet<string> Known = [.. Phase1, .. DefaultPhase2];

    /// <summary>
    /// "DA" → "da", "nb"/"nn" → "no", "pt-BR" → "pt"; a code outside Phase1 + DefaultPhase2 (or null/blank) → null.
    /// </summary>
    public static string? Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var primary = code.Trim().Split('-', '_')[0].ToLowerInvariant();
        if (primary is "nb" or "nn")
        {
            primary = "no";
        }

        return Known.Contains(primary) ? primary : null;
    }
}
