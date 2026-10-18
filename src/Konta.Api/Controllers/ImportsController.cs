using System.Text.Json;
using System.Text.Json.Serialization;
using Konta.Application.Imports;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ImportsController(IImportPreviewService importPreviewService) : ControllerBase
{
    private const long MaximumUploadBytes = 10 * 1024 * 1024;

    /// <summary>Analyzes a Société Générale CSV without saving its transactions.</summary>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumUploadBytes)]
    [ProducesResponseType<ImportPreview>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportPreview>> Preview(
        IFormFile? file,
        [FromForm] string? accountMappings,
        CancellationToken cancellationToken)
    {
        var validation = ValidateFile(file);
        if (validation is not null)
        {
            return validation;
        }

        using var stream = file!.OpenReadStream();
        return Ok(await importPreviewService.PreviewAsync(stream, ParseAccountMappings(accountMappings), cancellationToken));
    }

    /// <summary>Imports the valid transactions from a reviewed Société Générale CSV.</summary>
    [HttpPost("commit")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumUploadBytes)]
    [ProducesResponseType<ImportCommitResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportCommitResult>> Commit(
        IFormFile? file,
        [FromForm] string? accountMappings,
        [FromForm] string? review,
        CancellationToken cancellationToken)
    {
        var validation = ValidateFile(file);
        if (validation is not null)
        {
            return validation;
        }

        if (!TryParseReview(review, out var parsedReview))
        {
            return Problem(
                title: "Validation d'import invalide",
                detail: "Le contenu des décisions de lignes est mal formé.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            using var stream = file!.OpenReadStream();
            return Ok(await importPreviewService.CommitAsync(
                stream,
                ParseAccountMappings(accountMappings),
                parsedReview,
                cancellationToken));
        }
        catch (UnmappedAccountsException exception)
        {
            var problem = new ProblemDetails
            {
                Title = "Comptes à associer",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            };
            problem.Extensions["accounts"] = exception.Accounts;
            return Conflict(problem);
        }
        catch (InvalidImportReviewException exception)
        {
            var problem = new ProblemDetails
            {
                Title = "Corrections d'import invalides",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            };
            problem.Extensions["errors"] = exception.Errors;
            return BadRequest(problem);
        }
        catch (InvalidTransferMatchException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Correspondance de transfert invalide",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static bool TryParseReview(
        string? review,
        out IReadOnlyList<ImportTransactionReview>? parsedReview)
    {
        parsedReview = null;
        if (string.IsNullOrWhiteSpace(review))
        {
            return true;
        }

        try
        {
            parsedReview = JsonSerializer.Deserialize<ImportTransactionReview[]>(
                review,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new JsonStringEnumConverter() }
                });
            return parsedReview is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyDictionary<string, Guid>? ParseAccountMappings(string? accountMappings)
    {
        if (string.IsNullOrWhiteSpace(accountMappings))
        {
            return null;
        }

        try
        {
            var byNumber = JsonSerializer.Deserialize<Dictionary<string, Guid>>(accountMappings);
            if (byNumber is not null)
            {
                return byNumber;
            }

            var mappings = JsonSerializer.Deserialize<ImportAccountMapping[]>(accountMappings);
            if (mappings is null)
            {
                return null;
            }

            return mappings
                .Where(mapping => !string.IsNullOrWhiteSpace(mapping.BankAccountNumber))
                .GroupBy(mapping => mapping.BankAccountNumber, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().AccountId, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private ActionResult? ValidateFile(IFormFile? file)
    {
        if (file is null)
        {
            return Problem(
                title: "Fichier manquant",
                detail: "Ajoutez un fichier CSV dans le champ multipart « file ».",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (file.Length == 0)
        {
            return Problem(
                title: "Fichier vide",
                detail: "Le fichier sélectionné est vide.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (file.Length > MaximumUploadBytes)
        {
            return Problem(
                title: "Fichier trop volumineux",
                detail: "La taille maximale autorisée est de 10 Mo.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                title: "Format non pris en charge",
                detail: "Sélectionnez un fichier CSV.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private sealed record ImportAccountMapping(string BankAccountNumber, Guid AccountId);
}