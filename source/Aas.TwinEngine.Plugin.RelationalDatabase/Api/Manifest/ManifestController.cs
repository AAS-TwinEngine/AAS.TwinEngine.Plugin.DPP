using System.Text.Json.Nodes;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest;

[ApiController]
[Route("")]
[ApiVersion(1)]
public class ManifestController : ControllerBase
{
    [HttpGet("manifest")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> RetrieveManifestDataAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Feature not available: implementation is in progress.");
    }
}
