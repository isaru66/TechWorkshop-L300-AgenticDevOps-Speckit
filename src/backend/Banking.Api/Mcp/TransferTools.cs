using System.ComponentModel;
using Banking.Api.Contracts;
using Banking.Api.Controllers;
using Banking.Api.Services;
using ModelContextProtocol.Server;

namespace Banking.Api.Mcp;

[McpServerToolType]
public sealed class TransferTools(BankingService service)
{
    [McpServerTool(Name = "transfer_funds"), Description("Transfer THB between two different accounts atomically.")]
    public Task<TransferOperationDto> TransferFunds(
        [Description("Source account UUID.")] string sourceAccountId,
        [Description("Destination account UUID.")] string destinationAccountId,
        [Description("Positive THB amount as a decimal string with at most two decimals, e.g. \"250.00\".")] string amount,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.TransferAsync(RouteId.Parse(sourceAccountId), RouteId.Parse(destinationAccountId),
            amount, "THB", cancellationToken));
}
