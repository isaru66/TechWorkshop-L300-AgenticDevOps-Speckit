using Banking.Api.Services;
using Banking.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class TransactionsController(BankingService service) : ControllerBase
{
    [HttpGet("{entryId}")]
    [ProducesResponseType<TransactionEntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(string entryId, CancellationToken cancellationToken) =>
        Ok(await service.EntryAsync(RouteId.Parse(entryId), cancellationToken));
}
