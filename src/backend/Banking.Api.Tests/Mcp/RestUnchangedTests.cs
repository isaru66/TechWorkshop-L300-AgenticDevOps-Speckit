using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Mcp;

public sealed class RestUnchangedTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task RestRoutesAndHistoryOrderAreUnchanged()
    {
        await using var mcp = new McpTestClient(fixture);
        var http = mcp.Http;
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/transfers")).StatusCode);

        var account = await mcp.NewAccountAsync(await mcp.NewCustomerAsync());
        await mcp.CallAsync("deposit", new { accountId = account, amount = "5.00" });
        await mcp.CallAsync("withdraw", new { accountId = account, amount = "1.00" });
        using var doc = await http.GetFromJsonAsync<JsonDocument>($"/api/accounts/{account}/transactions");
        var types = doc!.RootElement.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("type").GetString()!).ToArray();
        Assert.Equal(["DEPOSIT", "WITHDRAWAL"], types);
    }
}
