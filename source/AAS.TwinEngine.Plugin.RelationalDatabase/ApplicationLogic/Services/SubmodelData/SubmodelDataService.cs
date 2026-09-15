using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Observability;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

using IQueryProvider = AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SubmodelDataService(ISubmodelMetadataExtractor submodelMetadataExtractor,
    ISemanticIdToColumnMapper semanticIdToColumnMapper,
    ISemanticTreeResponseBuilder semanticTreeResponseBuilder,
    IQueryProvider queryProvider,
    ISubmodelDataProvider submodelDataProvider,
    ILogger<SubmodelDataService> logger) : ISubmodelDataService
{
    public async Task<SemanticTreeNode> GetValuesBySemanticIds(JsonSchema jsonSchema, string submodelId, CancellationToken cancellationToken)
    {
        using var span = PluginTracing.StartSpan(PluginTracing.Spans.FetchingData, PluginTracing.Attributes.SubmodelId, submodelId);

        try
        {
            var requestSemanticTreeNode = JsonSchemaParser.ParseJsonSchema(jsonSchema, logger);

            var extractionResult = submodelMetadataExtractor.ExtractSubmodelMetadata(submodelId);
            _ = span?.SetTag(PluginTracing.Attributes.ProductId, extractionResult.ProductId);

            var semanticIdToColumnMapping = semanticIdToColumnMapper.GetSemanticIdToColumnMapping(requestSemanticTreeNode);

            var sqlQuery = GetSqlQueryForSubmodel(extractionResult.SubmodelName.ToString());

            var responseSemanticTreeNode = await submodelDataProvider.GetSubmodelValuesAsync(sqlQuery, extractionResult.ProductId, cancellationToken).ConfigureAwait(false);

            var result = semanticTreeResponseBuilder.BuildResponse(requestSemanticTreeNode, responseSemanticTreeNode, semanticIdToColumnMapping);

            return result;
        }
        catch (Exception ex)
        {
            throw HandleSubmodelDataException(ex);
        }
    }

    public async Task<IReadOnlyDictionary<string, SemanticTreeNode>> GetValuesBySemanticIds(
        JsonSchema jsonSchema,
        IReadOnlyList<string> submodelIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jsonSchema);
        ArgumentNullException.ThrowIfNull(submodelIds);

        if (submodelIds.Count == 0)
        {
            throw new InvalidUserInputException();
        }

        using var span = PluginTracing.StartSpan(
            PluginTracing.Spans.FetchingData,
            PluginTracing.Attributes.SubmodelId,
            submodelIds[0]);

        try
        {
            var firstMetadata = submodelMetadataExtractor.ExtractSubmodelMetadata(submodelIds[0]);
            var requestSemanticTreeNode = JsonSchemaParser.ParseJsonSchema(jsonSchema, logger);
            var semanticIdToColumnMapping = semanticIdToColumnMapper.GetSemanticIdToColumnMapping(requestSemanticTreeNode);
            var sqlQuery = GetSqlQueryForSubmodel(firstMetadata.SubmodelName.ToString());

            var productIds = submodelIds
                .Select(submodelMetadataExtractor.ExtractProductId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var responseSemanticTreeNodes = await submodelDataProvider
                .GetSubmodelValuesAsync(sqlQuery, productIds, cancellationToken)
                .ConfigureAwait(false);

            var resultTasks = submodelIds.Select(submodelId => Task.Run(() =>
            {
                var productId = submodelMetadataExtractor.ExtractProductId(submodelId);
                if (!responseSemanticTreeNodes.TryGetValue(productId, out var responseSemanticTreeNode))
                {
                    throw new ResourceNotValidException();
                }

                var resultSemanticTreeNode = JsonSchemaParser.ParseJsonSchema(jsonSchema, logger);
                return new
                {
                    SubmodelId = submodelId,
                    Result = semanticTreeResponseBuilder.BuildResponse(
                        resultSemanticTreeNode,
                        responseSemanticTreeNode,
                        semanticIdToColumnMapping)
                };
            }, cancellationToken));

            var results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
            return results.ToDictionary(result => result.SubmodelId, result => result.Result, StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            throw HandleSubmodelDataException(ex);
        }
    }

    private string GetSqlQueryForSubmodel(string submodelName)
    {
        var sqlQuery = queryProvider.GetQuery(submodelName);
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new QueryNotAvailableException();
        }

        return sqlQuery;
    }

    private static Exception HandleSubmodelDataException(Exception exception)
    {
        return exception switch
        {
            ResourceNotFoundException ex => new SubmodelDataNotFoundException(ex),

            ResourceNotValidException ex => new SubmodelDataNotFoundException(ex),

            ResponseParsingException ex => new InternalDataProcessingException(ex),

            ValidationFailedException ex => new InternalDataProcessingException(ex),

            _ => exception
        };
    }
}
