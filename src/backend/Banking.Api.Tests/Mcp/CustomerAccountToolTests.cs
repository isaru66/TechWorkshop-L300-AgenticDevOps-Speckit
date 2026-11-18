using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Mcp;

public sealed class CustomerAccountToolTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task CustomerLifecycle()
    {
        await using var mcp = new McpTestClient(fixture);
        var id = await mcp.NewCustomerAsync("Somchai");
        var got = await mcp.CallAsync("get_customer", new { customerId = id });
        Assert.Equal("Somchai", got.GetProperty("name").GetString());

        var list = await mcp.CallAsync("list_customers", new { });
        Assert.Contains(list.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetString() == id);

        var updated = await mcp.CallAsync("update_customer", new { customerId = id, phone = "0812345678" });
        Assert.Equal("0812345678", updated.GetProperty("phone").GetString());
        Assert.Equal("Somchai", updated.GetProperty("name").GetString());

        Assert.Contains("invalid_input:", await mcp.ErrorAsync("update_customer", new { customerId = id, name = " " }));
        await mcp.CallAsync("delete_customer", new { customerId = id });
        Assert.Contains("not_found:", await mcp.ErrorAsync("get_customer", new { customerId = id }));
    }

    [Fact]
    public async Task AccountLifecycleAndDependencyRules()
    {
        await using var mcp = new McpTestClient(fixture);
        var customer = await mcp.NewCustomerAsync();
        var account = await mcp.NewAccountAsync(customer);
        Assert.Equal("0.00", await mcp.BalanceAsync(account));

        var accounts = await mcp.CallAsync("list_accounts", new { customerId = customer });
        Assert.Single(accounts.GetProperty("items").EnumerateArray());

        Assert.Contains("has_dependents:", await mcp.ErrorAsync("delete_customer", new { customerId = customer }));

        await mcp.CallAsync("deposit", new { accountId = account, amount = "10.00" });
        Assert.Contains("has_dependents:", await mcp.ErrorAsync("delete_account", new { accountId = account }));
        await mcp.CallAsync("withdraw", new { accountId = account, amount = "10.00" });
        Assert.Contains("has_dependents:", await mcp.ErrorAsync("delete_account", new { accountId = account }));

        var empty = await mcp.NewAccountAsync(customer);
        await mcp.CallAsync("delete_account", new { accountId = empty });
        Assert.Contains("not_found:", await mcp.ErrorAsync("get_account", new { accountId = empty }));
    }

    [Fact]
    public async Task InvalidInputsAreReportedClearly()
    {
        await using var mcp = new McpTestClient(fixture);
        Assert.Contains("invalid_input:", await mcp.ErrorAsync("create_customer", new { name = "  " }));
        Assert.Contains("invalid_input:", await mcp.ErrorAsync("get_customer", new { customerId = "nope" }));
        Assert.Contains("not_found:", await mcp.ErrorAsync("create_account", new { customerId = Guid.NewGuid() }));
        Assert.Contains("not_found:", await mcp.ErrorAsync("delete_customer", new { customerId = Guid.NewGuid() }));
    }
}
