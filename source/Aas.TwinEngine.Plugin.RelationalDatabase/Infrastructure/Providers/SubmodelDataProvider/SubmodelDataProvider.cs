using System.Data;
using System.Threading;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelDataProvider.Helper;

using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelDataProvider;

public class SubmodelDataProvider(ILogger<SubmodelDataProvider> logger,
    IDbConnectionFactory connectionFactory) : ISubmodelDataProvider
{
    public async Task<SemanticTreeNode> GetSubmodelValuesAsync(string sqlQuery, string productId, CancellationToken cancellationToken)
    {
        var parameter = CreateProductIdParameter(productId);

        var jsonResult = await ExecuteQueryWithParameterAsync(sqlQuery, new[] { parameter }, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            logger.LogWarning("Query returned empty result for productId : {ProductId} ", productId);
            throw new ResponseNotFoundException();
        }

        var resultSemanticTreeNode = JsonResponseParser.ParseJson(jsonResult);

        return resultSemanticTreeNode;
    }

    private async Task<string?> ExecuteQueryWithParameterAsync(string query, SqlParameter[] parameters, CancellationToken cancellationToken)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new SqlCommand(query, connection);
        command.CommandTimeout = 30;

        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                ? null
                : reader.GetString(0);
        }

        return null;
    }
    private static SqlParameter CreateProductIdParameter(string productId)
    {
        return new SqlParameter("@ProductId", SqlDbType.NVarChar, 200)
        {
            Value = productId
        };
    }
}
