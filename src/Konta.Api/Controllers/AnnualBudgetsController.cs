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

    [HttpPost("{year:int}/posts")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> CreatePost(
        int year,
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
                year,
                request.Name,
                request.Type.Value,
                request.ExpenseKind,
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { year }, post);
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

    [HttpPut("{year:int}/posts/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderPosts(
        int year,
        ReorderAnnualBudgetPostsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await annualBudgetService.ReorderPostsAsync(
                year,
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

    [HttpPut("posts/{id:guid}/months/{month:int}")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> SetMonthlyAmount(
        Guid id,
        int month,
        SetMonthlyAmountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await annualBudgetService.SetMonthlyAmountAsync(id, month, request.Amount!.Value, cancellationToken);
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

    [HttpPost("posts/{id:guid}/months/{month:int}/copy-forward")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnnualBudgetPostItem>> CopyMonthlyAmountForward(
        Guid id,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await annualBudgetService.CopyMonthlyAmountForwardAsync(id, month, cancellationToken);
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

    [HttpPost("{year:int}/extras")]
    [ProducesResponseType<BudgetExtraItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BudgetExtraItem>> AddExtra(
        int year,
        AddBudgetExtraRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var extra = await annualBudgetService.AddExtraAsync(
                year,
                request.Month!.Value,
                request.Label,
                request.Amount!.Value,
                cancellationToken);
            return Created($"/api/annual-budgets/extras/{extra.Id}", extra);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(InvalidRequest(exception));
        }
    }

    [HttpDelete("extras/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExtra(Guid id, CancellationToken cancellationToken) =>
        await annualBudgetService.DeleteExtraAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("posts/{id:guid}/months/{month:int}")]
    [ProducesResponseType<AnnualBudgetPostItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnualBudgetPostItem>> ClearMonthlyAmount(
        Guid id,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await annualBudgetService.SetMonthlyAmountAsync(id, month, null, cancellationToken);
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

public sealed record AddBudgetExtraRequest
{
    [Required, Range(1, 12)]
    public int? Month { get; init; }

    [Required, StringLength(200)]
    public required string Label { get; init; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal? Amount { get; init; }
}

public sealed record SetBudgetPostCategoriesRequest
{
    [Required]
    public required IReadOnlyList<Guid> CategoryIds { get; init; }
}
