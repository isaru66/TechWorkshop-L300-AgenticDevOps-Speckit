using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Banking.Api.Tests.Integration;

namespace Banking.Api.Tests.Mcp;

public sealed class McpTestClient : IAsyncDisposable
{
    private readonly BankingApiFactory _factory;
    private readonly HttpClient _client;
    private int _id;

    public McpTestClient(PostgresFixture fixture)
    {
        _factory = new BankingApiFactory(fixture);
        _client = _factory.CreateClient();
    }

    public HttpClient Http => _client;

    public async Task<JsonElement> RpcAsync(string method, object? parameters = null)
    {
        var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = ++_id, method, @params = parameters });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        var json = text.TrimStart().StartsWith('{') ? text
            : string.Join("", text.Split('\n').Where(x => x.StartsWith("data:")).Select(x => x[5..].Trim()).Take(1));
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public async Task<(bool IsError, string Text)> CallRawAsync(string tool, object arguments)
    {
        var response = await RpcAsync("tools/call", new { name = tool, arguments });
        var result = response.GetProperty("result");
        var isError = result.TryGetProperty("isError", out var flag) && flag.GetBoolean();
        return (isError, result.GetProperty("content")[0].GetProperty("text").GetString()!);
    }

    public async Task<JsonElement> CallAsync(string tool, object arguments)
    {
        var (isError, text) = await CallRawAsync(tool, arguments);
        Assert.False(isError, text);
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    public async Task<string> ErrorAsync(string tool, object arguments)
    {
        var (isError, text) = await CallRawAsync(tool, arguments);
        Assert.True(isError, text);
        return text;
    }

    public async Task<string> NewCustomerAsync(string name = "Demo Customer") =>
        (await CallAsync("create_customer", new { name })).GetProperty("id").GetString()!;

    public async Task<string> NewAccountAsync(string customerId) =>
        (await CallAsync("create_account", new { customerId })).GetProperty("id").GetString()!;

    public async Task<string> BalanceAsync(string accountId) =>
        (await CallAsync("get_account", new { accountId })).GetProperty("balance").GetString()!;

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
