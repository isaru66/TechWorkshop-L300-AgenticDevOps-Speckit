using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Banking.Api.Data;

public sealed class BankingDesignTimeFactory : IDesignTimeDbContextFactory<BankingDbContext>
{
    public BankingDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Banking");
        if (string.IsNullOrWhiteSpace(connection))
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables()
                .Build();
            connection = configuration.GetConnectionString("Banking")
                ?? "Host=127.0.0.1;Database=banking;Username=banking";
        }

        return new BankingDbContext(new DbContextOptionsBuilder<BankingDbContext>()
            .UseNpgsql(connection).Options);
    }
}
