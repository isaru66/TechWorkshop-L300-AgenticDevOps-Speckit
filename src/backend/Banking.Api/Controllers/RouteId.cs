using Banking.Api.Contracts;

namespace Banking.Api.Controllers;

internal static class RouteId
{
    public static Guid Parse(string value) =>
        Guid.TryParse(value, out var id) ? id : throw BankingProblem.InvalidInput("The identifier must be a UUID.");
}
