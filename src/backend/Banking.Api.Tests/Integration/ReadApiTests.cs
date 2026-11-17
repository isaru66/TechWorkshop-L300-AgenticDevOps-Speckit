using System.Net;
using System.Net.Http.Json;
using Banking.Api.Data.Entities;
using System.Text.Json;

namespace Banking.Api.Tests.Integration;

public sealed class ReadApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task BrowsesCustomersAndAccountsWithoutMutationAndDistinguishesInvalidIdentifiers()
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Fictional customer", CreatedAt = DateTimeOffset.UtcNow };
        var emptyCustomer = new Customer { Id = Guid.NewGuid(), Name = "No accounts", CreatedAt = DateTimeOffset.UtcNow };
        var account = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = 101.25m, CreatedAt = DateTimeOffset.UtcNow };
        var emptyAccount = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = 10m, CreatedAt = DateTimeOffset.UtcNow };
        var noHistoryAccount = new Account { Id = Guid.NewGuid(), CustomerId = customer.Id, Currency = "THB", Balance = 0m, CreatedAt = DateTimeOffset.UtcNow };
        var completed = DateTimeOffset.UtcNow;
        var transfer = new Transfer { Id = Guid.NewGuid(), SourceAccountId = account.Id, DestinationAccountId = emptyAccount.Id, Currency = "THB", Amount = 2m, CompletedAt = completed };
        var entry = new TransactionEntry { Id = Guid.NewGuid(), Sequence = 1, AccountId = account.Id, Type = "TRANSFER_OUT", Amount = 2m, Currency = "THB", CompletedAt = completed, ResultingBalance = 101.25m, TransferId = transfer.Id, CounterpartyAccountId = emptyAccount.Id };
        var otherEntry = new TransactionEntry { Id = Guid.NewGuid(), Sequence = 2, AccountId = emptyAccount.Id, Type = "TRANSFER_IN", Amount = 2m, Currency = "THB", CompletedAt = completed, ResultingBalance = 10m, TransferId = transfer.Id, CounterpartyAccountId = account.Id };
        var deposit = new TransactionEntry { Id = Guid.NewGuid(), Sequence = 3, AccountId = account.Id, Type = "DEPOSIT", Amount = 1m, Currency = "THB", CompletedAt = completed, ResultingBalance = 102.25m };
        await using (var db = fixture.CreateContext())
        {
            db.Customers.Add(customer);
            db.Customers.Add(emptyCustomer);
            db.Accounts.Add(account);
            db.Accounts.Add(emptyAccount);
            db.Accounts.Add(noHistoryAccount);
            db.Transfers.Add(transfer);
            db.TransactionEntries.AddRange(entry, otherEntry, deposit);
            await db.SaveChangesAsync();
        }

        await using var factory = new BankingApiFactory(fixture);
        using var client = factory.CreateClient();
        using var page = await client.GetFromJsonAsync<System.Text.Json.JsonDocument>("/api/customers?page=1&pageSize=1");
        Assert.NotNull(page);
        Assert.Equal(1, page.RootElement.GetProperty("items").GetArrayLength());
        using var details = await client.GetFromJsonAsync<System.Text.Json.JsonDocument>($"/api/accounts/{account.Id}");
        Assert.NotNull(details);
        Assert.Equal("101.25", details.RootElement.GetProperty("balance").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/customers/{customer.Id}")).StatusCode);
        using var accounts = await client.GetFromJsonAsync<JsonDocument>($"/api/customers/{customer.Id}/accounts?page=1&pageSize=1");
        Assert.Equal(3, accounts!.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, accounts.RootElement.GetProperty("items").GetArrayLength());
        using var emptyAccounts = await client.GetFromJsonAsync<JsonDocument>($"/api/customers/{emptyCustomer.Id}/accounts");
        Assert.Equal(0, emptyAccounts!.RootElement.GetProperty("items").GetArrayLength());
        var accountIds = new List<Guid>();
        for (var index = 1; index <= 3; index++)
        {
            using var accountPage = await client.GetFromJsonAsync<JsonDocument>($"/api/accounts?page={index}&pageSize=1");
            accountIds.Add(accountPage!.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        Assert.Equal(new[] { account.Id, emptyAccount.Id, noHistoryAccount.Id }.OrderBy(x => x.ToString()), accountIds);
        using var balance = await client.GetFromJsonAsync<JsonDocument>($"/api/accounts/{account.Id}/balance");
        Assert.Equal("101.25", balance!.RootElement.GetProperty("balance").GetString());
        using var history = await client.GetFromJsonAsync<JsonDocument>($"/api/accounts/{account.Id}/transactions");
        var historicalEntry = history!.RootElement.GetProperty("items")[0];
        Assert.Equal("1", historicalEntry.GetProperty("sequence").GetString());
        Assert.Equal("2.00", historicalEntry.GetProperty("amount").GetString());
        Assert.Equal(transfer.Id.ToString(), historicalEntry.GetProperty("transferId").GetString());
        using var historySecondPage = await client.GetFromJsonAsync<JsonDocument>($"/api/accounts/{account.Id}/transactions?page=2&pageSize=1");
        Assert.Equal("3", historySecondPage!.RootElement.GetProperty("items")[0].GetProperty("sequence").GetString());
        using var emptyHistory = await client.GetFromJsonAsync<JsonDocument>($"/api/accounts/{noHistoryAccount.Id}/transactions");
        Assert.Equal(0, emptyHistory!.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/transactions/{entry.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/transfers/{transfer.Id}")).StatusCode);
        using var transfers = await client.GetFromJsonAsync<JsonDocument>("/api/transfers");
        Assert.Equal(1, transfers!.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/accounts?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/accounts?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/customers/{Guid.NewGuid()}/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/accounts/not-a-uuid")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/accounts/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/customers", new { name = "New" })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/accounts", new { customerId = customer.Id })).StatusCode);
    }
}
