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

/// <summary>
/// Staff-editable config telling the SDS crawler where an authorisation holder publishes its SDS.
/// Unique per (AuthorisationHolderId, Domain); every edit is versioned.
/// </summary>
public class HolderSdsSite : PnBase
{
    /// <summary>BMD holder (AuthorisationHolders table).</summary>
    public int AuthorisationHolderId { get; set; }

    /// <summary>e.g. "cidlines.com"; one row per holder+domain.</summary>
    [StringLength(255)]
    public string Domain { get; set; } = "";

    public HolderSiteState State { get; set; }

    /// <summary>What the crawler may use on this site.</summary>
    public HolderSiteStrategy Strategies { get; set; }

    /// <summary>SDS/download list page.</summary>
    [StringLength(1024)]
    public string? ListingPageUrl { get; set; }

    /// <summary>e.g. "https://x.dk/produkt/{slug}".</summary>
    [StringLength(1024)]
    public string? ProductPageUrlTemplate { get; set; }

    /// <summary>e.g. "https://x.dk/search?q={q}".</summary>
    [StringLength(1024)]
    public string? SiteSearchUrlTemplate { get; set; }

    /// <summary>CSS selector for PDF links; null = all a[href$=.pdf].</summary>
    [StringLength(255)]
    public string? LinkSelector { get; set; }

    public DateTime? LastCrawledAt { get; set; }

    public int CandidatesFound { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }
}
