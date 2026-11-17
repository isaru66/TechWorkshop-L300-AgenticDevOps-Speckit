namespace Banking.Api.Contracts;

public sealed record MoneyOperationRequest(string? Amount, string? Currency);

public sealed record MoneyOperationDto(Guid AccountId, string Currency, string Balance, TransactionEntryDto Transaction);
