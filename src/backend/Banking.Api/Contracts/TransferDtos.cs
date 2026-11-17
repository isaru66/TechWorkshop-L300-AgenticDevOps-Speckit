namespace Banking.Api.Contracts;

public sealed record TransferRequest(Guid? SourceAccountId, Guid? DestinationAccountId, string? Amount, string? Currency);

public sealed record TransferOperationDto(TransferDto Transfer, BalanceDto SourceBalance,
    BalanceDto DestinationBalance, IReadOnlyList<TransactionEntryDto> Entries);
