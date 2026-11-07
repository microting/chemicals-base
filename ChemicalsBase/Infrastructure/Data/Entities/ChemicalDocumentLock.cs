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
using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

/// <summary>
/// One row per chemical, written only by <see cref="Documents.ChemicalDocumentLocks.AcquireAsync"/> to serialise
/// document writers for that chemical across all Galera nodes. Plain bookkeeping: not versioned, no WorkflowState.
/// Cascades away with its chemical.
/// </summary>
public class ChemicalDocumentLock
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ChemicalId { get; set; }

    /// <summary>Number of times the lock was acquired; only bumped so the acquire is a real row write.</summary>
    public long Counter { get; set; }

    public DateTime UpdatedAt { get; set; }
}
