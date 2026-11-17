namespace Banking.Api.Data.Entities;

public sealed class Account
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public required string Currency { get; set; }
    public decimal Balance { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
