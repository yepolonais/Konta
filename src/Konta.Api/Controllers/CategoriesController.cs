using System.ComponentModel.DataAnnotations;
using Konta.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryListItem>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await categoryService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoryListItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [ProducesResponseType<CategoryListItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryListItem>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryService.CreateAsync(request.Name, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }
        catch (DuplicateCategoryException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Catégorie déjà existante",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Nom de catégorie invalide",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CategoryListItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryListItem>> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryService.UpdateAsync(id, request.Name, cancellationToken);
            return category is null ? NotFound() : Ok(category);
        }
        catch (DuplicateCategoryException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Catégorie déjà existante",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Nom de catégorie invalide",
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
        var deleted = await categoryService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

public sealed record CreateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }
}

public sealed record UpdateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }
}
