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

/// <summary>SDS PDFs are stored as "&lt;md5 of content&gt;.pdf"; Product.FileName holds the md5.</summary>
public static class SdsFileNames
{
    /// <summary>
    /// md5 of zero bytes. UploadPdf produced it for every upload until 2026-09 (it hashed the
    /// stream after copying without rewinding), so a product carrying it has no usable SDS.
    /// </summary>
    public const string EmptyContentMd5 = "d41d8cd98f00b204e9800998ecf8427e";

    public static bool IsMissing(string? fileName) =>
        string.IsNullOrWhiteSpace(fileName) || fileName == EmptyContentMd5;
}
