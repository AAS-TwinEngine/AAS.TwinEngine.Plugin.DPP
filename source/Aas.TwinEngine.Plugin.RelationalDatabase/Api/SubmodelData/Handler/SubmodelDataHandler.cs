using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public class SubmodelDataHandler(
    ILogger<SubmodelDataHandler> logger,
    ISubmodelDataService submodelDataService,
    IJsonSchemaParser jsonSchemaParser,
    ISemanticTreeHandler semanticTreeHandler) : ISubmodelDataHandler
{
    public async Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Start executing get request for product data");

        logger.LogInformation("Processing request for submodel ID: {SubmodelId}", request?.submodelId);

        var semanticIds = jsonSchemaParser.ParseJsonSchema(request!.dataQuery);

        var filledSemanticIds = await submodelDataService.GetValuesBySemanticIds(semanticIds, request.submodelId).ConfigureAwait(false);

        var result = semanticTreeHandler.GetJson(filledSemanticIds, request.dataQuery);

        return result;
    }
}
