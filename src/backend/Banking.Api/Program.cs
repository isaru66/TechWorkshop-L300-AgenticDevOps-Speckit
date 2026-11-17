using Banking.Api.Contracts;
using Banking.Api.Data;
using Banking.Api.Import;
using Banking.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var isSeedImport = args.Contains("--import-seed", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(isSeedImport ? [] : args);

builder.Services.AddDbContext<BankingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Banking")));
builder.Services.AddScoped<BankingTransactionCoordinator>();
builder.Services.AddScoped<SeedImporter>();
builder.Services.AddScoped<BankingService>();
builder.Services.AddSingleton<IBankingCommitObserver, BankingCommitObserver>();
builder.Services.AddExceptionHandler<BankingExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Type = "about:blank",
            Title = "Invalid input",
            Status = StatusCodes.Status400BadRequest,
            Detail = "The request contains invalid values."
        };
        details.Extensions["code"] = context.ModelState.Keys.Any(key =>
            key.Equals("$.amount", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("amount", StringComparison.OrdinalIgnoreCase))
            ? "invalid_amount" : "invalid_input";
        details.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        var result = new BadRequestObjectResult(details);
        result.ContentTypes.Add("application/problem+json");
        return result;
    };
});
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, _) =>
    {
        var propertyName = context.JsonPropertyInfo?.Name;
        if (propertyName is "amount" or "balance" or "resultingBalance")
        {
            schema.Pattern = propertyName == "amount"
                ? @"^[0-9]{1,16}(\.[0-9]{1,2})?$"
                : @"^[0-9]{1,16}\.[0-9]{2}$";
            schema.Description = "THB amount encoded as a decimal string, never a JSON number.";
        }
        return Task.CompletedTask;
    });
});
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://127.0.0.1:8080");

var app = builder.Build();
if (isSeedImport)
{
    var pathIndex = Array.IndexOf(args, "--seed-path");
    if (!app.Environment.IsDevelopment() || pathIndex < 0 || pathIndex + 1 >= args.Length ||
        !Path.IsPathFullyQualified(args[pathIndex + 1]))
    {
        Console.Error.WriteLine("Import requires Development and an absolute --seed-path.");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<SeedImporter>().ImportAsync(args[pathIndex + 1]);
        foreach (var (type, counts) in result.Counts)
            Console.WriteLine($"{type}: inserted={counts.Inserted}, skipped={counts.Skipped}");
    }
    catch (SeedInputException exception)
    {
        Console.Error.WriteLine(exception.Message);
        Environment.ExitCode = 1;
    }
    catch (Exception)
    {
        Console.Error.WriteLine("Import failed. Check seed format and local database availability.");
        Environment.ExitCode = 1;
    }
    return;
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Banking API"));
}

app.MapControllers();
app.MapGet("/health", async (BankingDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ready" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

public partial class Program;
