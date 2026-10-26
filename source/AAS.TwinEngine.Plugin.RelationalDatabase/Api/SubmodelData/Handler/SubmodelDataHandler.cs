using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public class SubmodelDataHandler(ILogger<SubmodelDataHandler> logger, ISubmodelDataService submodelDataService, IJsonSchemaValidator jsonSchemaValidator, ISemanticTreeHandler semanticTreeHandler) : ISubmodelDataHandler
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

    public async Task<IReadOnlyList<GetSubmodelDataBatchResponse>> GetSubmodelDataAsync(IReadOnlyCollection<GetSubmodelDataBatchRequest> requests, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            throw new InvalidUserInputException();
        }

        var batchGroupProcessingTasks = requests.Select(request => ProcessBatchGroupAsync(request, cancellationToken));
        var responsesPerBatchGroup = await Task.WhenAll(batchGroupProcessingTasks).ConfigureAwait(false);

        return responsesPerBatchGroup.SelectMany(group => group).ToArray();
    }

    private async Task<IReadOnlyList<GetSubmodelDataBatchResponse>> ProcessBatchGroupAsync(GetSubmodelDataBatchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var decodedIds = DecodeAndDeduplicateIds(request.SubmodelIds);
        var semanticTrees = await submodelDataService
            .GetValuesBySemanticIds(request.Schema, decodedIds, cancellationToken)
            .ConfigureAwait(false);

        var responseTasks = decodedIds.Select(submodelId => Task.Run(() =>
        {
            if (!semanticTrees.TryGetValue(submodelId, out var semanticTree))
            {
                throw new SubmodelDataNotFoundException();
            }

            return new GetSubmodelDataBatchResponse(
                submodelId,
                semanticTreeHandler.GetJson(semanticTree, request.Schema, validateResponse: false));
        }, cancellationToken));

        return await Task.WhenAll(responseTasks).ConfigureAwait(false);
    }

    private IReadOnlyList<string> DecodeAndDeduplicateIds(IReadOnlyCollection<string> encodedIds)
    {
        ArgumentNullException.ThrowIfNull(encodedIds);

        var decodedIds = new List<string>(encodedIds.Count);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var encodedId in encodedIds)
        {
            var decodedId = encodedId.DecodeBase64(logger);
            if (!seenIds.Add(decodedId))
            {
                logger.LogWarning("Skipping duplicate submodel ID in batch group: {SubmodelId}", decodedId);
                continue;
            }

            decodedIds.Add(decodedId);
        }

        if (decodedIds.Count == 0)
        {
            throw new InvalidUserInputException();
        }

        return decodedIds;
    }

    private async Task<TDto> GetResourceByIdAsync<TModel, TDto>(string? encodedId, string resourceName, Func<string, Task<TModel?>> fetchFunc, Func<TModel, TDto> mapFunc)
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
