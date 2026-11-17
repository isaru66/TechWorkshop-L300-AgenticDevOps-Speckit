using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Banking.Api.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Tests.Import;

public sealed class ImportCommandTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task DispatchIsDevelopmentOnlyAbsoluteAndDoesNotBindHttp()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var production = await Run("Production", BaselineImportTests.SeedPath(), port);
            Assert.NotEqual(0, production.ExitCode);
            var relative = await Run("Development", "relative", port);
            Assert.NotEqual(0, relative.ExitCode);
            var first = await Run("Development", BaselineImportTests.SeedPath(), port);
            Assert.Equal(0, first.ExitCode);
            Assert.Contains("customers: inserted=102, skipped=0", first.Output);
            var second = await Run("Development", BaselineImportTests.SeedPath(), port);
            Assert.Equal(0, second.ExitCode);
            Assert.Contains("transaction_entries: inserted=0, skipped=1002", second.Output);
            await using var db = fixture.CreateContext();
            var marker = await db.SeedRecords.FirstAsync();
            marker.SourceFingerprint = new string('0', 64);
            await db.SaveChangesAsync();
            var conflict = await Run("Development", BaselineImportTests.SeedPath(), port);
            Assert.NotEqual(0, conflict.ExitCode);
            Assert.Contains("conflicting seed UUID", conflict.Output);
            Assert.DoesNotContain("Password=", conflict.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task<(int ExitCode, string Output)> Run(string environment, string path, int port)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--import-seed");
        start.ArgumentList.Add("--seed-path");
        start.ArgumentList.Add(path);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        start.Environment["ConnectionStrings__Banking"] = fixture.ConnectionString;
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Import process did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            process.Kill(true);
            throw;
        }
        return (process.ExitCode, (await stdout) + (await stderr));
    }
}
