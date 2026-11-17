namespace Banking.Api.Data.Entities;

public sealed class Transfer
{
    public Guid Id { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
