using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Banking.Api.Tests.Integration;

public sealed class AmbiguousCommitTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("deposits")]
    [InlineData("withdrawals")]
    [InlineData("transfers")]
    public async Task ReportsUnknownWhenCommitSucceedsButAcknowledgementIsLost(string operation)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Fictional", CreatedAt = DateTimeOffset.UtcNow };
        var source = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = 20m, CreatedAt = DateTimeOffset.UtcNow };
        var destination = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = 5m, CreatedAt = DateTimeOffset.UtcNow };
        await using (var db = fixture.CreateContext())
        {
            db.Customers.Add(customer);
            db.Accounts.AddRange(source, destination);
            await db.SaveChangesAsync();
        }

        await using var host = new BankingApiFactory(fixture, interceptor: new LostCommitAcknowledgement());
        using var client = host.CreateClient();
        var response = operation == "transfers"
            ? await client.PostAsJsonAsync("/api/transfers", new { sourceAccountId = source.Id, destinationAccountId = destination.Id, amount = "1.00", currency = "THB" })
            : await client.PostAsJsonAsync($"/api/accounts/{source.Id}/{operation}", new { amount = "1.00", currency = "THB" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("completion_unknown", problem!.RootElement.GetProperty("code").GetString());
        Assert.Contains("unknown", problem.RootElement.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
        await using var verify = fixture.CreateContext();
        Assert.Equal(operation == "deposits" ? 21m : 19m, (await verify.Accounts.SingleAsync(x => x.Id == source.Id)).Balance);
        Assert.Equal(operation == "transfers" ? 2 : 1, await verify.TransactionEntries.CountAsync(x => x.AccountId == source.Id || x.AccountId == destination.Id));
    }

    private sealed class LostCommitAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default) =>
            throw new TimeoutException("Simulated lost commit acknowledgement.");
    }
}
