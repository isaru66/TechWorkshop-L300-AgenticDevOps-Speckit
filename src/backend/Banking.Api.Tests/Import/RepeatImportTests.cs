using System.Text.Json;
using Banking.Api.Data;
using Banking.Api.Import;
using Banking.Api.Services;
using Banking.Api.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Import;

public sealed class RepeatImportTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task ReimportAfterAllMoneyActivityPreservesEveryRecordAndSeedFingerprint()
    {
        var source = Guid.Parse("00209a31-c483-41bb-a9fd-35530f8f680b");
        var destination = Guid.Parse("fc99d3e9-f19d-439e-87e4-c7a54b585c7d");
        await using var db = fixture.CreateContext();
        var coordinator = new BankingTransactionCoordinator(db, new BankingCommitObserver());
        var importer = new SeedImporter(db, coordinator);
        var service = new BankingService(db, coordinator);
        await importer.ImportAsync(BaselineImportTests.SeedPath());
        Assert.All((await importer.ImportAsync(BaselineImportTests.SeedPath())).Counts.Values, x => Assert.Equal(0, x.Inserted));
        await service.ChangeBalanceAsync(source, "1.00", "THB", true, default);
        await service.ChangeBalanceAsync(source, "0.50", "THB", false, default);
        await service.TransferAsync(source, destination, "100.00", "THB", default);
        var before = await Snapshot(db);
        var repeated = await importer.ImportAsync(BaselineImportTests.SeedPath());
        Assert.All(repeated.Counts.Values, x => Assert.Equal(0, x.Inserted));
        Assert.Equal(before, await Snapshot(db));
    }

    internal static async Task<string> Snapshot(BankingDbContext db) => JsonSerializer.Serialize(new
    {
        customers = await db.Customers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        accounts = await db.Accounts.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        transfers = await db.Transfers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        entries = await db.TransactionEntries.AsNoTracking().OrderBy(x => x.Sequence).ToListAsync(),
        metadata = await db.SeedRecords.AsNoTracking().OrderBy(x => x.EntityType).ThenBy(x => x.RecordId).ToListAsync()
    });
}
