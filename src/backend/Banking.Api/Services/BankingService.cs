using Banking.Api.Contracts;
using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        await coordinator.CommitAsync(transaction, cancellationToken);
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
        await coordinator.CommitAsync(transaction, cancellationToken);
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

    public async Task<PageResult<TransactionEntryDto>> HistoryAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken, bool newestFirst = false)
    {
        if (!await db.Accounts.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
            throw BankingProblem.NotFound();
        var entries = db.TransactionEntries.AsNoTracking().Where(x => x.AccountId == id);
        return await PageResult<TransactionEntryDto>.CreateAsync(
            newestFirst ? entries.OrderByDescending(x => x.Sequence) : entries.OrderBy(x => x.Sequence),
            page, pageSize, TransactionEntryDto.From, cancellationToken);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CustomerWriteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw BankingProblem.InvalidInput("Name is required.");
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = TruncatedNow(),
            EnglishName = request.EnglishName,
            Email = request.Email,
            Phone = request.Phone,
            AddressLine = request.AddressLine,
            Subdistrict = request.Subdistrict,
            District = request.District,
            Province = request.Province,
            Postcode = request.Postcode,
            DateOfBirth = request.DateOfBirth,
            Occupation = request.Occupation,
            NationalId = request.NationalId
        };
        db.Customers.Add(customer);
        await SaveWriteAsync(cancellationToken);
        return CustomerDto.From(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid id, CustomerWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
            throw BankingProblem.InvalidInput("Name cannot be blank.");
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw BankingProblem.NotFound();
        if (request.Name is not null) customer.Name = request.Name.Trim();
        customer.EnglishName = request.EnglishName ?? customer.EnglishName;
        customer.Email = request.Email ?? customer.Email;
        customer.Phone = request.Phone ?? customer.Phone;
        customer.AddressLine = request.AddressLine ?? customer.AddressLine;
        customer.Subdistrict = request.Subdistrict ?? customer.Subdistrict;
        customer.District = request.District ?? customer.District;
        customer.Province = request.Province ?? customer.Province;
        customer.Postcode = request.Postcode ?? customer.Postcode;
        customer.DateOfBirth = request.DateOfBirth ?? customer.DateOfBirth;
        customer.Occupation = request.Occupation ?? customer.Occupation;
        customer.NationalId = request.NationalId ?? customer.NationalId;
        await SaveWriteAsync(cancellationToken);
        return CustomerDto.From(customer);
    }

    public async Task DeleteCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw BankingProblem.NotFound();
        if (await db.Accounts.AnyAsync(x => x.CustomerId == id, cancellationToken))
            throw BankingProblem.HasDependents("The customer still owns accounts and cannot be deleted.");
        db.Customers.Remove(customer);
        await SaveWriteAsync(cancellationToken);
    }

    public async Task<AccountDto> CreateAccountAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == customerId, cancellationToken))
            throw BankingProblem.NotFound();
        var account = new Account { Id = Guid.NewGuid(), CustomerId = customerId, Currency = "THB", Balance = 0m, CreatedAt = TruncatedNow() };
        db.Accounts.Add(account);
        await SaveWriteAsync(cancellationToken);
        return AccountDto.From(account);
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await coordinator.AcquireWriterLockAsync(cancellationToken);
        var account = (await coordinator.LockAccountsAsync([id], cancellationToken)).SingleOrDefault()
            ?? throw BankingProblem.NotFound();
        await db.Entry(account).ReloadAsync(cancellationToken);
        if (account.Balance != 0m ||
            await db.TransactionEntries.AnyAsync(x => x.AccountId == id || x.CounterpartyAccountId == id, cancellationToken) ||
            await db.Transfers.AnyAsync(x => x.SourceAccountId == id || x.DestinationAccountId == id, cancellationToken))
            throw BankingProblem.HasDependents("The account has a balance or transaction history and cannot be deleted.");
        db.Accounts.Remove(account);
        await SaveWriteAsync(cancellationToken);
        await coordinator.CommitAsync(transaction, cancellationToken);
    }

    private static DateTimeOffset TruncatedNow()
    {
        var now = DateTimeOffset.UtcNow;
        return now.AddTicks(-(now.Ticks % 10));
    }

    private async Task SaveWriteAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw BankingProblem.HasDependents("The record is referenced by other records.");
        }
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
