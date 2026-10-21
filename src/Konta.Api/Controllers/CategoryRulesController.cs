using System.ComponentModel.DataAnnotations;
using Konta.Application.CategoryRules;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/category-rules")]
public sealed class CategoryRulesController(ICategoryRuleService categoryRuleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryRuleListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryRuleListItem>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await categoryRuleService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoryRuleListItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryRuleListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await categoryRuleService.GetAsync(id, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    [ProducesResponseType<CategoryRuleListItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryRuleListItem>> Create(
        CreateCategoryRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await categoryRuleService.CreateAsync(
                request.Name,
                request.Pattern,
                request.CategoryId,
                request.SubcategoryName,
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule);
        }
        catch (DuplicateCategoryRuleException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Règle de catégorie déjà existante",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Règle de catégorie invalide",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CategoryRuleListItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryRuleListItem>> Update(
        Guid id,
        UpdateCategoryRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await categoryRuleService.UpdateAsync(
                id,
                request.Name,
                request.Pattern,
                request.CategoryId,
                request.SubcategoryName,
                cancellationToken);
            return rule is null ? NotFound() : Ok(rule);
        }
        catch (DuplicateCategoryRuleException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Règle de catégorie déjà existante",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Règle de catégorie invalide",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await categoryRuleService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

public sealed record CreateCategoryRuleRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string Pattern { get; init; }

    [Required]
    public required Guid CategoryId { get; init; }

    [StringLength(200)]
    public string? SubcategoryName { get; init; }
}

public sealed record UpdateCategoryRuleRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string Pattern { get; init; }

    [Required]
    public required Guid CategoryId { get; init; }

    [StringLength(200)]
    public string? SubcategoryName { get; init; }
}
