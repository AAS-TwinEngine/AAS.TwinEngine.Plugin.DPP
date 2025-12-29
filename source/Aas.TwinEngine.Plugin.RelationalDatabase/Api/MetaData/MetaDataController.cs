using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("metadata")]
[ApiVersion(1)]
public class MetaDataController : ControllerBase
{
    [HttpGet("shells")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetShellDescriptorsAsync([FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken) => throw new NotImplementedException("Feature not available: implementation is in progress.");

    [HttpGet("shells/{AasIdentifier}")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetShellDescriptorAsync([FromRoute] string aasIdentifier, CancellationToken cancellationToken) => throw new NotImplementedException("Feature not available: implementation is in progress.");

    [HttpGet("assets/{shellIdentifier}")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetAssetAsync([FromRoute] string shellIdentifier, CancellationToken cancellationToken) => throw new NotImplementedException("Feature not available: implementation is in progress.");
}
