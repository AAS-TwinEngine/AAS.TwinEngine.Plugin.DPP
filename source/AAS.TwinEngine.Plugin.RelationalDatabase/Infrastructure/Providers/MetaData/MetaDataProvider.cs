using System.Data.Common;
using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

using Npgsql;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;

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

    private List<ShellDescriptorData>? DeserializeAndProcessItems(string jsonResult)
    {
        var allItems = JsonSerializer.Deserialize<List<ShellDescriptorData>>(jsonResult);
        if (allItems == null || allItems.Count == 0)
        {
            return null;
        }

        var validItems = new List<ShellDescriptorData>(allItems.Count);
        foreach (var item in allItems)
        {
            ApplyShellDescriptorDefaults(item);

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                logger.LogError("ShellDescriptor with null/empty Id excluded from response. GlobalAssetId: {GlobalAssetId}", item.GlobalAssetId);
                continue;
            }

            validItems.Add(item);
        }

        return validItems;
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

            return null;
        }

        var item = JsonSerializer.Deserialize<ShellDescriptorData>(jsonResult);

        if (item == null)
        {
            return null;
        }

        ApplyShellDescriptorDefaults(item);

        if (string.IsNullOrWhiteSpace(item.Id))
        {
            logger.LogError("Rejecting metadata-shells because the descriptor Id is null or empty. GlobalAssetId: {GlobalAssetId}", item.GlobalAssetId);
            throw new ValidationFailedException("Shell Id is null or empty.");
        }

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
