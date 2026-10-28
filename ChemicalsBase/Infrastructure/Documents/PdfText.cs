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

using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>
/// The one shared PDF text extraction (PdfPig): chemicals-base (Tasks 5 and 9), the service and the plugin
/// (12g backfill, 12j bulk upload) all call this copy (F69).
/// </summary>
public static class PdfText
{
    /// <summary>
    /// The text of the first <paramref name="maxPages"/> pages in content order, pages joined by "\n".
    /// "" when the bytes are not a readable PDF (damaged, encrypted, not a PDF) or the pages carry no text layer.
    /// </summary>
    public static string Pages(byte[] pdf, int maxPages)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPages, 1);

        try
        {
            using var document = PdfDocument.Open(pdf);
            var count = Math.Min(maxPages, document.NumberOfPages);
            var pages = new List<string>(count);
            for (var number = 1; number <= count; number++)
            {
                pages.Add(ContentOrderTextExtractor.GetText(document.GetPage(number)));
            }

            return string.Join("\n", pages);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // PdfPig reports a damaged or unsupported file with several exception types (format, encryption,
            // invalid operation, index errors); for the callers each one means "no usable text".
            return "";
        }
    }
}
