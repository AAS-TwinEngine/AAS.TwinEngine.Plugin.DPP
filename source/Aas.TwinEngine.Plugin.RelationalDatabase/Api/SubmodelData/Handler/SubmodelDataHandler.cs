using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

using NJsonSchema.Validation;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public class SubmodelDataHandler(
    ILogger<SubmodelDataHandler> logger,
    ISubmodelDataService submodelDataService,
    IJsonSchemaValidator jsonSchemaValidator,
    ISemanticTreeHandler semanticTreeHandler) : ISubmodelDataHandler
{
    public async Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request for submodel ID: {SubmodelId}", request?.submodelId);

        var decodedSubmodelId = request?.submodelId.DecodeBase64(logger);

        jsonSchemaValidator.ValidateRequestSchema(request!.dataQuery);

        var filledSemanticIds = await submodelDataService.GetValuesBySemanticIds(request.dataQuery, decodedSubmodelId!, cancellationToken).ConfigureAwait(false);

        var result = semanticTreeHandler.GetJson(filledSemanticIds, request.dataQuery);

        return result;
    }
}
