using System.Diagnostics.CodeAnalysis;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.Health;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("health")]
[ApiVersion(1)]
public class HealthController(ILogger<HealthController> logger, IHealthService healthService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var isHealthy = await healthService.IsHealthyAsync(cancellationToken).ConfigureAwait(false);

        if (isHealthy)
        {
            return Ok(new { status = "Healthy" });
        }

        logger.LogWarning("Health check reported unhealthy state");
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
    }
}
