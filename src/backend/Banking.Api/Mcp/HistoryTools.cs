using System.ComponentModel;
using Banking.Api.Contracts;
using Banking.Api.Controllers;
using Banking.Api.Services;
using ModelContextProtocol.Server;

namespace Banking.Api.Mcp;

[McpServerToolType]
public sealed class HistoryTools(BankingService service)
{
    [McpServerTool(Name = "get_transaction_history"), Description("Get an account's transaction history, newest first, paged.")]
    public Task<PageResult<TransactionEntryDto>> GetTransactionHistory(
        [Description("Account UUID.")] string accountId,
        [Description("Page number, starting at 1.")] int page = 1,
        [Description("Items per page, 1-100.")] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.HistoryAsync(RouteId.Parse(accountId), page, pageSize, cancellationToken, newestFirst: true));
}
