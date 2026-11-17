using Banking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(BankingService service) : ControllerBase
{
    [HttpGet("{entryId}")]
    public async Task<IActionResult> Get(string entryId, CancellationToken cancellationToken) =>
        Ok(await service.EntryAsync(RouteId.Parse(entryId), cancellationToken));
}
