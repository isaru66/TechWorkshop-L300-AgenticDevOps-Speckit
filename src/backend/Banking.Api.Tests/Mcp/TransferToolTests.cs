using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Mcp;

public sealed class TransferToolTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private async Task<(McpTestClient Mcp, string A, string B)> SetupAsync(string fund = "1000.00")
    {
        var mcp = new McpTestClient(fixture);
        var customer = await mcp.NewCustomerAsync();
        var a = await mcp.NewAccountAsync(customer);
        var b = await mcp.NewAccountAsync(customer);
        await mcp.CallAsync("deposit", new { accountId = a, amount = fund });
        return (mcp, a, b);
    }

    [Fact]
    public async Task SuccessfulTransferUpdatesBothBalances()
    {
        var (mcp, a, b) = await SetupAsync();
        await using var _ = mcp;
        var result = await mcp.CallAsync("transfer_funds", new { sourceAccountId = a, destinationAccountId = b, amount = "250.00" });
        Assert.Equal("750.00", result.GetProperty("sourceBalance").GetProperty("balance").GetString());
        Assert.Equal("250.00", result.GetProperty("destinationBalance").GetProperty("balance").GetString());
        Assert.Equal(["TRANSFER_OUT", "TRANSFER_IN"],
            result.GetProperty("entries").EnumerateArray().Select(x => x.GetProperty("type").GetString()!).ToArray());
        Assert.Equal("750.00", await mcp.BalanceAsync(a));
        Assert.Equal("250.00", await mcp.BalanceAsync(b));
    }

    [Fact]
    public async Task RejectedTransfersLeaveBalancesUnchanged()
    {
        var (mcp, a, b) = await SetupAsync();
        await using var _ = mcp;
        (object Args, string Code)[] cases =
        [
            (new { sourceAccountId = a, destinationAccountId = b, amount = "1000.01" }, "insufficient_funds"),
            (new { sourceAccountId = a, destinationAccountId = b, amount = "0" }, "invalid_amount"),
            (new { sourceAccountId = a, destinationAccountId = b, amount = "-5.00" }, "invalid_amount"),
            (new { sourceAccountId = a, destinationAccountId = b, amount = "1.001" }, "invalid_amount"),
            (new { sourceAccountId = a, destinationAccountId = a, amount = "1.00" }, "same_account"),
            (new { sourceAccountId = a, destinationAccountId = Guid.NewGuid(), amount = "1.00" }, "not_found"),
        ];
        foreach (var (args, code) in cases)
            Assert.Contains($"{code}:", await mcp.ErrorAsync("transfer_funds", args));
        Assert.Equal("1000.00", await mcp.BalanceAsync(a));
        Assert.Equal("0.00", await mcp.BalanceAsync(b));
    }

    [Fact]
    public async Task WithdrawEnforcesFunds()
    {
        var (mcp, a, _) = await SetupAsync("50.00");
        await using var _ = mcp;
        var result = await mcp.CallAsync("withdraw", new { accountId = a, amount = "20.00" });
        Assert.Equal("30.00", result.GetProperty("balance").GetString());
        Assert.Contains("insufficient_funds:", await mcp.ErrorAsync("withdraw", new { accountId = a, amount = "30.01" }));
    }
}
