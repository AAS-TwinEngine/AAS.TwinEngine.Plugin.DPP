using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public class SubmodelDataHandler(
    ILogger<SubmodelDataHandler> logger,
    ISubmodelDataService submodelDataService,
    IJsonSchemaValidator jsonSchemaValidator,
    ISemanticTreeHandler semanticTreeHandler) : ISubmodelDataHandler
{
    public Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        jsonSchemaValidator.ValidateRequestSchema(request.dataQuery);

        return GetResourceByIdAsync(
            request.submodelId,
            "submodel-data",
            async (decodedId) =>
            {
                var semanticTree = await submodelDataService.GetValuesBySemanticIds(
                    request.dataQuery,
                    decodedId,
                    cancellationToken).ConfigureAwait(false);

                return semanticTree;
            },
            (semanticTree) => semanticTreeHandler.GetJson(semanticTree, request.dataQuery)
        );
    }

    private async Task<TDto> GetResourceByIdAsync<TModel, TDto>(
        string? encodedId,
        string resourceName,
        Func<string, Task<TModel?>> fetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        var decodedId = encodedId?.DecodeBase64(logger);
        logger.LogInformation("Start executing get request for {ResourceName}. Identifier: {DecodedId}", resourceName, decodedId);

        var result = await fetchFunc(decodedId!).ConfigureAwait(false);
        ValidateResourceExists(result, resourceName, decodedId!);

        return mapFunc(result!);
    }

    private void ValidateResourceExists<T>(T? result, string resourceName, string? decodedId = null)
    {
        if (result is null)
        {
            if (decodedId is not null)
            {
                logger.LogError("{ResourceName} not found for Identifier: {DecodedId}", resourceName, decodedId);
            }
            else
            {
                logger.LogError("{ResourceName} not found.", resourceName);
            }

            throw new NotFoundException();
        }
    }
}
