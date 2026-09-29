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
}