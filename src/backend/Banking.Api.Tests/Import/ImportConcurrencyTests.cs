using Banking.Api.Data;
using Banking.Api.Import;
using Banking.Api.Services;
using Banking.Api.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Import;

public sealed class ImportConcurrencyTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task OverlappingImportsAndWriterAreCoordinatedWithoutLosingActivity()
    {
        async Task<ImportResult> Import()
        {
            await using var context = fixture.CreateContext();
            return await new SeedImporter(context, new BankingTransactionCoordinator(context, new BankingCommitObserver()))
                .ImportAsync(BaselineImportTests.SeedPath());
        }
        var imports = await Task.WhenAll(Import(), Import());
        Assert.Equal(1355, imports.Sum(result => result.Counts.Values.Sum(count => count.Inserted)));
        Assert.Equal(1355, imports.Sum(result => result.Counts.Values.Sum(count => count.Skipped)));

        var observer = new BlockingObserver();
        await using var writerDb = fixture.CreateContext();
        var writer = new BankingService(writerDb, new BankingTransactionCoordinator(writerDb, observer));
        var source = Guid.Parse("00209a31-c483-41bb-a9fd-35530f8f680b");
        var mutation = writer.ChangeBalanceAsync(source, "1.00", "THB", true, default);
        await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var repeat = Import();
        try
        {
            await Task.Delay(250);
            Assert.False(repeat.IsCompleted);
        }
        finally
        {
            observer.Release.TrySetResult();
        }
        var operation = await mutation;
        var result = await repeat;
        Assert.All(result.Counts.Values, count => Assert.Equal(0, count.Inserted));
        Assert.True(long.Parse(operation.Transaction.Sequence) > 1002);
        await using var verify = fixture.CreateContext();
        Assert.Equal(52609.86m, (await verify.Accounts.SingleAsync(x => x.Id == source)).Balance);
        Assert.Equal(1003, await verify.TransactionEntries.CountAsync());
        Assert.Equal(1003, await verify.TransactionEntries.Select(x => x.Sequence).Distinct().CountAsync());

        await using (var failedDb = fixture.CreateContext())
        {
            var failed = new BankingService(failedDb, new BankingTransactionCoordinator(failedDb, new ThrowingObserver()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => failed.ChangeBalanceAsync(source, "0.01", "THB", true, default));
        }
        await using (var nextDb = fixture.CreateContext())
        {
            var next = new BankingService(nextDb, new BankingTransactionCoordinator(nextDb, new BankingCommitObserver()));
            Assert.Equal("1005", (await next.ChangeBalanceAsync(source, "0.01", "THB", true, default)).Transaction.Sequence);
        }
        var files = new[] { "customers", "accounts", "transfers", "transaction_entries" }
            .ToDictionary(type => type, type => File.ReadAllText(Path.Combine(BaselineImportTests.SeedPath(), $"{type}.csv")));
        var entryRow = files["transaction_entries"].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .First(x => x.Contains(",DEPOSIT,", StringComparison.Ordinal)).TrimEnd('\r').Split(',');
        entryRow[0] = Guid.NewGuid().ToString();
        entryRow[1] = "1004";
        files["transaction_entries"] += "\n" + string.Join(',', entryRow) + "\n";
        var extension = SeedCsvReader.ReadSources(type => new StringReader(files[type]));
        await using (var importDb = fixture.CreateContext())
        {
            var importer = new SeedImporter(importDb, new BankingTransactionCoordinator(importDb, new BankingCommitObserver()));
            Assert.Equal(1, (await importer.ImportBatchAsync(extension)).Counts["transaction_entries"].Inserted);
        }
        await using (var finalDb = fixture.CreateContext())
        {
            var final = new BankingService(finalDb, new BankingTransactionCoordinator(finalDb, new BankingCommitObserver()));
            var later = await final.ChangeBalanceAsync(source, "0.01", "THB", true, default);
            Assert.Equal("1006", later.Transaction.Sequence);
            Assert.Equal("52609.88", later.Balance);
        }
        entryRow[0] = Guid.NewGuid().ToString();
        entryRow[1] = "1006";
        files["transaction_entries"] += "\n" + string.Join(',', entryRow) + "\n";
        var conflicting = SeedCsvReader.ReadSources(type => new StringReader(files[type]));
        await using var collisionDb = fixture.CreateContext();
        var collisionImporter = new SeedImporter(collisionDb, new BankingTransactionCoordinator(collisionDb, new BankingCommitObserver()));
        var before = await RepeatImportTests.Snapshot(collisionDb);
        var error = await Assert.ThrowsAsync<SeedInputException>(() => collisionImporter.ImportBatchAsync(conflicting));
        Assert.Contains("transaction_entries.csv", error.Message);
        Assert.Contains(entryRow[0], error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1006", error.Message);
        Assert.Equal(before, await RepeatImportTests.Snapshot(collisionDb));
    }

    private sealed class BlockingObserver : IBankingCommitObserver
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task BeforeCommitAsync(CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class ThrowingObserver : IBankingCommitObserver
    {
        public Task BeforeCommitAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Injected failure.");
    }
}
