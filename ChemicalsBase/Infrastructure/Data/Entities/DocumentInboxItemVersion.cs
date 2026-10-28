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

using System.ComponentModel.DataAnnotations;
using ChemicalsBase.Infrastructure.Data.Enums;

namespace ChemicalsBase.Infrastructure.Data.Entities;

/// <summary>History row written by PnBase.MapVersion on every Create/Update.</summary>
public class DocumentInboxItemVersion : PnBase
{
    public int DocumentInboxItemId { get; set; }

    /// <summary>One upload (many PDFs or one zip).</summary>
    [StringLength(36)]
    public string BatchId { get; set; } = "";

    /// <summary>Entry name with any path stripped.</summary>
    [StringLength(255)]
    public string OriginalFileName { get; set; } = "";

    /// <summary>Blob key is "&lt;md5&gt;.pdf"; "" when NotPdf.</summary>
    [StringLength(32)]
    public string ContentMd5 { get; set; } = "";

    public long SizeBytes { get; set; }

    /// <summary>Sds / Label from the classifier.</summary>
    public ChemicalDocumentType? DetectedType { get; set; }

    [StringLength(8)]
    public string? DetectedLanguage { get; set; }

    public double? LanguageConfidence { get; set; }

    public int? MatchedChemicalId { get; set; }

    /// <summary>The PendingReview document created on match/assign.</summary>
    public int? ChemicalDocumentId { get; set; }

    public int? MatchScore { get; set; }

    /// <summary>Validator tokens plus top-3 suggestions, e.g. "sugg=12,57,903".</summary>
    [StringLength(1024)]
    public string? MatchNotes { get; set; }

    public DocumentInboxStatus Status { get; set; }

    [StringLength(255)]
    public string UploadedBy { get; set; } = "";

    [StringLength(255)]
    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    /// <summary>Staff note entered with the upload.</summary>
    [StringLength(255)]
    public string? Note { get; set; }

    /// <summary>Why the item was rejected or how it was resolved.</summary>
    [StringLength(255)]
    public string? ResolutionNote { get; set; }

    /// <summary>The earlier inbox row with the same content, when Status is Duplicate.</summary>
    public int? DuplicateOfInboxItemId { get; set; }
}
