using System.ComponentModel.DataAnnotations;
using Konta.Application.Budgets;
using Konta.Domain.Budgets;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/annual-budgets")]
public sealed class AnnualBudgetsController(IAnnualBudgetService annualBudgetService) : ControllerBase
{
    [HttpGet("{year:int}")]
    [ProducesResponseType<AnnualBudgetSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnualBudgetSummary>> Get(int year, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await annualBudgetService.GetAsync(year, cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpGet("{year:int}/months/{month:int}")]
    [ProducesResponseType<AnnualBudgetMonthSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnualBudgetMonthSummary>> GetMonth(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await annualBudgetService.GetMonthAsync(year, month, cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpPost("posts")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> CreatePost(
        SaveAnnualBudgetPostRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Type.HasValue || !Enum.IsDefined(request.Type.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Type de poste invalide",
                Detail = "Choisissez un type de revenu, dépense ou épargne.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            if (request.ExpenseKind.HasValue && !Enum.IsDefined(request.ExpenseKind.Value))
            {
                return BadRequest(InvalidExpenseKind());
            }

            var post = await annualBudgetService.CreatePostAsync(
                request.Name,
                request.Type.Value,
                request.ExpenseKind,
                cancellationToken);
            return CreatedAtAction(nameof(UpdatePost), new { id = post.Id }, post);
        }
        catch (DuplicateAnnualBudgetPostException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Poste déjà existant",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpPut("posts/{id:guid}")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> UpdatePost(
        Guid id,
        SaveAnnualBudgetPostRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Type.HasValue || !Enum.IsDefined(request.Type.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Type de poste invalide",
                Detail = "Choisissez un type de revenu, dépense ou épargne.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            if (request.ExpenseKind.HasValue && !Enum.IsDefined(request.ExpenseKind.Value))
            {
                return BadRequest(InvalidExpenseKind());
            }

            var post = await annualBudgetService.UpdatePostAsync(
                id,
                request.Name,
                request.Type.Value,
                request.ExpenseKind,
                cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (DuplicateAnnualBudgetPostException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Poste déjà existant",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpPut("posts/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderPosts(
        ReorderAnnualBudgetPostsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await annualBudgetService.ReorderPostsAsync(
                request.Type,
                request.ExpenseKind,
                request.PostIds,
                cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Réorganisation impossible",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpDelete("posts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken cancellationToken) =>
        await annualBudgetService.DeletePostAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPut("{year:int}/posts/{id:guid}/months/{month:int}")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> SetMonthlyAmount(
        int year,
        Guid id,
        int month,
        SetMonthlyAmountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await annualBudgetService.SetMonthlyAmountAsync(id, year, month, request.Amount!.Value, cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Poste désactivé",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPut("posts/{id:guid}/categories")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> SetCategoryMappings(
        Guid id,
        SetBudgetPostCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await annualBudgetService.SetCategoryMappingsAsync(id, request.CategoryIds, cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (DuplicateBudgetCategoryMappingException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Catégorie déjà rattachée",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Poste désactivé",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("{year:int}/posts/{id:guid}/months/{month:int}/copy-forward")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> CopyMonthlyAmountForward(
        int year,
        Guid id,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await annualBudgetService.CopyMonthlyAmountForwardAsync(id, year, month, cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Recopie impossible",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpDelete("{year:int}/posts/{id:guid}/months/{month:int}")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnualBudgetPostItem>> ClearMonthlyAmount(
        int year,
        Guid id,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await annualBudgetService.SetMonthlyAmountAsync(id, year, month, null, cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Poste désactivé",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static ProblemDetails InvalidExpenseKind() => new()
    {
        Title = "Catégorie de dépense invalide",
        Detail = "Choisissez une charge fixe ou une dépense variable.",
        Status = StatusCodes.Status400BadRequest
    };

    private static ProblemDetails InvalidRequest(ArgumentException exception) => new()
    {
        Title = "Données de budget invalides",
        Detail = exception.Message,
        Status = StatusCodes.Status400BadRequest
    };
}

public sealed record SaveAnnualBudgetPostRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required]
    public AnnualBudgetPostType? Type { get; init; }

    public AnnualBudgetExpenseKind? ExpenseKind { get; init; }
}

public sealed record ReorderAnnualBudgetPostsRequest
{
    [Required]
    public AnnualBudgetPostType Type { get; init; }

    public AnnualBudgetExpenseKind? ExpenseKind { get; init; }

    [Required, MinLength(1)]
    public required IReadOnlyList<Guid> PostIds { get; init; }
}

public sealed record SetMonthlyAmountRequest
{
    [Required, Range(0, double.MaxValue)]
    public decimal? Amount { get; init; }
}

public sealed record SetBudgetPostCategoriesRequest
{
    [Required]
    public required IReadOnlyList<Guid> CategoryIds { get; init; }
}
