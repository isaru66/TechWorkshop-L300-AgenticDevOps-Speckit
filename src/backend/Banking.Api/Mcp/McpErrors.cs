using Banking.Api.Contracts;
using ModelContextProtocol;

namespace Banking.Api.Mcp;

internal static class McpErrors
{
    public static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (BankingProblem problem)
        {
            throw new McpException($"{problem.Code}: {problem.Message}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not McpException)
        {
            throw new McpException("unexpected_error: An unexpected error occurred.");
        }
    }

    public static async Task<object> RunDelete(Func<Task> action)
    {
        await Run(async () => { await action(); return 0; });
        return new { deleted = true };
    }
}
