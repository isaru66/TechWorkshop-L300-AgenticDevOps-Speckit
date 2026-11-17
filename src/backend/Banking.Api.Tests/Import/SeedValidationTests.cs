using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Banking.Api.Import;
using Banking.Api.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Import;

public sealed class SeedValidationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public void RejectsDuplicateIdentifiersSequencesAndUnresolvedReferencesBeforeImport()
    {
        var source = LoadSources();
        var customerRows = source["customers"].Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var duplicate = Copy(source);
        duplicate["customers"] += "\n" + customerRows[1] + "\n";
        Assert.Throws<SeedInputException>(() => Parse(duplicate));

        var entries = source["transaction_entries"].Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var entryFields = entries[1].TrimEnd('\r').Split(',');
        var badSequence = Copy(source);
        badSequence["transaction_entries"] = badSequence["transaction_entries"].Replace(
            $"{entryFields[0]},1,", $"{entryFields[0]},2,", StringComparison.Ordinal);
        var sequenceError = Assert.Throws<SeedInputException>(() => Parse(badSequence));
        Assert.Contains("transaction_entries.csv", sequenceError.Message);
        Assert.Contains("row", sequenceError.Message);
        Assert.Contains("sequence", sequenceError.Message);

        var accounts = source["accounts"].Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var accountFields = accounts[1].TrimEnd('\r').Split(',');
        var unresolved = Copy(source);
        unresolved["accounts"] = unresolved["accounts"].Replace(accountFields[1], Guid.NewGuid().ToString("D"), StringComparison.Ordinal);
        var referenceError = Assert.Throws<SeedInputException>(() => Parse(unresolved));
        Assert.Contains("accounts.csv", referenceError.Message);
        Assert.Contains("row", referenceError.Message);
        Assert.Contains("reference", referenceError.Message);

        var missingHeader = Copy(source);
        missingHeader["customers"] = missingHeader["customers"].Replace(",national_id", "", StringComparison.Ordinal);
        Assert.Throws<SeedInputException>(() => Parse(missingHeader));
        var malformedRow = Copy(source);
        malformedRow["customers"] = malformedRow["customers"].Replace(customerRows[1], customerRows[1].TrimEnd('\r') + ",extra", StringComparison.Ordinal);
        var error = Assert.Throws<SeedInputException>(() => Parse(malformedRow));
        Assert.Contains("customers.csv", error.Message);
        Assert.Contains("row", error.Message);
        Assert.DoesNotContain(customerRows[1], error.Message);
    }

    [Fact]
    public void RejectsInvalidMoneyCurrencyTimestampAndTransferSides()
    {
        var source = LoadSources();
        var accountRow = source["accounts"].Split('\n', StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd('\r');
        var fields = accountRow.Split(',');
        foreach (var (index, value) in new[] { (2, "USD"), (3, "1.001"), (3, "-1"), (4, "2024-01-01T12:00:00") })
        {
            var invalid = Copy(source);
            var changed = (string[])fields.Clone();
            changed[index] = value;
            invalid["accounts"] = invalid["accounts"].Replace(accountRow, string.Join(',', changed), StringComparison.Ordinal);
            Assert.Throws<SeedInputException>(() => Parse(invalid));
        }

        var transferRow = source["transfers"].Split('\n', StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd('\r');
        var transferFields = transferRow.Split(',');
        transferFields[3] = "0.01";
        var mismatch = Copy(source);
        mismatch["transfers"] = mismatch["transfers"].Replace(transferRow, string.Join(',', transferFields), StringComparison.Ordinal);
        Assert.Throws<SeedInputException>(() => Parse(mismatch));

        var entryRow = source["transaction_entries"].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .First(x => x.Contains(",TRANSFER_OUT,", StringComparison.Ordinal)).TrimEnd('\r');
        var entryFields = entryRow.Split(',');
        foreach (var (index, value) in new[] { (0, "not-a-uuid"), (3, "INVALID"), (4, "0"), (5, "USD"), (9, Guid.NewGuid().ToString()) })
        {
            var invalid = Copy(source);
            var changed = (string[])entryFields.Clone();
            changed[index] = value;
            invalid["transaction_entries"] = invalid["transaction_entries"].Replace(entryRow, string.Join(',', changed), StringComparison.Ordinal);
            Assert.Throws<SeedInputException>(() => Parse(invalid));
        }
    }

    [Fact]
    public async Task ChangedAcceptedFingerprintRejectsImportWithoutChangingStoredBankingData()
    {
        await using var db = fixture.CreateContext();
        var importer = new SeedImporter(db, new BankingTransactionCoordinator(db, new BankingCommitObserver()));
        await importer.ImportAsync(BaselineImportTests.SeedPath());
        var marker = await db.SeedRecords.FirstAsync(x => x.EntityType == "accounts");
        marker.SourceFingerprint = new string('0', 64);
        await db.SaveChangesAsync();
        var before = await db.Accounts.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Balance);
        var historyBefore = await db.TransactionEntries.AsNoTracking().CountAsync();

        await Assert.ThrowsAsync<SeedInputException>(() => importer.ImportAsync(BaselineImportTests.SeedPath()));

        var after = await db.Accounts.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Balance);
        Assert.Equal(before.OrderBy(x => x.Key), after.OrderBy(x => x.Key));
        Assert.Equal(historyBefore, await db.TransactionEntries.AsNoTracking().CountAsync());
        Assert.Equal(1355, await db.SeedRecords.CountAsync());
    }

    private static Dictionary<string, string> LoadSources() =>
        new[] { "customers", "accounts", "transfers", "transaction_entries" }
            .ToDictionary(type => type, type => File.ReadAllText(Path.Combine(BaselineImportTests.SeedPath(), $"{type}.csv")));

    private static Dictionary<string, string> Copy(Dictionary<string, string> source) => new(source);

    private static SeedBatch Parse(Dictionary<string, string> sources) =>
        SeedCsvReader.ReadSources(type => new StringReader(sources[type]));
}
