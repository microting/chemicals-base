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

using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace ChemicalBase.Tests;

/// <summary>Builds small real PDFs for tests (test project only, never in the NuGet package).</summary>
public static class TestPdf
{
    /// <summary>A one-page A4 PDF carrying <paramref name="text"/> in Helvetica.</summary>
    public static byte[] Bytes(string text) => Pages(text);

    /// <summary>An A4 PDF with one page per entry, each carrying its text in Helvetica.</summary>
    public static byte[] Pages(params string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var text in pageTexts)
        {
            builder.AddPage(PageSize.A4).AddText(text, 12, new PdfPoint(40, 780), font);
        }

        return builder.Build();
    }

    /// <summary>A one-page A4 PDF holding only an image (a scan without a text layer).</summary>
    public static byte[] ImageOnly()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        page.AddPng(Convert.FromBase64String(GreyPng2X2), new PdfRectangle(40, 400, 555, 800));
        return builder.Build();
    }

    // A valid 2x2 grey RGB PNG.
    private const string GreyPng2X2 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAADklEQVR4nGNoAAMGCAUAKg4GARWeQtcAAAAASUVORK5CYII=";
}
