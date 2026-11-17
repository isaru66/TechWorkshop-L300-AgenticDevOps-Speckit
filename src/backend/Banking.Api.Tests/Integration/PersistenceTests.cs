using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.Api.Data;
using Banking.Api.Import;
using Banking.Api.Tests.Import;

namespace Banking.Api.Tests.Integration;

public sealed class PersistenceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task RestartRetainsCommittedActivityAndDoesNotAutomaticallyImport()
    {
        const string source = "00209a31-c483-41bb-a9fd-35530f8f680b";
        const string destination = "fc99d3e9-f19d-439e-87e4-c7a54b585c7d";
        await using (var emptyHost = new BankingApiFactory(fixture))
        using (var client = emptyHost.CreateClient())
        using (var customers = await client.GetFromJsonAsync<JsonDocument>("/api/customers"))
            Assert.Equal(0, customers!.RootElement.GetProperty("totalCount").GetInt32());

        await using (var db = fixture.CreateContext())
            await new SeedImporter(db, new BankingTransactionCoordinator(db, new BankingCommitObserver())).ImportAsync(BaselineImportTests.SeedPath());
        string sourceBefore;
        string historyBefore;
        string destinationBefore;
        string destinationHistoryBefore;
        await using (var host = new BankingApiFactory(fixture))
        using (var client = host.CreateClient())
        {
            var response = await client.PostAsJsonAsync("/api/transfers", new { sourceAccountId = source, destinationAccountId = destination, amount = "1.00", currency = "THB" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            sourceBefore = await client.GetStringAsync($"/api/accounts/{source}/balance");
            destinationBefore = await client.GetStringAsync($"/api/accounts/{destination}/balance");
            historyBefore = await client.GetStringAsync($"/api/accounts/{source}/transactions?pageSize=100");
            destinationHistoryBefore = await client.GetStringAsync($"/api/accounts/{destination}/transactions?pageSize=100");
        }
        await using (var host = new BankingApiFactory(fixture))
        using (var client = host.CreateClient())
        {
            Assert.Equal(sourceBefore, await client.GetStringAsync($"/api/accounts/{source}/balance"));
            Assert.Equal(destinationBefore, await client.GetStringAsync($"/api/accounts/{destination}/balance"));
            Assert.Equal(historyBefore, await client.GetStringAsync($"/api/accounts/{source}/transactions?pageSize=100"));
            Assert.Equal(destinationHistoryBefore, await client.GetStringAsync($"/api/accounts/{destination}/transactions?pageSize=100"));
        }
        await using var verify = fixture.CreateContext();
        var beforeImport = await RepeatImportTests.Snapshot(verify);
        var repeat = await new SeedImporter(verify, new BankingTransactionCoordinator(verify, new BankingCommitObserver())).ImportAsync(BaselineImportTests.SeedPath());
        Assert.All(repeat.Counts.Values, count => Assert.Equal(0, count.Inserted));
        Assert.Equal(beforeImport, await RepeatImportTests.Snapshot(verify));
    }
}
