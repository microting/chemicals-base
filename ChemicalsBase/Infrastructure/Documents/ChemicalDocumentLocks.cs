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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>Per-chemical lock for document writers, replicated across the Galera cluster.</summary>
public static class ChemicalDocumentLocks
{
    private const string AcquireSql =
        "INSERT INTO ChemicalDocumentLocks (ChemicalId, Counter, UpdatedAt) VALUES (@id, 1, @now) " +
        "ON DUPLICATE KEY UPDATE Counter = Counter + 1, UpdatedAt = @now";

    /// <summary>
    /// Locks <paramref name="chemicalId"/> until the caller's open transaction on <paramref name="db"/> commits or
    /// rolls back, by upserting its <see cref="Data.Entities.ChemicalDocumentLock"/> row in exactly one statement.
    /// <para>
    /// The lock is a real write, not <c>SELECT … FOR UPDATE</c>: production MariaDB is a multi-node Galera cluster
    /// behind a load balancer, and Galera does not replicate <c>FOR UPDATE</c> row locks, so a read lock serialises
    /// only writers that happen to hit the same node. A write takes part in Galera certification: of two conflicting
    /// transactions on different nodes, one is aborted at commit with error 1213 (deadlock) and must be retried by
    /// the caller. On a single node the second writer simply waits for the first to commit.
    /// </para>
    /// <para>
    /// The chemical must exist (foreign key, cascade delete). The command runs on the transaction's connection
    /// directly, outside any execution strategy, so it works whether or not the context retries on failure.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">No transaction is open on <paramref name="db"/>.</exception>
    public static async Task AcquireAsync(ChemicalsDbContext db, int chemicalId, DateTime nowUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        var tx = db.Database.CurrentTransaction
                 ?? throw new InvalidOperationException(
                     "ChemicalDocumentLocks.AcquireAsync needs an open transaction: the lock lasts until it ends.");

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = tx.GetDbTransaction();
        command.CommandText = AcquireSql;
        AddParameter(command, "@id", chemicalId);
        AddParameter(command, "@now", nowUtc);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
