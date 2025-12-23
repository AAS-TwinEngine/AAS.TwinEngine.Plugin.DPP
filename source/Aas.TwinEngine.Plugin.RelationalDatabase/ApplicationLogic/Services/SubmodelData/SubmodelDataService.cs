using System.Text.Json;
using System.Text.RegularExpressions;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

using Azure.Core;

using Json.Schema;

using Microsoft.Extensions.Options;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Sharad.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SubmodelDataService(ISubmodelMetadataExtractor submodelMetadataExtractor,
    ISemanticIdToColumnMapper semanticIdToColumnMapper,
    ISemanticTreeResponseBuilder semanticTreeResponseBuilder,
    IQueryProvider queryProvider,
    ISubmodelDataProvider submodelDataProvider,
    ILogger<SubmodelDataService> logger) : ISubmodelDataService
{

    public async Task<SemanticTreeNode> GetValuesBySemanticIds(JsonSchema jsonSchema, string submodelId, CancellationToken cancellationToken)
    {
        var requestSemanticTreeNode = JsonSchemaParser.ParseJsonSchema(jsonSchema, logger);

        var extractionResult = submodelMetadataExtractor.ExtractSubmodelMetadata(submodelId);

        var semanticIdToColumnMapping = semanticIdToColumnMapper.GetSemanticIdToColumnMapping(requestSemanticTreeNode);

        var sqlQuery = GetSqlQueryForSubmodel(extractionResult.SubmodelName.ToString());

        var responseSemanticTreeNode = await submodelDataProvider.GetSubmodelValuesAsync(sqlQuery, extractionResult.ProductId, cancellationToken).ConfigureAwait(false);

        var result = semanticTreeResponseBuilder.BuildResponse(requestSemanticTreeNode, responseSemanticTreeNode, semanticIdToColumnMapping);

        return result;
    }

    private string GetSqlQueryForSubmodel(string submodelName)
    {
        var sqlQuery = queryProvider.GetQuery(submodelName);
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new InvalidOperationException($"SQL query not found for: {submodelName}");
        }

        return sqlQuery;
    }
}
