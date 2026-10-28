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

/// <summary>Limits for PDF text, which is untrusted: every public text entry point caps its input before any regex.</summary>
internal static class UntrustedText
{
    /// <summary>Only the first this-many characters are read: pages 1–2 of an SDS are far shorter.</summary>
    internal const int MaxScanLength = 64 * 1024;

    /// <summary>Per call of a regex on PDF text; callers treat a timeout as "not found".</summary>
    internal static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    /// <summary>The first <see cref="MaxScanLength"/> characters, one fewer when the cut would split a surrogate pair
    /// (a lone surrogate makes string.Normalize throw).</summary>
    internal static string Cap(string text) =>
        text.Length <= MaxScanLength ? text
        : text[..(char.IsHighSurrogate(text[MaxScanLength - 1]) ? MaxScanLength - 1 : MaxScanLength)];
}
