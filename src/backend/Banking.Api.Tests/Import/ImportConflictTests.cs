using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Banking.Api.Import;
using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Import;

public sealed class ImportConflictTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task RejectsChangedMissingAndCollidingRowsWithoutMutationAndAcceptsValidExtension()
    {
        await using var db = fixture.CreateContext();
        var importer = new SeedImporter(db, new BankingTransactionCoordinator(db, new BankingCommitObserver()));
        await importer.ImportAsync(BaselineImportTests.SeedPath());
        var source = Sources();
        var accountRow = source["accounts"].Split('\n', StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd('\r');
        var accountFields = accountRow.Split(',');
        accountFields[3] = accountFields[3] == "1.00" ? "2.00" : "1.00";
        var changed = new Dictionary<string, string>(source)
        {
            ["accounts"] = source["accounts"].Replace(accountRow, string.Join(',', accountFields), StringComparison.Ordinal)
        };
        var prior = await RepeatImportTests.Snapshot(db);
        await Assert.ThrowsAsync<SeedInputException>(() => importer.ImportBatchAsync(Parse(changed)));
        Assert.Equal(prior, await RepeatImportTests.Snapshot(db));

        var entryRow = source["transaction_entries"].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .First(x => x.Contains(",DEPOSIT,", StringComparison.Ordinal)).TrimEnd('\r');
        var missing = new Dictionary<string, string>(source)
        {
            ["transaction_entries"] = source["transaction_entries"].Replace(entryRow, "", StringComparison.Ordinal)
        };
        await Assert.ThrowsAsync<SeedInputException>(() => importer.ImportBatchAsync(Parse(missing)));
        Assert.Equal(prior, await RepeatImportTests.Snapshot(db));

        var collisionId = Guid.NewGuid();
        db.Customers.Add(new Customer { Id = collisionId, Name = "Existing", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var withCollision = AddCustomer(source, collisionId);
        prior = await RepeatImportTests.Snapshot(db);
        await Assert.ThrowsAsync<SeedInputException>(() => importer.ImportBatchAsync(Parse(withCollision)));
        Assert.Equal(prior, await RepeatImportTests.Snapshot(db));

        var extension = AddCustomer(source, Guid.NewGuid());
        var result = await importer.ImportBatchAsync(Parse(extension));
        Assert.Equal(1, result.Counts["customers"].Inserted);
        Assert.All(result.Counts.Where(x => x.Key != "customers"), x => Assert.Equal(0, x.Value.Inserted));
        Assert.All((await importer.ImportBatchAsync(Parse(extension))).Counts.Values, x => Assert.Equal(0, x.Inserted));
        Assert.Throws<SeedInputException>(() => SeedCsvReader.Read(Path.Combine(BaselineImportTests.SeedPath(), "missing")));
    }

    private static Dictionary<string, string> Sources() =>
        new[] { "customers", "accounts", "transfers", "transaction_entries" }
            .ToDictionary(type => type, type => File.ReadAllText(Path.Combine(BaselineImportTests.SeedPath(), $"{type}.csv")));

    private static SeedBatch Parse(Dictionary<string, string> source) =>
        SeedCsvReader.ReadSources(type => new StringReader(source[type]));

    private static Dictionary<string, string> AddCustomer(Dictionary<string, string> source, Guid id)
    {
        var row = source["customers"].Split('\n', StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd('\r');
        var originalId = row[..row.IndexOf(',')];
        return new Dictionary<string, string>(source)
        {
            ["customers"] = source["customers"] + "\n" + row.Replace(originalId, id.ToString(), StringComparison.Ordinal) + "\n"
        };
    }
}
