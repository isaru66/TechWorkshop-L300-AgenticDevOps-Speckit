using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Integration;

public sealed class MoneyApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task ValidatesRequestsReturnsCreatedEntryAndPreservesStateOnRejection()
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Fictional", CreatedAt = DateTimeOffset.UtcNow };
        var account = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = 20m, CreatedAt = DateTimeOffset.UtcNow };
        await using (var db = fixture.CreateContext())
        {
            db.Customers.Add(customer);
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
        }
        await using var factory = new BankingApiFactory(fixture);
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync($"/api/accounts/{account.Id}/deposits", new { amount = "0.01", currency = "THB" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var body = await created.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("20.01", body!.RootElement.GetProperty("balance").GetString());
        var entryId = body.RootElement.GetProperty("transaction").GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/transactions/{entryId}", created.Headers.Location!.ToString());

        foreach (var json in new[] { """{"amount":1,"currency":"THB"}""", """{"amount":"1.001","currency":"THB"}""" })
        {
            var response = await client.PostAsync($"/api/accounts/{account.Id}/deposits", new StringContent(json, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
            Assert.Equal("invalid_amount", problem!.RootElement.GetProperty("code").GetString());
        }
        var balanceBefore = await client.GetStringAsync($"/api/accounts/{account.Id}/balance");
        var historyBefore = await client.GetStringAsync($"/api/accounts/{account.Id}/transactions");
        var rejected = await client.PostAsJsonAsync($"/api/accounts/{account.Id}/withdrawals", new { amount = "20.02", currency = "THB" });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        using var rejection = await rejected.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("insufficient_funds", rejection!.RootElement.GetProperty("code").GetString());
        Assert.Equal(balanceBefore, await client.GetStringAsync($"/api/accounts/{account.Id}/balance"));
        Assert.Equal(historyBefore, await client.GetStringAsync($"/api/accounts/{account.Id}/transactions"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/accounts/bad/deposits", new { amount = "1", currency = "THB" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/accounts/{Guid.NewGuid()}/deposits", new { amount = "1", currency = "THB" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/accounts/{account.Id}/deposits", new { amount = "1", currency = "USD" })).StatusCode);
        await using var verify = fixture.CreateContext();
        Assert.Equal(20.01m, (await verify.Accounts.SingleAsync(x => x.Id == account.Id)).Balance);
        Assert.Equal(1, await verify.TransactionEntries.CountAsync(x => x.AccountId == account.Id));
    }
}
