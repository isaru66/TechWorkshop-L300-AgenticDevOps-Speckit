using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Mcp;

public sealed class HistoryToolTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task HistoryIsNewestFirstWithCounterparty()
    {
        await using var mcp = new McpTestClient(fixture);
        var customer = await mcp.NewCustomerAsync();
        var a = await mcp.NewAccountAsync(customer);
        var b = await mcp.NewAccountAsync(customer);
        await mcp.CallAsync("deposit", new { accountId = a, amount = "1000.00" });
        await mcp.CallAsync("transfer_funds", new { sourceAccountId = a, destinationAccountId = b, amount = "250.00" });

        var history = await mcp.CallAsync("get_transaction_history", new { accountId = a });
        var items = history.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(["TRANSFER_OUT", "DEPOSIT"], items.Select(x => x.GetProperty("type").GetString()!).ToArray());
        Assert.Equal("250.00", items[0].GetProperty("amount").GetString());
        Assert.Equal("750.00", items[0].GetProperty("resultingBalance").GetString());
        Assert.Equal(b, items[0].GetProperty("counterpartyAccountId").GetString());

        var paged = await mcp.CallAsync("get_transaction_history", new { accountId = a, page = 2, pageSize = 1 });
        Assert.Equal("DEPOSIT", paged.GetProperty("items")[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task EmptyAndUnknownAccounts()
    {
        await using var mcp = new McpTestClient(fixture);
        var account = await mcp.NewAccountAsync(await mcp.NewCustomerAsync());
        var history = await mcp.CallAsync("get_transaction_history", new { accountId = account });
        Assert.Empty(history.GetProperty("items").EnumerateArray());
        Assert.Contains("not_found:", await mcp.ErrorAsync("get_transaction_history", new { accountId = Guid.NewGuid() }));
    }
}
