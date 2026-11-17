using Banking.Api.Services;
using Banking.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public sealed class TransfersController(BankingService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await service.TransfersAsync(page, pageSize, cancellationToken));

    [HttpGet("{transferId}")]
    public async Task<IActionResult> Get(string transferId, CancellationToken cancellationToken) =>
        Ok(await service.TransferAsync(RouteId.Parse(transferId), cancellationToken));

    [HttpPost]
    [ProducesResponseType<TransferOperationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(TransferRequest request, CancellationToken cancellationToken)
    {
        if (!request.SourceAccountId.HasValue || !request.DestinationAccountId.HasValue)
            throw BankingProblem.InvalidInput("Source and destination account identifiers are required.");
        var result = await service.TransferAsync(request.SourceAccountId.Value, request.DestinationAccountId.Value,
            request.Amount, request.Currency, cancellationToken);
        return Created($"/api/transfers/{result.Transfer.Id}", result);
    }
}
