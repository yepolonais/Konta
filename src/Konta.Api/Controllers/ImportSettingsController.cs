using System.ComponentModel.DataAnnotations;
using Konta.Application.Imports;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/import-settings")]
public sealed class ImportSettingsController(IImportSettingsService importSettingsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ImportSettings>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportSettings>> Get(CancellationToken cancellationToken)
    {
        return Ok(await importSettingsService.GetAsync(cancellationToken));
    }

    [HttpPut]
    [ProducesResponseType<ImportSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportSettings>> Save(
        UpdateImportSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await importSettingsService.SaveAsync(
                request.BankIdentifier,
                request.BankUrl,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Paramètres d’import invalides",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}

public sealed record UpdateImportSettingsRequest
{
    [Required]
    [StringLength(100)]
    public string BankIdentifier { get; init; } = "";

    [Required]
    [StringLength(2048)]
    public string BankUrl { get; init; } = "";
}
