using Microsoft.AspNetCore.Mvc;

namespace Konta.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Returns the API health status.</summary>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("ok"));
}

/// <summary>Health status returned by the API.</summary>
public sealed record HealthResponse(string Status);