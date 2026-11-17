using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Banking.Api.Data;
using Banking.Api.Import;
using Banking.Api.Tests.Import;

namespace Banking.Api.Tests.Integration;

public sealed class TransferApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task SeededTransferAndInsufficientFundsHaveExactBothAccountSnapshots()
    {
        const string source = "00209a31-c483-41bb-a9fd-35530f8f680b";
        const string destination = "fc99d3e9-f19d-439e-87e4-c7a54b585c7d";
        await using (var db = fixture.CreateContext())
            await new SeedImporter(db, new BankingTransactionCoordinator(db, new BankingCommitObserver())).ImportAsync(BaselineImportTests.SeedPath());
        await using var factory = new BankingApiFactory(fixture);
        using var client = factory.CreateClient();
        var sourceHistory = await Items(client, $"/api/accounts/{source}/transactions");
        var destinationHistory = await Items(client, $"/api/accounts/{destination}/transactions");
        var transfers = await Items(client, "/api/transfers");
        Assert.Equal(4, sourceHistory.Count);
        Assert.Equal(6, destinationHistory.Count);
        var response = await client.PostAsJsonAsync("/api/transfers", new { sourceAccountId = source, destinationAccountId = destination, amount = "100.00", currency = "THB" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = result!.RootElement;
        Assert.Equal("52508.86", root.GetProperty("sourceBalance").GetProperty("balance").GetString());
        Assert.Equal("47143.92", root.GetProperty("destinationBalance").GetProperty("balance").GetString());
        var transferId = root.GetProperty("transfer").GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/transfers/{transferId}", response.Headers.Location!.ToString());
        var entries = root.GetProperty("entries");
        Assert.Equal(2, entries.GetArrayLength());
        Assert.All(entries.EnumerateArray(), entry => Assert.Equal(transferId, entry.GetProperty("transferId").GetGuid()));
        var sourceAfter = await Items(client, $"/api/accounts/{source}/transactions");
        var destinationAfter = await Items(client, $"/api/accounts/{destination}/transactions");
        Assert.Equal(sourceHistory, sourceAfter.Take(sourceHistory.Count));
        Assert.Equal(destinationHistory, destinationAfter.Take(destinationHistory.Count));
        Assert.Equal(sourceHistory.Count + 1, sourceAfter.Count);
        Assert.Equal(destinationHistory.Count + 1, destinationAfter.Count);
        var transfersAfter = await Items(client, "/api/transfers");
        Assert.Equal(transfers.Count + 1, transfersAfter.Count);
        var sourceBalance = await client.GetStringAsync($"/api/accounts/{source}/balance");
        var destinationBalance = await client.GetStringAsync($"/api/accounts/{destination}/balance");
        var rejected = await client.PostAsJsonAsync("/api/transfers", new { sourceAccountId = source, destinationAccountId = destination, amount = "52508.87", currency = "THB" });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        using var problem = await rejected.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("insufficient_funds", problem!.RootElement.GetProperty("code").GetString());
        Assert.Equal(sourceBalance, await client.GetStringAsync($"/api/accounts/{source}/balance"));
        Assert.Equal(destinationBalance, await client.GetStringAsync($"/api/accounts/{destination}/balance"));
        Assert.Equal(sourceAfter, await Items(client, $"/api/accounts/{source}/transactions"));
        Assert.Equal(destinationAfter, await Items(client, $"/api/accounts/{destination}/transactions"));
        Assert.Equal(transfersAfter, await Items(client, "/api/transfers"));
        foreach (var (body, status, code) in new[]
        {
            ($$"""{"sourceAccountId":"{{source}}","destinationAccountId":"{{source}}","amount":"1","currency":"THB"}""", HttpStatusCode.BadRequest, "same_account"),
            ($$"""{"sourceAccountId":"{{source}}","destinationAccountId":"{{destination}}","amount":1,"currency":"THB"}""", HttpStatusCode.BadRequest, "invalid_amount"),
            ($$"""{"sourceAccountId":"{{source}}","destinationAccountId":"{{destination}}","amount":"1.001","currency":"THB"}""", HttpStatusCode.BadRequest, "invalid_amount"),
            ($$"""{"sourceAccountId":"{{source}}","destinationAccountId":"{{destination}}","amount":"1","currency":"USD"}""", HttpStatusCode.BadRequest, "currency_mismatch"),
            ($$"""{"sourceAccountId":"{{source}}","destinationAccountId":"{{Guid.NewGuid()}}","amount":"1","currency":"THB"}""", HttpStatusCode.NotFound, "not_found"),
            ($$"""{"sourceAccountId":"invalid","destinationAccountId":"{{destination}}","amount":"1","currency":"THB"}""", HttpStatusCode.BadRequest, "invalid_input")
        })
        {
            var invalid = await client.PostAsync("/api/transfers", new StringContent(body, Encoding.UTF8, "application/json"));
            Assert.Equal(status, invalid.StatusCode);
            using var invalidProblem = await invalid.Content.ReadFromJsonAsync<JsonDocument>();
            Assert.Equal(code, invalidProblem!.RootElement.GetProperty("code").GetString());
        }
        Assert.Equal(sourceAfter, await Items(client, $"/api/accounts/{source}/transactions"));
        Assert.Equal(destinationAfter, await Items(client, $"/api/accounts/{destination}/transactions"));
    }

    private static async Task<List<string>> Items(HttpClient client, string path)
    {
        var items = new List<string>();
        for (var page = 1; ; page++)
        {
            using var document = await client.GetFromJsonAsync<JsonDocument>($"{path}?page={page}&pageSize=100");
            var root = document!.RootElement;
            items.AddRange(root.GetProperty("items").EnumerateArray().Select(x => x.GetRawText()));
            if (items.Count >= root.GetProperty("totalCount").GetInt32()) return items;
        }
    }
}
