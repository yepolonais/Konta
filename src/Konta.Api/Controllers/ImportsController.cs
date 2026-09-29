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
    public ActionResult<ImportPreview> Preview(IFormFile? file)
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

        using var stream = file.OpenReadStream();
        return Ok(importPreviewService.Preview(stream));
    }
}