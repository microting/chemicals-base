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
public class ChemicalDocumentVersion : PnBase
{
    public int ChemicalDocumentId { get; set; }

    public int ChemicalId { get; set; }

    /// <summary>Danish Sds: the product it was first accepted onto.</summary>
    public int? ProductId { get; set; }

    public ChemicalDocumentType Type { get; set; }

    public ChemicalDocumentSource Source { get; set; }

    public ChemicalDocumentStatus Status { get; set; }

    /// <summary>S3 key is "&lt;md5&gt;.pdf", as for SDS today.</summary>
    [StringLength(32)]
    public string ContentMd5 { get; set; } = "";

    [StringLength(64)]
    public string ContentSha256 { get; set; } = "";

    public long SizeBytes { get; set; }

    [StringLength(1024)]
    public string? SourceUrl { get; set; }

    /// <summary>BMD DownloadExternal GUID.</summary>
    [StringLength(64)]
    public string? SourceFileId { get; set; }

    [StringLength(255)]
    public string? SourceFileName { get; set; }

    /// <summary>e.g. "5 L, 4x5 L &amp; 10 L".</summary>
    [StringLength(255)]
    public string? SourcePackaging { get; set; }

    /// <summary>BMD External integer id.</summary>
    public int? SourceProductId { get; set; }

    /// <summary>"bmd" or "site:&lt;HolderSdsSite.Id&gt;:&lt;strategy&gt;".</summary>
    [StringLength(64)]
    public string? SourceAdapter { get; set; }

    public DateTime? SourceRevisionDate { get; set; }

    /// <summary>e.g. "III".</summary>
    [StringLength(16)]
    public string? SourceRevision { get; set; }

    public DateTime RetrievedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    /// <summary>Last revision re-check.</summary>
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>0..100, candidates only.</summary>
    public int? ValidationScore { get; set; }

    /// <summary>Tokens, e.g. "sds=yes; lang=da(0.92); id=regno; rev=2024-03-01".</summary>
    [StringLength(1024)]
    public string? ValidationNotes { get; set; }

    public bool StaffLocked { get; set; }

    [StringLength(255)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(255)]
    public string? ReviewNote { get; set; }
}
