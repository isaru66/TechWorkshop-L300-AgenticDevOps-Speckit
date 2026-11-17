using Banking.Api.Services;
using Banking.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(BankingService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await service.AccountsAsync(page, pageSize, cancellationToken));

    [HttpGet("{accountId}")]
    public async Task<IActionResult> Get(string accountId, CancellationToken cancellationToken) =>
        Ok(await service.AccountAsync(RouteId.Parse(accountId), cancellationToken));

    [HttpGet("{accountId}/balance")]
    public async Task<IActionResult> Balance(string accountId, CancellationToken cancellationToken) =>
        Ok(await service.BalanceAsync(RouteId.Parse(accountId), cancellationToken));

    [HttpGet("{accountId}/transactions")]
    public async Task<IActionResult> History(string accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await service.HistoryAsync(RouteId.Parse(accountId), page, pageSize, cancellationToken));

    [HttpPost("{accountId}/deposits")]
    [ProducesResponseType<MoneyOperationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Deposit(string accountId, MoneyOperationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeBalanceAsync(RouteId.Parse(accountId), request.Amount, request.Currency, true, cancellationToken);
        return Created($"/api/transactions/{result.Transaction.Id}", result);
    }

    [HttpPost("{accountId}/withdrawals")]
    [ProducesResponseType<MoneyOperationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Withdraw(string accountId, MoneyOperationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeBalanceAsync(RouteId.Parse(accountId), request.Amount, request.Currency, false, cancellationToken);
        return Created($"/api/transactions/{result.Transaction.Id}", result);
    }
}
