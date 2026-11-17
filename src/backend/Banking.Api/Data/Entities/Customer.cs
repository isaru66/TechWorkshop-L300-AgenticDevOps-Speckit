namespace Banking.Api.Data.Entities;

public sealed class Customer
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? EnglishName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? Subdistrict { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? Postcode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Occupation { get; set; }
    public string? NationalId { get; set; }
}
