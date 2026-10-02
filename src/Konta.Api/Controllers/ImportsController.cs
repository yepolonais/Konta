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
    public async Task<ActionResult<ImportPreview>> Preview(IFormFile? file, CancellationToken cancellationToken)
    {
        var validation = ValidateFile(file);
        if (validation is not null)
        {
            return validation;
        }

        using var stream = file!.OpenReadStream();
        return Ok(await importPreviewService.PreviewAsync(stream, cancellationToken));
    }

    /// <summary>Imports the valid transactions from a reviewed Société Générale CSV.</summary>
    [HttpPost("commit")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumUploadBytes)]
    [ProducesResponseType<ImportCommitResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportCommitResult>> Commit(IFormFile? file, CancellationToken cancellationToken)
    {
        var validation = ValidateFile(file);
        if (validation is not null)
        {
            return validation;
        }

        try
        {
            using var stream = file!.OpenReadStream();
            return Ok(await importPreviewService.CommitAsync(stream, cancellationToken));
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
}