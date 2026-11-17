using Banking.Api.Contracts;
using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Banking.Api.Services;
using Banking.Api.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Services;

public sealed class DepositWithdrawalTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task ChangesBalanceAndHistoryAtomicallyAndRejectsInvalidOperations()
    {
        var id = await CreateAccount(100m);
        await using var db = fixture.CreateContext();
        var service = Service(db);
        var deposit = await service.ChangeBalanceAsync(id, "0.01", "THB", true, default);
        Assert.Equal("100.01", deposit.Balance);
        Assert.Equal("DEPOSIT", deposit.Transaction.Type);
        var withdrawal = await service.ChangeBalanceAsync(id, "100.01", "THB", false, default);
        Assert.Equal("0.00", withdrawal.Balance);
        Assert.Equal("WITHDRAWAL", withdrawal.Transaction.Type);
        foreach (var value in new[] { "0", "-1", "1.001", "NaN", "10000000000000000" })
            Assert.Equal("invalid_amount", (await Assert.ThrowsAsync<BankingProblem>(() =>
                service.ChangeBalanceAsync(id, value, "THB", true, default))).Code);
        Assert.Equal("insufficient_funds", (await Assert.ThrowsAsync<BankingProblem>(() =>
            service.ChangeBalanceAsync(id, "0.01", "THB", false, default))).Code);
        Assert.Equal(2, await db.TransactionEntries.CountAsync(x => x.AccountId == id));

        var limitId = await CreateAccount(MoneyValue.Maximum);
        Assert.Equal("balance_limit_exceeded", (await Assert.ThrowsAsync<BankingProblem>(() =>
            service.ChangeBalanceAsync(limitId, "0.01", "THB", true, default))).Code);
    }

    [Fact]
    public async Task SerializesCompetingWithdrawalsAndRollsBackBeforeCommit()
    {
        var id = await CreateAccount(10m);
        async Task<string> Withdraw()
        {
            await using var context = fixture.CreateContext();
            try { await Service(context).ChangeBalanceAsync(id, "10.00", "THB", false, default); return "success"; }
            catch (BankingProblem problem) { return problem.Code; }
        }
        var outcomes = await Task.WhenAll(Withdraw(), Withdraw());
        Assert.Equal(new[] { "insufficient_funds", "success" }, outcomes.Order());
        await using var db = fixture.CreateContext();
        Assert.Equal(0m, (await db.Accounts.SingleAsync(x => x.Id == id)).Balance);
        Assert.Equal(1, await db.TransactionEntries.CountAsync(x => x.AccountId == id));

        var rollbackId = await CreateAccount(20m);
        await using var rollbackContext = fixture.CreateContext();
        var failing = new BankingService(rollbackContext, new BankingTransactionCoordinator(rollbackContext, new ThrowingObserver()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing.ChangeBalanceAsync(rollbackId, "1.00", "THB", true, default));
        await using var verify = fixture.CreateContext();
        Assert.Equal(20m, (await verify.Accounts.SingleAsync(x => x.Id == rollbackId)).Balance);
        Assert.Equal(0, await verify.TransactionEntries.CountAsync(x => x.AccountId == rollbackId));
    }

    private async Task<Guid> CreateAccount(decimal balance)
    {
        await using var db = fixture.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Fictional", CreatedAt = DateTimeOffset.UtcNow };
        var account = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = balance, CreatedAt = DateTimeOffset.UtcNow };
        db.Customers.Add(customer);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private static BankingService Service(BankingDbContext db) => new(db, new BankingTransactionCoordinator(db, new BankingCommitObserver()));

    private sealed class ThrowingObserver : IBankingCommitObserver
    {
        public Task BeforeCommitAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Injected failure.");
    }
}
