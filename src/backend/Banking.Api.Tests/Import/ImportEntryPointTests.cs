using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Banking.Api.Import;
using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Import;

public sealed class ImportEntryPointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task RejectedInvalidAndConflictingInputsLeaveEveryStoredRecordUnchanged()
    {
        await using var db = fixture.CreateContext();
        var importer = new SeedImporter(db, new BankingTransactionCoordinator(db, new BankingCommitObserver()));
        await importer.ImportAsync(BaselineImportTests.SeedPath());
        var collisionId = Guid.NewGuid();
        db.Customers.Add(new Customer { Id = collisionId, Name = "Existing", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var source = new[] { "customers", "accounts", "transfers", "transaction_entries" }
            .ToDictionary(type => type, type => File.ReadAllText(Path.Combine(BaselineImportTests.SeedPath(), $"{type}.csv")));
        var customer = Row(source, "customers", 1);
        var account = Row(source, "accounts", 1);
        var entry = Row(source, "transaction_entries", 1);
        var transfer = Row(source, "transfers", 1);
        var customerFields = customer.Split(',');
        var accountFields = account.Split(',');
        var entryFields = entry.Split(',');
        var transferFields = transfer.Split(',');
        var secondEntryFields = Row(source, "transaction_entries", 2).Split(',');
        var transferEntry = source["transaction_entries"].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .First(row => row.Contains(",TRANSFER_OUT,", StringComparison.Ordinal)).TrimEnd('\r');
        var transferEntryFields = transferEntry.Split(',');
        var depositEntry = source["transaction_entries"].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .First(row => row.Contains(",DEPOSIT,", StringComparison.Ordinal)).TrimEnd('\r');

        var cases = new Dictionary<string, Dictionary<string, string>>
        {
            ["missing column"] = Change(source, "customers", source["customers"].Replace(",national_id", "", StringComparison.Ordinal)),
            ["malformed row"] = Change(source, "customers", source["customers"].Replace(customer, customer + ",extra", StringComparison.Ordinal)),
            ["duplicate UUID"] = Append(source, "customers", customer),
            ["duplicate sequence"] = ReplaceField(source, "transaction_entries", entry, entryFields, 1, secondEntryFields[1]),
            ["invalid UUID"] = ReplaceField(source, "accounts", account, accountFields, 0, "invalid"),
            ["invalid decimal"] = ReplaceField(source, "accounts", account, accountFields, 3, "1.001"),
            ["invalid currency"] = ReplaceField(source, "accounts", account, accountFields, 2, "USD"),
            ["invalid type"] = ReplaceField(source, "transaction_entries", entry, entryFields, 3, "INVALID"),
            ["unresolved customer"] = ReplaceField(source, "accounts", account, accountFields, 1, Guid.NewGuid().ToString()),
            ["unresolved optional reference"] = ReplaceField(source, "transaction_entries", transferEntry, transferEntryFields, 9, Guid.NewGuid().ToString()),
            ["mismatched transfer"] = ReplaceField(source, "transfers", transfer, transferFields, 3, "0.01"),
            ["changed accepted record"] = ReplaceField(source, "customers", customer, customerFields, 1, "Changed fictional name"),
            ["missing accepted record"] = Change(source, "transaction_entries", source["transaction_entries"].Replace(depositEntry, "", StringComparison.Ordinal)),
            ["untracked collision"] = Append(source, "customers", customer.Replace(customerFields[0], collisionId.ToString(), StringComparison.Ordinal))
        };
        var before = await RepeatImportTests.Snapshot(db);
        foreach (var (name, input) in cases)
        {
            var error = await Record.ExceptionAsync(async () => await importer.ImportAsync(type => new StringReader(input[type])));
            Assert.True(error is SeedInputException, $"{name}: expected a seed input error, received {error?.GetType().Name ?? "none"}.");
            Assert.Equal(before, await RepeatImportTests.Snapshot(db));
        }

        var missing = await Record.ExceptionAsync(async () =>
            await importer.ImportAsync(Path.Combine(BaselineImportTests.SeedPath(), "missing")));
        Assert.IsType<SeedInputException>(missing);
        Assert.Equal(before, await RepeatImportTests.Snapshot(db));
        missing = await Record.ExceptionAsync(async () =>
            await importer.ImportAsync(type => type == "accounts"
                ? throw new SeedInputException("accounts.csv: required file is missing.")
                : new StringReader(source[type])));
        Assert.IsType<SeedInputException>(missing);
        Assert.Equal(before, await RepeatImportTests.Snapshot(db));
    }

    private static string Row(Dictionary<string, string> source, string type, int index) =>
        source[type].Split('\n', StringSplitOptions.RemoveEmptyEntries)[index].TrimEnd('\r');

    private static Dictionary<string, string> Change(Dictionary<string, string> source, string type, string text) =>
        new(source) { [type] = text };

    private static Dictionary<string, string> Append(Dictionary<string, string> source, string type, string row) =>
        Change(source, type, source[type] + "\n" + row + "\n");

    private static Dictionary<string, string> ReplaceField(Dictionary<string, string> source, string type,
        string row, string[] fields, int index, string value)
    {
        var replacement = (string[])fields.Clone();
        replacement[index] = value;
        return Change(source, type, source[type].Replace(row, string.Join(',', replacement), StringComparison.Ordinal));
    }
}
