using Banking.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Banking.Api.Tests.Integration;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _schema = $"test_{Guid.NewGuid():N}";
    private readonly string _adminConnection;
    private bool _created;

    public string ConnectionString { get; }

    public PostgresFixture()
    {
        var source = Environment.GetEnvironmentVariable("BANKING_TEST_CONNECTION")
            ?? throw new InvalidOperationException("BANKING_TEST_CONNECTION is required for PostgreSQL integration tests.");
        var builder = new NpgsqlConnectionStringBuilder(source);
        if (builder.Database != "banking_test" || builder.Port != 5433 ||
            builder.Host is not ("127.0.0.1" or "localhost"))
            throw new InvalidOperationException("Integration tests require the local banking_test database on port 5433.");

        builder.SearchPath = null;
        _adminConnection = builder.ConnectionString;
        builder.SearchPath = _schema;
        ConnectionString = builder.ConnectionString;
    }

    public BankingDbContext CreateContext() => new(new DbContextOptionsBuilder<BankingDbContext>()
        .UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", connection))
            await command.ExecuteNonQueryAsync();
        _created = true;
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_created)
            return;

        await using var connection = new NpgsqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA \"{_schema}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }
}
