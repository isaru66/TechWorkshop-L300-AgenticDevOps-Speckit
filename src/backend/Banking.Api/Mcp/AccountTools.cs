using System.ComponentModel;
using Banking.Api.Contracts;
using Banking.Api.Controllers;
using Banking.Api.Services;
using ModelContextProtocol.Server;

namespace Banking.Api.Mcp;

[McpServerToolType]
public sealed class AccountTools(BankingService service)
{
    [McpServerTool(Name = "list_accounts"), Description("List accounts, paged. Optionally filter by customer.")]
    public Task<PageResult<AccountDto>> ListAccounts(
        [Description("Only accounts of this customer UUID.")] string? customerId = null,
        [Description("Page number, starting at 1.")] int page = 1,
        [Description("Items per page, 1-100.")] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => customerId is null
            ? service.AccountsAsync(page, pageSize, cancellationToken)
            : service.CustomerAccountsAsync(RouteId.Parse(customerId), page, pageSize, cancellationToken));

    [McpServerTool(Name = "get_account"), Description("Get one account (including its THB balance) by ID.")]
    public Task<AccountDto> GetAccount(
        [Description("Account UUID.")] string accountId, CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.AccountAsync(RouteId.Parse(accountId), cancellationToken));

    [McpServerTool(Name = "create_account"), Description("Open a new THB account with a 0.00 balance for a customer.")]
    public Task<AccountDto> CreateAccount(
        [Description("Owner customer UUID.")] string customerId, CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.CreateAccountAsync(RouteId.Parse(customerId), cancellationToken));

    [McpServerTool(Name = "delete_account"), Description("Delete an account. Fails with has_dependents if it has a balance or history.")]
    public Task<object> DeleteAccount(
        [Description("Account UUID.")] string accountId, CancellationToken cancellationToken = default) =>
        McpErrors.RunDelete(() => service.DeleteAccountAsync(RouteId.Parse(accountId), cancellationToken));

    [McpServerTool(Name = "deposit"), Description("Deposit THB into an account.")]
    public Task<MoneyOperationDto> Deposit(
        [Description("Account UUID.")] string accountId,
        [Description("Positive THB amount as a decimal string with at most two decimals, e.g. \"100.00\".")] string amount,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.ChangeBalanceAsync(RouteId.Parse(accountId), amount, "THB", true, cancellationToken));

    [McpServerTool(Name = "withdraw"), Description("Withdraw THB from an account.")]
    public Task<MoneyOperationDto> Withdraw(
        [Description("Account UUID.")] string accountId,
        [Description("Positive THB amount as a decimal string with at most two decimals, e.g. \"100.00\".")] string amount,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.ChangeBalanceAsync(RouteId.Parse(accountId), amount, "THB", false, cancellationToken));
}
