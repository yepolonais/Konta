using Konta.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/projection")]
public sealed class ProjectionController(IMonthlyProjectionService projectionService) : ControllerBase
{
    /// <summary>Returns the current month's projection for the designated common account.</summary>
    [HttpGet]
    [ProducesResponseType<MonthlyProjection>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MonthlyProjection>> Get(CancellationToken cancellationToken)
    {
        var projection = await projectionService.GetAsync(cancellationToken);
        return projection is null ? NotFound() : Ok(projection);
    }
}
