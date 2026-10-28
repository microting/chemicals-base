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

namespace ChemicalsBase.Infrastructure.Data.Entities;

/// <summary>
/// Feed-only shape of a tenant-visible SDS in one language (Type Sds, Status Active).
/// Lives in .Entities ON PURPOSE: PnBase.MapVersion skips properties whose type name contains
/// "ChemicalsBase.Infrastructure.Data.Entities", so Chemical.SdsDocuments is never copied to ChemicalVersion.
/// </summary>
public sealed class ChemicalSdsFeedItem
{
    /// <summary>ISO 639-1: "da", "en", "de", …</summary>
    public string Language { get; set; } = "";

    /// <summary>The document's ContentMd5.</summary>
    public string FileName { get; set; } = "";

    public DateTime? RevisionDate { get; set; }

    /// <summary>Set for "da" items that belong to a product (mirrors Products.FileName).</summary>
    public string? ProductName { get; set; }
}
