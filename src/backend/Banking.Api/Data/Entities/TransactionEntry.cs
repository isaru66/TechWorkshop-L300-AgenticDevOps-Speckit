namespace Banking.Api.Data.Entities;

public sealed class TransactionEntry
{
    public Guid Id { get; set; }
    public long Sequence { get; set; }
    public Guid AccountId { get; set; }
    public required string Type { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public decimal ResultingBalance { get; set; }
    public Guid? TransferId { get; set; }
    public Guid? CounterpartyAccountId { get; set; }
}
