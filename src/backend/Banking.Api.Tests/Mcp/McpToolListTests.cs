using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Mcp;

public sealed class McpToolListTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task InitializeAndToolsListExposeAllTools()
    {
        await using var mcp = new McpTestClient(fixture);
        var init = await mcp.RpcAsync("initialize", new
        {
            protocolVersion = "2025-06-18",
            capabilities = new { },
            clientInfo = new { name = "tests", version = "1.0" }
        });
        Assert.True(init.GetProperty("result").TryGetProperty("capabilities", out _));

        var list = await mcp.RpcAsync("tools/list");
        var names = list.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()).Order().ToArray();
        string[] expected =
        [
            "create_account", "create_customer", "delete_account", "delete_customer", "deposit", "get_account",
            "get_customer", "get_transaction_history", "list_accounts", "list_customers", "transfer_funds",
            "update_customer", "withdraw"
        ];
        Assert.Equal(expected.Order(), names);
    }
}
