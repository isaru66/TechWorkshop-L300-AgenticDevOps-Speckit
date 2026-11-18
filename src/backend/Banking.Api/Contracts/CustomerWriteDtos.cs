namespace Banking.Api.Contracts;

public sealed record CustomerWriteRequest(string? Name, string? EnglishName, string? Email, string? Phone,
    string? AddressLine, string? Subdistrict, string? District, string? Province, string? Postcode,
    DateOnly? DateOfBirth, string? Occupation, string? NationalId);
