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

using ChemicalsBase.Infrastructure;
using ChemicalsBase.Infrastructure.Data.Entities;
using ChemicalsBase.Infrastructure.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using NUnit.Framework;

namespace ChemicalBase.Tests;

[TestFixture]
public class ChemicalDocumentLockTests : DbTestFixture
{
    /// <summary>Upper bound for every wait in these tests; nothing sleeps.</summary>
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    private static readonly DateTime T1 = new(2026, 11, 7, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 11, 7, 9, 30, 0, DateTimeKind.Utc);

    private int _chemicalId;

    [SetUp]
    public async Task CreateChemical()
    {
        var chemical = new Chemical
        {
            Name = "Testmiddel",
            RegistrationNo = "1-234",
            RemoteId = "remote-1",
            BiocideUser = new List<int>(),
            PesticideProductGroup = new List<int>(),
            BiocideProductType = new List<int>(),
            PesticidePossibleUse = new List<int>(),
            BiocidePossibleUse = new List<int>(),
            BiocideSpecialUse = new List<int>(),
            BarcodeValue = new List<string>(),
            ClassificationAndLabeling = new ClassificationAndLabeling
            {
                CLP = new CLP(),
                DPD = new DPD { RiskPhrases = new List<int>() },
            },
        };
        await chemical.Create(DbContext);
        _chemicalId = chemical.Id;
    }

    [Test]
    public async Task First_acquire_inserts_and_second_increments()
    {
        await AcquireCommittedAsync(T1);

        var first = await StoredLock();
        Assert.That(first.Counter, Is.EqualTo(1));
        Assert.That(first.UpdatedAt, Is.EqualTo(T1));

        await AcquireCommittedAsync(T2);

        var second = await StoredLock();
        Assert.That(second.Counter, Is.EqualTo(2));
        Assert.That(second.UpdatedAt, Is.EqualTo(T2));
        Assert.That(await DbContext.ChemicalDocumentLocks.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Acquire_without_transaction_throws_and_writes_nothing()
    {
        await using var db = PlainContext();

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await ChemicalDocumentLocks.AcquireAsync(db, _chemicalId, T1));
        Assert.That(await DbContext.ChemicalDocumentLocks.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task Second_transaction_waits_until_first_commits()
    {
        await using var first = PlainContext();
        await using var second = PlainContext();
        await using var observer = PlainContext(RootConnectionString());

        await using var firstTx = await BeginAndAcquireAsync(first, T1);
        await using var secondTx = await second.Database.BeginTransactionAsync();
        // Bounded: if the first never releases, the second fails within MaxWait instead of the 50 s default.
        await second.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 30");
        var secondThread = ((MySqlConnection)second.Database.GetDbConnection()).ServerThread;
        var secondAcquire = ChemicalDocumentLocks.AcquireAsync(second, _chemicalId, T2);

        await WaitUntilLockWaitAsync(observer, secondThread, secondAcquire);
        Assert.That(secondAcquire.IsCompleted, Is.False, "the second acquire must wait for the first commit");

        await firstTx.CommitAsync();
        await secondAcquire.WaitAsync(MaxWait);
        await secondTx.CommitAsync();

        var stored = await StoredLock();
        Assert.That(stored.Counter, Is.EqualTo(2));
        Assert.That(stored.UpdatedAt, Is.EqualTo(T2));
    }

    [Test]
    public async Task Second_transaction_fails_fast_while_first_holds_and_succeeds_after_commit()
    {
        await using var first = PlainContext();
        await using var second = PlainContext();

        await using var firstTx = await BeginAndAcquireAsync(first, T1);
        await using var secondTx = await second.Database.BeginTransactionAsync();
        await second.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 1");

        var timeout = await Assert.ThrowsAsync<MySqlException>(async () =>
            await ChemicalDocumentLocks.AcquireAsync(second, _chemicalId, T2).WaitAsync(MaxWait));
        Assert.That(timeout!.ErrorCode, Is.EqualTo(MySqlErrorCode.LockWaitTimeout));

        await firstTx.CommitAsync();
        // A lock wait timeout rolls back only the statement: the second transaction can acquire now.
        await ChemicalDocumentLocks.AcquireAsync(second, _chemicalId, T2).WaitAsync(MaxWait);
        await secondTx.CommitAsync();

        Assert.That((await StoredLock()).Counter, Is.EqualTo(2));
    }

    [Test]
    public async Task Deleting_the_chemical_deletes_its_lock_row()
    {
        await AcquireCommittedAsync(T1);
        Assert.That(await DbContext.ChemicalDocumentLocks.CountAsync(), Is.EqualTo(1));

        await DbContext.Chemicals.Where(c => c.Id == _chemicalId).ExecuteDeleteAsync();

        Assert.That(await DbContext.ChemicalDocumentLocks.CountAsync(), Is.EqualTo(0));
    }

    private async Task<IDbContextTransaction> BeginAndAcquireAsync(ChemicalsDbContext db, DateTime nowUtc)
    {
        var tx = await db.Database.BeginTransactionAsync();
        await ChemicalDocumentLocks.AcquireAsync(db, _chemicalId, nowUtc);
        return tx;
    }

    private async Task AcquireCommittedAsync(DateTime nowUtc)
    {
        await using var db = PlainContext();
        await using var tx = await BeginAndAcquireAsync(db, nowUtc);
        await tx.CommitAsync();
    }

    private Task<ChemicalDocumentLock> StoredLock() =>
        DbContext.ChemicalDocumentLocks.AsNoTracking().SingleAsync(l => l.ChemicalId == _chemicalId);

    /// <summary>
    /// Polls INNODB_TRX until the thread's transaction is in LOCK WAIT. Fails if the acquire completes first
    /// (it did not wait) or MaxWait passes.
    /// </summary>
    private static async Task WaitUntilLockWaitAsync(ChemicalsDbContext observer, int thread, Task acquire)
    {
        var deadline = DateTime.UtcNow + MaxWait;
        while (DateTime.UtcNow < deadline)
        {
            if (acquire.IsCompleted)
            {
                await acquire; // surface its exception, if any
                Assert.Fail("the second acquire completed while the first transaction still held the lock");
            }

            var waiting = await observer.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM information_schema.INNODB_TRX " +
                    "WHERE trx_mysql_thread_id = {0} AND trx_state = 'LOCK WAIT'", thread)
                .SingleAsync();
            if (waiting > 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        Assert.Fail($"the second acquire was not seen in LOCK WAIT within {MaxWait.TotalSeconds} s");
    }

    /// <summary>INNODB_TRX needs the PROCESS privilege; the test container's root shares the user's password.</summary>
    private string RootConnectionString() =>
        new MySqlConnectionStringBuilder(ConnectionString) { UserID = "root" }.ConnectionString;

    /// <summary>A context without the retrying execution strategy, so a test may own a transaction.</summary>
    private ChemicalsDbContext PlainContext(string? connectionString = null)
    {
        var cs = connectionString ?? ConnectionString;
        return new(new DbContextOptionsBuilder<ChemicalsDbContext>()
            .UseMySql(cs, ServerVersion.AutoDetect(cs))
            .Options);
    }
}
