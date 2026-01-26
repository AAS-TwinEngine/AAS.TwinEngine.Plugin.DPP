using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
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
        try
        {
            var requestSemanticTreeNode = JsonSchemaParser.ParseJsonSchema(jsonSchema, logger);

            var extractionResult = submodelMetadataExtractor.ExtractSubmodelMetadata(submodelId);

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
