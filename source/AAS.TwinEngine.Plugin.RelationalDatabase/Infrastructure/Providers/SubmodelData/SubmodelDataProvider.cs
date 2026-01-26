using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;

using Npgsql;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData;

public class SubmodelDataProvider(ILogger<SubmodelDataProvider> logger, IJsonResponseParser jsonResponseParser, IQueryExecutor queryExecutor) : ISubmodelDataProvider
{
    public async Task<SemanticTreeNode> GetSubmodelValuesAsync(string sqlQuery, string productId, CancellationToken cancellationToken)
    {
        var parameters = new List<DbParameter>
        {
            Create(productId)
        };

        var jsonResult = await queryExecutor.ExecuteQueryAsync(sqlQuery, parameters, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            logger.LogError("Query returned empty result for productId : {ProductId} ", productId);
            throw new ResourceNotValidException();
        }

        var resultSemanticTreeNode = jsonResponseParser.ParseJson(jsonResult);

        return resultSemanticTreeNode;
    }

    public static DbParameter Create(object? value) => new NpgsqlParameter("@ProductId", value ?? DBNull.Value);
}
