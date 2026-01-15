using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

using Asp.Versioning;

using Json.Schema;

using Microsoft.AspNetCore.Mvc;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("")]
[ApiVersion(1)]
public class SubmodelDataController(ISubmodelDataHandler submodelDataHandler) : ControllerBase
{
    [HttpPost("data/{submodelId}")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> RetrieveDataAsync([FromBody] JsonSchema dataQuery, [FromRoute] string submodelId, CancellationToken cancellationToken)
    {
        var request = new GetSubmodelDataRequest(submodelId, dataQuery);

        var aasData = await submodelDataHandler.GetSubmodelData(request, cancellationToken).ConfigureAwait(false);

        return Ok(aasData);
    }
}
