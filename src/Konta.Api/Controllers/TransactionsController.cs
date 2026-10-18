using Konta.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(ITransactionQueryService transactionQueryService) : ControllerBase
{
    /// <summary>Returns the saved transactions ordered by operation date.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionListItem>>> Get(CancellationToken cancellationToken)
    {
        var transactions = await transactionQueryService.ListAsync(cancellationToken);
        return Ok(transactions);
    }

    /// <summary>Returns income, expenses, and their transactions for a month or year.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<TransactionPeriodSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionPeriodSummary>> GetSummary(
        [FromQuery] int year,
        [FromQuery] int? month,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await transactionQueryService.GetPeriodSummaryAsync(
                year,
                month,
                accountId,
                categoryId,
                cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Période invalide",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}