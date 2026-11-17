using Banking.Api.Services;
using Banking.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/customers")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class CustomersController(BankingService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PageResult<CustomerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await service.CustomersAsync(page, pageSize, cancellationToken));

    [HttpGet("{customerId}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(string customerId, CancellationToken cancellationToken) =>
        Ok(await service.CustomerAsync(RouteId.Parse(customerId), cancellationToken));

    [HttpGet("{customerId}/accounts")]
    [ProducesResponseType<PageResult<AccountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Accounts(string customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await service.CustomerAccountsAsync(RouteId.Parse(customerId), page, pageSize, cancellationToken));
}
