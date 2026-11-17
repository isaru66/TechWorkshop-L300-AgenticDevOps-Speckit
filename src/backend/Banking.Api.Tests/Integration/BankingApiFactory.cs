using Banking.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Api.Tests.Integration;

public sealed class BankingApiFactory(PostgresFixture fixture, string environment = "Development", string? connectionString = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<BankingDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddDbContext<BankingDbContext>(options => options.UseNpgsql(connectionString ?? fixture.ConnectionString));
        });
    }
}
