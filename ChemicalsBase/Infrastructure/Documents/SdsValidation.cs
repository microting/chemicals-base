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

/// <summary>How well a candidate PDF fits an <see cref="SdsQuery"/>. Scoring only: keeping or dropping is the caller's.</summary>
/// <param name="IsSds">The classifier says Sds and the text has a section-1 heading of some language.</param>
/// <param name="Language">Detected language (ISO 639-1), or null.</param>
/// <param name="LanguageConfidence">0..1, from <see cref="SdsLanguageDetector"/>.</param>
/// <param name="MatchesTargetLanguage"><paramref name="Language"/> == the query's TargetLanguage.</param>
/// <param name="IdentityMatch">"regno", "name", "trade:&lt;trade name&gt;", "regno-family", or null.</param>
/// <param name="RevisionDate">The first date after a revision keyword, or null.</param>
/// <param name="Score">0..100.</param>
/// <param name="Notes">"sds=yes; lang=da(1.00); id=regno; rev=2024-03-01" (plus "lang-uncertain" when flagged).</param>
public sealed record SdsValidation(
    bool IsSds,
    string? Language,
    double LanguageConfidence,
    bool MatchesTargetLanguage,
    string? IdentityMatch,
    DateTime? RevisionDate,
    int Score,
    string Notes);
