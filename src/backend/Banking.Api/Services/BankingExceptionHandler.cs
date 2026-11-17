using Banking.Api.Contracts;
using Banking.Api.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Banking.Api.Services;

public sealed class BankingExceptionHandler(ILogger<BankingExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception as BankingProblem;
        var unknown = exception is BankingCompletionUnknownException;
        var unavailable = IsDatabaseFailure(exception);
        var status = problem?.Status ?? (unavailable || unknown ? 503 : 500);
        if (problem is null)
            logger.LogError(exception, "Banking request failed; trace {TraceId}", context.TraceIdentifier);

        var details = new ProblemDetails
        {
            Type = "about:blank",
            Title = problem?.Title ?? (unknown ? "Completion unknown" : unavailable ? "Operation unavailable" : "Unexpected error"),
            Status = status,
            Detail = problem?.Message ?? (unknown ? "Completion is unknown. Refresh balances and history before submitting again."
                : unavailable ? "The operation could not be completed." : "An unexpected error occurred.")
        };
        details.Extensions["code"] = problem?.Code ?? (unknown ? "completion_unknown" : unavailable ? "operation_unavailable" : "unexpected_error");
        details.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(details, cancellationToken);
        return true;
    }

    private static bool IsDatabaseFailure(Exception exception) =>
        exception is NpgsqlException or TimeoutException ||
        (exception.InnerException is not null && IsDatabaseFailure(exception.InnerException));
}
