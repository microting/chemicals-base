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

using ChemicalsBase.Infrastructure.Data.Enums;

namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>
/// A document to write through <see cref="ChemicalDocumentWriter"/>. Automation passes
/// <see cref="ChemicalDocumentStatus.PendingReview"/>; <see cref="ChemicalDocumentStatus.Active"/> is accepted only from
/// with a named <see cref="ReviewedBy"/> (staff endpoints pass the caller).
/// </summary>
public sealed record NewChemicalDocument(
    int ChemicalId, ChemicalDocumentType Type, ChemicalDocumentSource Source, ChemicalDocumentStatus Status, byte[] Pdf,
    string? SourceUrl = null, string? SourceFileId = null, string? SourceFileName = null, string? SourcePackaging = null,
    int? SourceProductId = null, string? SourceAdapter = null, DateTime? SourceRevisionDate = null, string? SourceRevision = null,
    int? ValidationScore = null, string? ValidationNotes = null, string? ReviewedBy = null, string? Language = null)
{
    /// <summary>The staff caller (<see cref="ReviewedBy"/>), or null when blank: then the write is automation,
    /// whatever <see cref="Source"/> says.</summary>
    internal string? Reviewer => ChemicalDocumentWriter.ReviewerOf(ReviewedBy);
}
