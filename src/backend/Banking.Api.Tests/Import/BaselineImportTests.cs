using System.Globalization;
using Banking.Api.Data;
using Banking.Api.Import;
using Banking.Api.Tests.Integration;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Import;

public sealed class BaselineImportTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    internal static string SeedPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BankingWorkshop.slnx")))
            directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), "src", "data", "seed");
    }

    [Fact]
    public async Task ImportsOriginalBalancesAndHistoryWithoutReplayAndRepeatsWithoutDuplicates()
    {
        await using var db = fixture.CreateContext();
        var importer = new SeedImporter(db, new BankingTransactionCoordinator(db, new BankingCommitObserver()));
        var result = await importer.ImportAsync(SeedPath());

        Assert.Equal(102, result.Counts["customers"].Inserted);
        Assert.Equal(150, result.Counts["accounts"].Inserted);
        Assert.Equal(101, result.Counts["transfers"].Inserted);
        Assert.Equal(1002, result.Counts["transaction_entries"].Inserted);
        Assert.Equal(102, await db.Customers.CountAsync());
        Assert.Equal(150, await db.Accounts.CountAsync());
        Assert.Equal(101, await db.Transfers.CountAsync());
        Assert.Equal(1002, await db.TransactionEntries.CountAsync());

        var customers = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id);
        foreach (var row in ReadCsv("customers"))
        {
            var actual = customers[Guid.Parse(row["id"])];
            Assert.Equal(row["name"], actual.Name);
            Assert.Equal(Time(row["created_at"]), actual.CreatedAt);
            Assert.Equal(row["english_name"], actual.EnglishName);
            Assert.Equal(row["email"], actual.Email);
            Assert.Equal(row["phone"], actual.Phone);
            Assert.Equal(row["address_line"], actual.AddressLine);
            Assert.Equal(row["subdistrict"], actual.Subdistrict);
            Assert.Equal(row["district"], actual.District);
            Assert.Equal(row["province"], actual.Province);
            Assert.Equal(row["postcode"], actual.Postcode);
            Assert.Equal(row["date_of_birth"].Length == 0 ? (DateOnly?)null : DateOnly.ParseExact(row["date_of_birth"], "yyyy-MM-dd", CultureInfo.InvariantCulture), actual.DateOfBirth);
            Assert.Equal(row["occupation"], actual.Occupation);
            Assert.Equal(row["national_id"], actual.NationalId);
        }

        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(x => x.Id);
        foreach (var row in ReadCsv("accounts"))
        {
            var actual = accounts[Guid.Parse(row["id"])];
            Assert.Equal(Guid.Parse(row["customer_id"]), actual.CustomerId);
            Assert.Equal(row["currency"], actual.Currency);
            Assert.Equal(Money(row["balance"]), actual.Balance);
            Assert.Equal(Time(row["created_at"]), actual.CreatedAt);
        }

        var transfers = await db.Transfers.AsNoTracking().ToDictionaryAsync(x => x.Id);
        foreach (var row in ReadCsv("transfers"))
        {
            var actual = transfers[Guid.Parse(row["id"])];
            Assert.Equal(Guid.Parse(row["source_account_id"]), actual.SourceAccountId);
            Assert.Equal(Guid.Parse(row["destination_account_id"]), actual.DestinationAccountId);
            Assert.Equal(Money(row["amount"]), actual.Amount);
            Assert.Equal(row["currency"], actual.Currency);
            Assert.Equal(Time(row["completed_at"]), actual.CompletedAt);
        }

        var entries = await db.TransactionEntries.AsNoTracking().ToDictionaryAsync(x => x.Id);
        foreach (var row in ReadCsv("transaction_entries"))
        {
            var actual = entries[Guid.Parse(row["id"])];
            Assert.Equal(long.Parse(row["sequence"], CultureInfo.InvariantCulture), actual.Sequence);
            Assert.Equal(Guid.Parse(row["account_id"]), actual.AccountId);
            Assert.Equal(row["type"], actual.Type);
            Assert.Equal(Money(row["amount"]), actual.Amount);
            Assert.Equal(row["currency"], actual.Currency);
            Assert.Equal(Time(row["completed_at"]), actual.CompletedAt);
            Assert.Equal(Money(row["resulting_balance"]), actual.ResultingBalance);
            Assert.Equal(OptionalGuid(row["transfer_id"]), actual.TransferId);
            Assert.Equal(OptionalGuid(row["counterparty_account_id"]), actual.CounterpartyAccountId);
        }

        var source = await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == Guid.Parse("00209a31-c483-41bb-a9fd-35530f8f680b"));
        Assert.Equal(52608.86m, source.Balance);
        Assert.Equal(4, await db.TransactionEntries.CountAsync(x => x.AccountId == source.Id));

        var second = await importer.ImportAsync(SeedPath());
        Assert.All(second.Counts.Values, count => Assert.Equal(0, count.Inserted));
        Assert.Equal(1002, await db.TransactionEntries.CountAsync());
        Assert.Equal(52608.86m, (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == source.Id)).Balance);
    }

    private static List<Dictionary<string, string>> ReadCsv(string name)
    {
        using var reader = File.OpenText(Path.Combine(SeedPath(), $"{name}.csv"));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? throw new InvalidOperationException("Missing header.");
        var rows = new List<Dictionary<string, string>>();
        while (csv.Read())
            rows.Add(headers.ToDictionary(header => header, header => csv.GetField(header) ?? ""));
        return rows;
    }

    private static decimal Money(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
    private static DateTimeOffset Time(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).ToUniversalTime();
    private static Guid? OptionalGuid(string value) => value.Length == 0 ? null : Guid.Parse(value);
}
