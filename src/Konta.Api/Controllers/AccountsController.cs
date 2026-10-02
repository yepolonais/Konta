using System.ComponentModel.DataAnnotations;
using Konta.Application.Accounts;
using Konta.Domain.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(IAccountService accountService) : ControllerBase
{
    /// <summary>Returns the accounts configured for Société Générale imports.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountListItem>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await accountService.ListAsync(cancellationToken));
    }

    /// <summary>Returns a configured account.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AccountListItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await accountService.GetAsync(id, cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    /// <summary>Associates a Société Générale account number with a Konta account.</summary>
    [HttpPost]
    [ProducesResponseType<AccountListItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountListItem>> Create(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Kind))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Type de compte invalide",
                Detail = "Sélectionnez un type de compte reconnu.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var account = await accountService.CreateAsync(
                request.Name,
                request.BankAccountNumber,
                request.BankLabel,
                request.Kind,
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (DuplicateAccountException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Compte déjà associé",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}

/// <summary>Information used to associate an imported bank account with Konta.</summary>
public sealed record CreateAccountRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string BankAccountNumber { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string BankLabel { get; init; }

    [Required]
    public required AccountKind Kind { get; init; }
}