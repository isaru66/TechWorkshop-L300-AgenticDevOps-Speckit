using Banking.Api.Contracts;
using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Banking.Api.Services;
using Banking.Api.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Services;

public sealed class TransferTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TransfersBothSidesAndRejectsWithoutMutation()
    {
        var (source, destination) = await Accounts(100m, 20m);
        await using var db = fixture.CreateContext();
        var service = Service(db);
        var result = await service.TransferAsync(source, destination, "100.00", "THB", default);
        Assert.Equal("0.00", result.SourceBalance.Balance);
        Assert.Equal("120.00", result.DestinationBalance.Balance);
        Assert.Equal(new[] { "TRANSFER_OUT", "TRANSFER_IN" }, result.Entries.Select(x => x.Type));
        Assert.All(result.Entries, x => Assert.Equal(result.Transfer.Id, x.TransferId));
        Assert.Equal(destination, result.Entries[0].CounterpartyAccountId);
        Assert.Equal(source, result.Entries[1].CounterpartyAccountId);
        Assert.Equal(result.Entries[0].CompletedAt, result.Entries[1].CompletedAt);
        Assert.Equal("insufficient_funds", (await Assert.ThrowsAsync<BankingProblem>(() =>
            service.TransferAsync(source, destination, "0.01", "THB", default))).Code);
        Assert.Equal("same_account", (await Assert.ThrowsAsync<BankingProblem>(() =>
            service.TransferAsync(source, source, "1", "THB", default))).Code);
        Assert.Equal("not_found", (await Assert.ThrowsAsync<BankingProblem>(() =>
            service.TransferAsync(source, Guid.NewGuid(), "1", "THB", default))).Code);
        Assert.Equal("currency_mismatch", (await Assert.ThrowsAsync<BankingProblem>(() =>
            service.TransferAsync(source, destination, "1", "USD", default))).Code);
        Assert.Equal(1, await db.Transfers.CountAsync(x => x.SourceAccountId == source));
        Assert.Equal(2, await db.TransactionEntries.CountAsync(x => x.AccountId == source || x.AccountId == destination));
        var (funded, full) = await Accounts(1m, MoneyValue.Maximum);
        Assert.Equal("balance_limit_exceeded", (await Assert.ThrowsAsync<BankingProblem>(() =>
            service.TransferAsync(funded, full, "1", "THB", default))).Code);
    }

    [Fact]
    public async Task CompetingAndOppositeTransfersSerializeAndInjectedFailureRollsBack()
    {
        var (source, destination) = await Accounts(10m, 10m);
        async Task<string> Move(Guid from, Guid to, string amount)
        {
            await using var context = fixture.CreateContext();
            try { await Service(context).TransferAsync(from, to, amount, "THB", default); return "success"; }
            catch (BankingProblem problem) { return problem.Code; }
        }
        var outcomes = await Task.WhenAll(Move(source, destination, "10"), Move(source, destination, "10"));
        Assert.Equal(new[] { "insufficient_funds", "success" }, outcomes.Order());
        var (otherSource, otherDestination) = await Accounts(10m, 10m);
        var opposite = await Task.WhenAll(Move(otherSource, otherDestination, "1"), Move(otherDestination, otherSource, "1"));
        Assert.All(opposite, result => Assert.Equal("success", result));
        await using var db = fixture.CreateContext();
        var total = await db.Accounts.Where(x => x.Id == source || x.Id == destination).SumAsync(x => x.Balance);
        Assert.Equal(20m, total);
        var beforeSource = (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == destination)).Balance;
        var beforeDestination = (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == source)).Balance;
        var transferCount = await db.Transfers.CountAsync();
        var entryCount = await db.TransactionEntries.CountAsync();
        await using var failingDb = fixture.CreateContext();
        var failing = new BankingService(failingDb, new BankingTransactionCoordinator(failingDb, new ThrowingObserver()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing.TransferAsync(destination, source, "1", "THB", default));
        await using var verify = fixture.CreateContext();
        Assert.Equal(beforeSource, (await verify.Accounts.SingleAsync(x => x.Id == destination)).Balance);
        Assert.Equal(beforeDestination, (await verify.Accounts.SingleAsync(x => x.Id == source)).Balance);
        Assert.Equal(transferCount, await verify.Transfers.CountAsync());
        Assert.Equal(entryCount, await verify.TransactionEntries.CountAsync());
    }

    private async Task<(Guid, Guid)> Accounts(decimal sourceBalance, decimal destinationBalance)
    {
        await using var db = fixture.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Fictional", CreatedAt = DateTimeOffset.UtcNow };
        var source = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = sourceBalance, CreatedAt = DateTimeOffset.UtcNow };
        var destination = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = destinationBalance, CreatedAt = DateTimeOffset.UtcNow };
        db.Customers.Add(customer);
        db.Accounts.AddRange(source, destination);
        await db.SaveChangesAsync();
        return (source.Id, destination.Id);
    }

    private static BankingService Service(BankingDbContext db) => new(db, new BankingTransactionCoordinator(db, new BankingCommitObserver()));
    private sealed class ThrowingObserver : IBankingCommitObserver
    {
        public Task BeforeCommitAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Injected failure.");
    }
}
