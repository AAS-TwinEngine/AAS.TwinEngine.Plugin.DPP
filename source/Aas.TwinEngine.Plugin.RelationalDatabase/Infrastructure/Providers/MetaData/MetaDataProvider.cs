using System.Data.Common;
using System.Text.Json;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

using Npgsql;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;

public class MetaDataProvider(ILogger<MetaDataProvider> logger, IQueryExecutor queryExecutor) : IMetaDataProvider
{
    public async Task<ShellDescriptorsData?> GetShellDescriptorsAsync(string query, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var jsonResult = await queryExecutor.ExecuteQueryAsync(query, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            return new ShellDescriptorsData
            {
                PagingMetaData = new PagingMetaData { Cursor = null },
                Result = Array.Empty<ShellDescriptorData>()
            };
        }

        var allItems = DeserializeAndProcessItems(jsonResult);

        if (allItems == null || allItems.Count == 0)
        {
            return new ShellDescriptorsData
            {
                PagingMetaData = new PagingMetaData { Cursor = null },
                Result = Array.Empty<ShellDescriptorData>()
            };
        }

        var (pagedItems, pagingMetaData) = Paginator.GetPagedResult(
            allItems,
            getId: x => x.GlobalAssetId,
            limit,
            cursor
        );

        return new ShellDescriptorsData
        {
            PagingMetaData = pagingMetaData,
            Result = pagedItems
        };
    }

    private static List<ShellDescriptorData>? DeserializeAndProcessItems(string jsonResult)
    {
        var allItems = JsonSerializer.Deserialize<List<ShellDescriptorData>>(jsonResult);
        if (allItems == null || allItems.Count == 0)
        {
            return null;
        }

        foreach (var item in allItems)
        {
            ApplyShellDescriptorDefaults(item);
        }

        return allItems;
    }

    public async Task<ShellDescriptorData?> GetShellDescriptorAsync(string query, string aasIdentifier, CancellationToken cancellationToken)
    {
        var parameters = new List<DbParameter>
        {
            Create("@aasId", aasIdentifier)
        };

        var jsonResult = await queryExecutor.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            logger.LogWarning("ShellDescriptor not found for AAS Identifier: {AasIdentifier}", aasIdentifier);

            return new ShellDescriptorData();
        }

        var item = JsonSerializer.Deserialize<ShellDescriptorData>(jsonResult);

        if (item == null)
        {
            return new ShellDescriptorData();
        }

        ApplyShellDescriptorDefaults(item);

        return item;
    }

    public async Task<AssetData?> GetAssetAsync(string query, string assetIdentifier, CancellationToken cancellationToken)
    {
        var parameters = new List<DbParameter>
        {
            Create("@aasId", assetIdentifier)
        };

        var jsonResult = await queryExecutor.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            logger.LogWarning("Asset not found for Asset Identifier: {AssetIdentifier}", assetIdentifier);

            return new AssetData();
        }

        var asset = JsonSerializer.Deserialize<AssetData>(jsonResult);

        return asset ?? new AssetData();
    }

    private static void ApplyShellDescriptorDefaults(ShellDescriptorData item)
    {
        item.Id ??= item.GlobalAssetId;

        if (item.SpecificAssetIds == null)
        {
            return;
        }

        foreach (var sai in item.SpecificAssetIds)
        {
            sai.Name ??= sai.Value;
        }
    }

    public static DbParameter Create(string name, object? value) => new NpgsqlParameter(name, value ?? DBNull.Value);
}
