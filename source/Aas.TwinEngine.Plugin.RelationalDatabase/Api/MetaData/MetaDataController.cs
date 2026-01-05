using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Requests;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("metadata")]
[ApiVersion(1)]
public class MetaDataController(
    IMetaDataHandler metaDataHandler) : ControllerBase
{
    [HttpGet("shells")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetShellDescriptorsAsync([FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken)
    {
        var request = new GetShellDescriptorsRequest(limit, cursor);

        var response = await metaDataHandler.GetShellDescriptors(request, cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("shells/{aasIdentifier}")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetShellDescriptorAsync([FromRoute] string aasIdentifier, CancellationToken cancellationToken)
    {
        var request = new GetShellDescriptorRequest(aasIdentifier);

        var response = await metaDataHandler.GetShellDescriptor(request, cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("assets/{shellIdentifier}")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetAssetAsync([FromRoute] string shellIdentifier, CancellationToken cancellationToken)
    {
        var request = new GetAssetRequest(shellIdentifier);

        var response = await metaDataHandler.GetAsset(request, cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }
}
