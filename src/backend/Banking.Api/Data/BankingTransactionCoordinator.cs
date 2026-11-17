using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Data;

public interface IBankingCommitObserver
{
    Task BeforeCommitAsync(CancellationToken cancellationToken);
}

public sealed class BankingCommitObserver : IBankingCommitObserver
{
    public Task BeforeCommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class BankingTransactionCoordinator(BankingDbContext db, IBankingCommitObserver observer)
{
    private const long SeedLockKey = 0x42616e6b53656564;

    public Task AcquireWriterLockAsync(CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({SeedLockKey})", cancellationToken);

    public Task AcquireImportLockAsync(CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({SeedLockKey})", cancellationToken);

    public Task<List<Account>> LockAccountsAsync(Guid[] ids, CancellationToken cancellationToken) =>
        db.Accounts.FromSqlInterpolated($"SELECT * FROM accounts WHERE id = ANY ({ids}) ORDER BY id FOR UPDATE")
            .ToListAsync(cancellationToken);

    public Task<long> NextSequenceAsync(CancellationToken cancellationToken) =>
        db.Database.SqlQueryRaw<long>("SELECT nextval('transaction_entry_sequence') AS \"Value\"")
            .SingleAsync(cancellationToken);

    public Task BeforeCommitAsync(CancellationToken cancellationToken) => observer.BeforeCommitAsync(cancellationToken);
}
