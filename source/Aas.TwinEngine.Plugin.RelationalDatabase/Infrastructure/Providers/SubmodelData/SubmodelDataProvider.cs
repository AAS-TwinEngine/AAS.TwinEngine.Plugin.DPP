using System.Data.Common;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.SqlExecutor;

using Npgsql;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData;

public class SubmodelDataProvider(ILogger<SubmodelDataProvider> logger,
    IJsonResponseParser jsonResponseParser
    , ISqlCommandExecutor sqlCommandExecutor) : ISubmodelDataProvider
{
    public async Task<SemanticTreeNode> GetSubmodelValuesAsync(string sqlQuery, string productId, CancellationToken cancellationToken)
    {
        var parameters = new List<DbParameter>
        {
            Create(productId)
        };

        var jsonResult = await sqlCommandExecutor.ExecuteQueryAsync(sqlQuery, parameters, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            logger.LogWarning("Query returned empty result for productId : {ProductId} ", productId);
            throw new ResponseNotFoundException();
        }

        var resultSemanticTreeNode = jsonResponseParser.ParseJson(jsonResult);

        return resultSemanticTreeNode;
    }

    public static DbParameter Create(object? value) => new NpgsqlParameter("@ProductId", value ?? DBNull.Value);
}
