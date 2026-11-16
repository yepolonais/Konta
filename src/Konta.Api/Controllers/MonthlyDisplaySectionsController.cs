using System.ComponentModel.DataAnnotations;
using Konta.Application.Budgets;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/monthly-display-sections")]
public sealed class MonthlyDisplaySectionsController(IMonthlyDisplaySectionService service) : ControllerBase
{
    /// <summary>Lists the custom sections of the monthly budget review.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MonthlyDisplaySectionItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MonthlyDisplaySectionItem>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    /// <summary>Creates a custom section and assigns existing budget posts to it.</summary>
    [HttpPost]
    [ProducesResponseType<MonthlyDisplaySectionItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MonthlyDisplaySectionItem>> Create(
        SaveMonthlyDisplaySectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var section = await service.CreateAsync(request.Name, request.PostIds, cancellationToken);
            return Created($"/api/monthly-display-sections/{section.Id}", section);
        }
        catch (ArgumentException exception) { return Invalid(exception, StatusCodes.Status400BadRequest); }
        catch (InvalidOperationException exception) { return Invalid(exception, StatusCodes.Status409Conflict); }
    }

    /// <summary>Returns a custom section.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MonthlyDisplaySectionItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MonthlyDisplaySectionItem>> Get(Guid id, CancellationToken cancellationToken)
    {
        var section = (await service.ListAsync(cancellationToken)).FirstOrDefault(item => item.Id == id);
        return section is null ? NotFound() : Ok(section);
    }

    /// <summary>Renames a custom section and replaces its post selection.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<MonthlyDisplaySectionItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MonthlyDisplaySectionItem>> Update(
        Guid id, SaveMonthlyDisplaySectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var section = await service.UpdateAsync(id, request.Name, request.PostIds, cancellationToken);
            return section is null ? NotFound() : Ok(section);
        }
        catch (ArgumentException exception) { return Invalid(exception, StatusCodes.Status400BadRequest); }
        catch (InvalidOperationException exception) { return Invalid(exception, StatusCodes.Status409Conflict); }
    }

    /// <summary>Deletes a section without deleting its budget posts.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    private ObjectResult Invalid(Exception exception, int status) => StatusCode(status, new ProblemDetails
    {
        Title = "Section invalide",
        Detail = exception.Message,
        Status = status
    });
}

/// <summary>Name and selected budget posts for a custom monthly review section.</summary>
public sealed record SaveMonthlyDisplaySectionRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required]
    public required IReadOnlyList<Guid> PostIds { get; init; }
}
