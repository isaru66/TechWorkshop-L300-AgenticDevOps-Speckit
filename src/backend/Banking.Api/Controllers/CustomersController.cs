using Banking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(BankingService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await service.CustomersAsync(page, pageSize, cancellationToken));

    [HttpGet("{customerId}")]
    public async Task<IActionResult> Get(string customerId, CancellationToken cancellationToken) =>
        Ok(await service.CustomerAsync(RouteId.Parse(customerId), cancellationToken));

    [HttpGet("{customerId}/accounts")]
    public async Task<IActionResult> Accounts(string customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await service.CustomerAccountsAsync(RouteId.Parse(customerId), page, pageSize, cancellationToken));
}
