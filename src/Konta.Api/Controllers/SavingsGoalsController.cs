using System.ComponentModel.DataAnnotations;
using Konta.Application.Savings;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/savings-goals")]
public sealed class SavingsGoalsController(ISavingsGoalService savingsGoalService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SavingsGoalItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SavingsGoalItem>>> Get(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await savingsGoalService.ListAsync(year, month, cancellationToken));
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

    [HttpPost]
    [ProducesResponseType<SavingsGoalItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SavingsGoalItem>> Create(
        SaveSavingsGoalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await savingsGoalService.CreateAsync(
                request.Name,
                request.AccountId,
                request.TargetAmount,
                request.PlannedMonthlyContribution,
                request.Year,
                request.Month,
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { year = request.Year, month = request.Month }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(InvalidRequest(exception.Message));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<SavingsGoalItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SavingsGoalItem>> Update(
        Guid id,
        SaveSavingsGoalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await savingsGoalService.UpdateAsync(
                id,
                request.Name,
                request.AccountId,
                request.TargetAmount,
                request.PlannedMonthlyContribution,
                request.Year,
                request.Month,
                cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(InvalidRequest(exception.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await savingsGoalService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    private static ProblemDetails InvalidRequest(string detail) => new()
    {
        Title = "Objectif d'épargne invalide",
        Detail = detail,
        Status = StatusCodes.Status400BadRequest
    };
}

public sealed record SaveSavingsGoalRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required]
    public required Guid AccountId { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal TargetAmount { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal PlannedMonthlyContribution { get; init; }

    [Range(1, 9998)]
    public int Year { get; init; }

    [Range(1, 12)]
    public int Month { get; init; }
}
