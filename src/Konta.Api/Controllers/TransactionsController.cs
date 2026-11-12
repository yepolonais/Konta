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

    /// <summary>Changes the category of a saved transaction.</summary>
    [HttpPut("{id:guid}/category")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeCategory(
        Guid id,
        ChangeTransactionCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var changed = await transactionQueryService.ChangeCategoryAsync(id, request.CategoryId, cancellationToken);
            return changed ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Catégorie invalide",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>Sets the budget post and personal description of a multi-post category transaction.</summary>
    [HttpPut("{id:guid}/budget-assignment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetBudgetAssignment(
        Guid id,
        SetTransactionBudgetAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var changed = await transactionQueryService.SetBudgetAssignmentAsync(
                id, request.BudgetPostId, request.Description, cancellationToken);
            return changed ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Rattachement invalide",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Rattachement impossible",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
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

public sealed record ChangeTransactionCategoryRequest
{
    public Guid CategoryId { get; init; }
}

/// <summary>Budget post and optional personal description for an operation.</summary>
public sealed record SetTransactionBudgetAssignmentRequest
{
    public Guid? BudgetPostId { get; init; }
    public string? Description { get; init; }
}
