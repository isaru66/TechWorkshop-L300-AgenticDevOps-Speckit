using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace Banking.Api.Tests.Integration;

public sealed class ApiHostTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task DevelopmentExposesOpenApiAndSwaggerAndDatabaseReadiness()
    {
        await using var factory = new BankingApiFactory(fixture);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [Fact]
    public async Task OpenApiDescribesReadResponsesErrorsAndMoneyExamples()
    {
        await using var factory = new BankingApiFactory(fixture);
        using var client = factory.CreateClient();
        using var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");
        var root = document!.RootElement;
        var paths = root.GetProperty("paths");
        foreach (var path in new[] { "/api/customers", "/api/customers/{customerId}", "/api/customers/{customerId}/accounts",
            "/api/accounts", "/api/accounts/{accountId}", "/api/accounts/{accountId}/balance", "/api/accounts/{accountId}/transactions",
            "/api/transactions/{entryId}", "/api/transfers", "/api/transfers/{transferId}" })
        {
            var responses = paths.GetProperty(path).GetProperty("get").GetProperty("responses");
            Assert.True(responses.GetProperty("200").GetProperty("content").GetProperty("application/json").TryGetProperty("schema", out _), path);
            Assert.True(responses.TryGetProperty("400", out _), path);
            Assert.True(responses.TryGetProperty("503", out _), path);
            Assert.True(responses.GetProperty("503").GetProperty("content").TryGetProperty("application/problem+json", out _), path);
        }
        foreach (var path in new[] { "/api/accounts/{accountId}/deposits", "/api/accounts/{accountId}/withdrawals", "/api/transfers" })
        {
            var responses = paths.GetProperty(path).GetProperty("post").GetProperty("responses");
            Assert.True(responses.TryGetProperty("409", out _), path);
            Assert.True(responses.TryGetProperty("503", out _), path);
            Assert.True(responses.GetProperty("409").GetProperty("content").TryGetProperty("application/problem+json", out _), path);
        }
        var amount = root.GetProperty("components").GetProperty("schemas").GetProperty("MoneyOperationRequest")
            .GetProperty("properties").GetProperty("amount");
        Assert.True(amount.TryGetProperty("pattern", out _));
        Assert.True(amount.TryGetProperty("examples", out _) || amount.TryGetProperty("example", out _));
        Assert.Equal("string", amount.GetProperty("type").GetString());
        var requestPattern = amount.GetProperty("pattern").GetString()!;
        Assert.Matches(requestPattern, "0.01");
        Assert.DoesNotMatch(requestPattern, "0.00");
        var responsePattern = root.GetProperty("components").GetProperty("schemas").GetProperty("TransactionEntryDto")
            .GetProperty("properties").GetProperty("amount").GetProperty("pattern").GetString()!;
        Assert.Matches(responsePattern, "1.00");
        Assert.DoesNotMatch(responsePattern, "1");
    }

    [Fact]
    public async Task ProductionDoesNotExposeDevelopmentDocumentation()
    {
        await using var factory = new BankingApiFactory(fixture, "Production");
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [Fact]
    public async Task ReadinessFailsWithoutLeakingDatabaseDetailsWhenDatabaseIsUnavailable()
    {
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Port = 1, Timeout = 1 };
        await using var factory = new BankingApiFactory(fixture, connectionString: connection.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("Password", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
