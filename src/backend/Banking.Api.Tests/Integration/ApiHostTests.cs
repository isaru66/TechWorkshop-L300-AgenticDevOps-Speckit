using System.Net;
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
