using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Integration;

public sealed class DatabaseMappingTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task MapsMoneyAndLocksAccountsInsideTransaction()
    {
        await using var db = fixture.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Fictional", CreatedAt = DateTimeOffset.UtcNow };
        var account = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = 101.25m, CreatedAt = DateTimeOffset.UtcNow };
        db.Customers.Add(customer);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await using var transaction = await db.Database.BeginTransactionAsync();
        var coordinator = new BankingTransactionCoordinator(db, new BankingCommitObserver());
        await coordinator.AcquireWriterLockAsync(default);
        var locked = await coordinator.LockAccountsAsync([account.Id], default);
        var sequence = await coordinator.NextSequenceAsync(default);

        Assert.Single(locked);
        Assert.Equal(101.25m, locked[0].Balance);
        Assert.True(sequence > 0);
        await transaction.CommitAsync();
    }

    [Fact]
    public async Task RejectsNegativeBalancesAtDatabaseBoundary()
    {
        await using var db = fixture.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Fictional", CreatedAt = DateTimeOffset.UtcNow };
        db.Customers.Add(customer);
        db.Accounts.Add(new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = -0.01m, CreatedAt = DateTimeOffset.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
