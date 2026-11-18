namespace Banking.Api.Contracts;

public sealed class BankingProblem(int status, string code, string title, string detail) : Exception(detail)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public string Title { get; } = title;

    public static BankingProblem InvalidInput(string detail) => new(400, "invalid_input", "Invalid input", detail);
    public static BankingProblem InvalidAmount() => new(400, "invalid_amount", "Invalid amount", "Enter a positive THB amount with at most two decimal places.");
    public static BankingProblem NotFound() => new(404, "not_found", "Not found", "The requested record was not found.");
    public static BankingProblem CurrencyMismatch() => new(400, "currency_mismatch", "Currency mismatch", "The currency must match the selected THB accounts.");
    public static BankingProblem SameAccount() => new(400, "same_account", "Same account", "Select two different accounts.");
    public static BankingProblem InsufficientFunds() => new(409, "insufficient_funds", "Insufficient funds", "The source account has insufficient funds for this operation.");
    public static BankingProblem BalanceLimit() => new(409, "balance_limit_exceeded", "Balance limit exceeded", "The operation would exceed the supported balance.");
    public static BankingProblem HasDependents(string detail) => new(409, "has_dependents", "Has dependents", detail);
}
