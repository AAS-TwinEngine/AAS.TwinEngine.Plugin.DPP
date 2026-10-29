using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;

using Asp.Versioning;

using Json.Schema;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("")]
[ApiVersion(1)]
public class SubmodelDataController(ISubmodelDataHandler submodelDataHandler) : ControllerBase
{
    [HttpPost("data/batch")]
    [ProducesResponseType(typeof(IReadOnlyList<GetSubmodelDataBatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<GetSubmodelDataBatchResponse>>> RetrieveBatchDataAsync([FromBody] IReadOnlyCollection<GetSubmodelDataBatchRequest> requests, CancellationToken cancellationToken)
    {
        var result = await submodelDataHandler
            .GetSubmodelDataAsync(requests, cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

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

    [HttpDelete("submodel-data/products/{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProductAsync([FromRoute] string productId, [FromServices] IProductHandler productHandler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productHandler);

        await productHandler.DeleteProductAsync(productId, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPut("submodel-data/products/{productId}")]
    [ProducesResponseType(typeof(UpdateProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateProductResponse>> UpdateProductAsync([FromRoute] string productId, [FromBody] UpdateProductRequest request, [FromServices] IProductHandler productHandler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productHandler);

        var response = await productHandler.UpdateProductAsync(productId, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("submodel-data/products")]
    [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateProductResponse>> CreateProductAsync([FromBody] CreateProductRequest request, [FromServices] IProductHandler productHandler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productHandler);

        var response = await productHandler.CreateProductAsync(request, cancellationToken).ConfigureAwait(false);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
