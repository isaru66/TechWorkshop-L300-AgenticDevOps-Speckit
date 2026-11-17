using Banking.Api.Contracts;
using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Services;

public sealed class BankingService(BankingDbContext db, BankingTransactionCoordinator coordinator)
{
    public async Task<TransferOperationDto> TransferAsync(Guid sourceId, Guid destinationId, string? value, string? currency, CancellationToken cancellationToken)
    {
        if (sourceId == destinationId)
            throw BankingProblem.SameAccount();
        if (!MoneyValue.TryParse(value, out var amount))
            throw BankingProblem.InvalidAmount();
        if (string.IsNullOrEmpty(currency))
            throw BankingProblem.InvalidInput("Currency is required.");
        if (currency != "THB")
            throw BankingProblem.CurrencyMismatch();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await coordinator.AcquireWriterLockAsync(cancellationToken);
        var accounts = await coordinator.LockAccountsAsync([sourceId, destinationId], cancellationToken);
        if (accounts.Count != 2)
            throw BankingProblem.NotFound();
        foreach (var account in accounts)
            await db.Entry(account).ReloadAsync(cancellationToken);
        var source = accounts.Single(x => x.Id == sourceId);
        var destination = accounts.Single(x => x.Id == destinationId);
        if (source.Currency != currency || destination.Currency != currency)
            throw BankingProblem.CurrencyMismatch();
        if (source.Balance < amount)
            throw BankingProblem.InsufficientFunds();
        if (destination.Balance > MoneyValue.Maximum - amount)
            throw BankingProblem.BalanceLimit();

        source.Balance -= amount;
        destination.Balance += amount;
        var now = DateTimeOffset.UtcNow;
        now = now.AddTicks(-(now.Ticks % 10));
        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            Amount = amount,
            Currency = currency,
            CompletedAt = now
        };
        var outgoing = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            Sequence = await coordinator.NextSequenceAsync(cancellationToken),
            AccountId = sourceId,
            Type = "TRANSFER_OUT",
            Amount = amount,
            Currency = currency,
            CompletedAt = now,
            ResultingBalance = source.Balance,
            TransferId = transfer.Id,
            CounterpartyAccountId = destinationId
        };
        var incoming = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            Sequence = await coordinator.NextSequenceAsync(cancellationToken),
            AccountId = destinationId,
            Type = "TRANSFER_IN",
            Amount = amount,
            Currency = currency,
            CompletedAt = now,
            ResultingBalance = destination.Balance,
            TransferId = transfer.Id,
            CounterpartyAccountId = sourceId
        };
        db.Transfers.Add(transfer);
        db.TransactionEntries.AddRange(outgoing, incoming);
        await db.SaveChangesAsync(cancellationToken);
        await coordinator.BeforeCommitAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new TransferOperationDto(TransferDto.From(transfer), BalanceDto.From(source), BalanceDto.From(destination),
            [TransactionEntryDto.From(outgoing), TransactionEntryDto.From(incoming)]);
    }

    public async Task<MoneyOperationDto> ChangeBalanceAsync(Guid id, string? value, string? currency, bool deposit, CancellationToken cancellationToken)
    {
        if (!MoneyValue.TryParse(value, out var amount))
            throw BankingProblem.InvalidAmount();
        if (string.IsNullOrEmpty(currency))
            throw BankingProblem.InvalidInput("Currency is required.");
        if (currency != "THB")
            throw BankingProblem.CurrencyMismatch();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await coordinator.AcquireWriterLockAsync(cancellationToken);
        var account = (await coordinator.LockAccountsAsync([id], cancellationToken)).SingleOrDefault()
            ?? throw BankingProblem.NotFound();
        await db.Entry(account).ReloadAsync(cancellationToken);
        if (account.Currency != currency)
            throw BankingProblem.CurrencyMismatch();
        if (!deposit && account.Balance < amount)
            throw BankingProblem.InsufficientFunds();
        if (deposit && account.Balance > MoneyValue.Maximum - amount)
            throw BankingProblem.BalanceLimit();

        account.Balance += deposit ? amount : -amount;
        var now = DateTimeOffset.UtcNow;
        var entry = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            Sequence = await coordinator.NextSequenceAsync(cancellationToken),
            AccountId = id,
            Type = deposit ? "DEPOSIT" : "WITHDRAWAL",
            Amount = amount,
            Currency = currency,
            CompletedAt = now.AddTicks(-(now.Ticks % 10)),
            ResultingBalance = account.Balance
        };
        db.TransactionEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await coordinator.BeforeCommitAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new MoneyOperationDto(id, currency, MoneyValue.Format(account.Balance), TransactionEntryDto.From(entry));
    }

    public async Task<PageResult<CustomerDto>> CustomersAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await PageResult<CustomerDto>.CreateAsync(db.Customers.AsNoTracking().OrderBy(x => x.Id),
            page, pageSize, CustomerDto.From, cancellationToken);

    public async Task<CustomerDto> CustomerAsync(Guid id, CancellationToken cancellationToken) =>
        CustomerDto.From(await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw BankingProblem.NotFound());

    public async Task<PageResult<AccountDto>> CustomerAccountsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
            throw BankingProblem.NotFound();
        return await PageResult<AccountDto>.CreateAsync(db.Accounts.AsNoTracking().Where(x => x.CustomerId == id).OrderBy(x => x.Id),
            page, pageSize, AccountDto.From, cancellationToken);
    }

    public async Task<PageResult<AccountDto>> AccountsAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await PageResult<AccountDto>.CreateAsync(db.Accounts.AsNoTracking().OrderBy(x => x.Id),
            page, pageSize, AccountDto.From, cancellationToken);

    public async Task<AccountDto> AccountAsync(Guid id, CancellationToken cancellationToken) =>
        AccountDto.From(await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw BankingProblem.NotFound());

    public async Task<BalanceDto> BalanceAsync(Guid id, CancellationToken cancellationToken) =>
        BalanceDto.From(await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw BankingProblem.NotFound());

    public async Task<PageResult<TransactionEntryDto>> HistoryAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await db.Accounts.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
            throw BankingProblem.NotFound();
        return await PageResult<TransactionEntryDto>.CreateAsync(
            db.TransactionEntries.AsNoTracking().Where(x => x.AccountId == id).OrderBy(x => x.Sequence),
            page, pageSize, TransactionEntryDto.From, cancellationToken);
    }

    public async Task<TransactionEntryDto> EntryAsync(Guid id, CancellationToken cancellationToken) =>
        TransactionEntryDto.From(await db.TransactionEntries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw BankingProblem.NotFound());

    public async Task<PageResult<TransferDto>> TransfersAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await PageResult<TransferDto>.CreateAsync(db.Transfers.AsNoTracking().OrderBy(x => x.CompletedAt).ThenBy(x => x.Id),
            page, pageSize, TransferDto.From, cancellationToken);

    public async Task<TransferDto> TransferAsync(Guid id, CancellationToken cancellationToken) =>
        TransferDto.From(await db.Transfers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw BankingProblem.NotFound());
}
