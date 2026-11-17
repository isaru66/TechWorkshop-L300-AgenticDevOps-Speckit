using Banking.Api.Data.Entities;
using System.Globalization;

namespace Banking.Api.Contracts;

public sealed record CustomerDto(Guid Id, string Name, DateTimeOffset CreatedAt, string? EnglishName,
    string? Email, string? Phone, string? AddressLine, string? Subdistrict, string? District,
    string? Province, string? Postcode, DateOnly? DateOfBirth, string? Occupation, string? NationalId)
{
    public static CustomerDto From(Customer customer) => new(customer.Id, customer.Name, customer.CreatedAt,
        customer.EnglishName, customer.Email, customer.Phone, customer.AddressLine, customer.Subdistrict,
        customer.District, customer.Province, customer.Postcode, customer.DateOfBirth,
        customer.Occupation, customer.NationalId);
}

public sealed record AccountDto(Guid Id, Guid CustomerId, string Currency, string Balance, DateTimeOffset CreatedAt)
{
    public static AccountDto From(Account account) => new(account.Id, account.CustomerId, account.Currency,
        MoneyValue.Format(account.Balance), account.CreatedAt);
}

public sealed record BalanceDto(Guid AccountId, string Currency, string Balance)
{
    public static BalanceDto From(Account account) => new(account.Id, account.Currency, MoneyValue.Format(account.Balance));
}

public sealed record TransferDto(Guid Id, Guid SourceAccountId, Guid DestinationAccountId,
    string Amount, string Currency, DateTimeOffset CompletedAt)
{
    public static TransferDto From(Transfer transfer) => new(transfer.Id, transfer.SourceAccountId,
        transfer.DestinationAccountId, MoneyValue.Format(transfer.Amount), transfer.Currency, transfer.CompletedAt);
}

public sealed record TransactionEntryDto(Guid Id, string Sequence, Guid AccountId, string Type,
    string Amount, string Currency, DateTimeOffset CompletedAt, string ResultingBalance,
    Guid? TransferId, Guid? CounterpartyAccountId)
{
    public static TransactionEntryDto From(TransactionEntry entry) => new(entry.Id,
        entry.Sequence.ToString(CultureInfo.InvariantCulture), entry.AccountId, entry.Type,
        MoneyValue.Format(entry.Amount), entry.Currency, entry.CompletedAt, MoneyValue.Format(entry.ResultingBalance),
        entry.TransferId, entry.CounterpartyAccountId);
}
