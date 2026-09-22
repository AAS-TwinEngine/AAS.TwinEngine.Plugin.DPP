using System.Data.Common;
using System.Text.Json;

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
        var results = await GetSubmodelValuesAsync(sqlQuery, new[] { productId }, cancellationToken).ConfigureAwait(false);

        return results[productId];
    }

    public async Task<IReadOnlyDictionary<string, SemanticTreeNode>> GetSubmodelValuesAsync(string sqlQuery, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        if (productIds.Count == 0)
        {
            throw new ResourceNotValidException();
        }

        var parameters = new List<DbParameter>
        {
            CreateProductIds(productIds)
        };

        var jsonResult = await queryExecutor.ExecuteQueryAsync(sqlQuery, parameters, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            logger.LogError("Query returned empty results for {ProductCount} products", productIds.Count);
            throw new ResourceNotValidException();
        }

        using var document = JsonDocument.Parse(jsonResult);
        var results = new Dictionary<string, SemanticTreeNode>(StringComparer.Ordinal);

        foreach (var productId in productIds)
        {
            if (!document.RootElement.TryGetProperty(productId, out var productResult))
            {
                logger.LogError("Query returned no result for productId: {ProductId}", productId);
                throw new ResourceNotValidException();
            }

            results[productId] = jsonResponseParser.ParseJson(productResult.GetRawText());
        }

        return results;
    }

    public static DbParameter CreateProductIds(IEnumerable<string> productIds) => new NpgsqlParameter("@ProductIds", productIds.ToArray());
}
