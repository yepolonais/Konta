using System.ComponentModel.DataAnnotations;
using Konta.Application.Planning;
using Konta.Domain.Forecasts;
using Konta.Domain.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/planning")]
public sealed class PlanningController(IPlanningService planningService) : ControllerBase
{
    [HttpGet("budgets")]
    [ProducesResponseType<IReadOnlyList<MonthlyBudgetItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MonthlyBudgetItem>>> GetBudgets(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await planningService.ListBudgetsAsync(year, month, cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidPeriod(exception));
        }
    }

    [HttpPost("budgets")]
    [ProducesResponseType<MonthlyBudgetItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MonthlyBudgetItem>> CreateBudget(
        CreateMonthlyBudgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var budget = await planningService.CreateBudgetAsync(request.CategoryId, request.Amount, cancellationToken);
            return CreatedAtAction(nameof(GetBudgets), new { year = DateTime.UtcNow.Year, month = DateTime.UtcNow.Month }, budget);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Budget déjà défini",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPut("budgets/{id:guid}")]
    [ProducesResponseType<MonthlyBudgetItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonthlyBudgetItem>> UpdateBudget(
        Guid id,
        UpdateMonthlyBudgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await planningService.UpdateBudgetAsync(id, request.Amount, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpPut("budgets/{id:guid}/overrides/{year:int}/{month:int}")]
    [ProducesResponseType<MonthlyBudgetItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonthlyBudgetItem>> SetBudgetOverride(
        Guid id,
        int year,
        int month,
        UpdateMonthlyBudgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await planningService.SetBudgetOverrideAsync(id, year, month, request.Amount, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpDelete("budgets/{id:guid}/overrides/{year:int}/{month:int}")]
    [ProducesResponseType<MonthlyBudgetItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonthlyBudgetItem>> DeleteBudgetOverride(
        Guid id,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await planningService.DeleteBudgetOverrideAsync(id, year, month, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidPeriod(exception));
        }
    }

    [HttpGet("forecasts")]
    [ProducesResponseType<IReadOnlyList<RecurringForecastItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurringForecastItem>>> GetForecasts(CancellationToken cancellationToken) =>
        Ok(await planningService.ListForecastsAsync(cancellationToken));

    [HttpPost("forecasts")]
    [ProducesResponseType<RecurringForecastItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringForecastItem>> CreateForecast(
        CreateRecurringForecastRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var forecast = await planningService.CreateForecastAsync(
                request.Name,
                request.LabelPattern,
                request.AccountId,
                request.Amount,
                request.Type,
                request.DayOfMonth,
                request.Frequency,
                request.StartDate,
                request.EndDate,
                request.CategoryId,
                cancellationToken);
            return CreatedAtAction(nameof(GetForecasts), forecast);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpDelete("forecasts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteForecast(Guid id, CancellationToken cancellationToken) =>
        await planningService.DeleteForecastAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("occurrences")]
    [ProducesResponseType<IReadOnlyList<ForecastOccurrenceItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ForecastOccurrenceItem>>> GetOccurrences(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await planningService.ListForecastOccurrencesAsync(startDate, endDate, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    private static ProblemDetails InvalidPeriod(ArgumentOutOfRangeException exception) => new()
    {
        Title = "Période invalide",
        Detail = exception.Message,
        Status = StatusCodes.Status400BadRequest
    };

    private static ProblemDetails InvalidRequest(ArgumentException exception) => new()
    {
        Title = "Données de planification invalides",
        Detail = exception.Message,
        Status = StatusCodes.Status400BadRequest
    };
}

public sealed record CreateMonthlyBudgetRequest
{
    [Required]
    public required Guid CategoryId { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Amount { get; init; }
}

public sealed record UpdateMonthlyBudgetRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Amount { get; init; }
}

public sealed record CreateRecurringForecastRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string LabelPattern { get; init; }

    [Required]
    public required Guid AccountId { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Amount { get; init; }

    [Required]
    public TransactionType Type { get; init; }

    [Range(1, 31)]
    public int DayOfMonth { get; init; }

    [Required]
    public ForecastFrequency Frequency { get; init; }

    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? CategoryId { get; init; }
}
