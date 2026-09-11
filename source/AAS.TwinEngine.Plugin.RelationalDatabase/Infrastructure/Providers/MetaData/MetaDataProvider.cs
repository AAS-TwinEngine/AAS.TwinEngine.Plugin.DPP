using System.Data.Common;
using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

using Npgsql;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;

public class MetaDataProvider(ILogger<MetaDataProvider> logger, IQueryExecutor queryExecutor) : IMetaDataProvider
{
    private const int DefaultPageSize = 100;

    public async Task<ShellDescriptorsData?> GetShellDescriptorsAsync(string query, int? limit, string? cursor, AssetIdFilterHeader? filter, string? idShort, string? assetKind, string? assetType, CancellationToken cancellationToken)
    {
        var pageSize = limit ?? DefaultPageSize;
        var (filteredQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, idShort, assetKind, assetType, Create, cursor, pageSize);

        var jsonResult = parameters.Count == 0
            ? await queryExecutor.ExecuteQueryAsync(filteredQuery, cancellationToken).ConfigureAwait(false)
            : await queryExecutor.ExecuteQueryAsync(filteredQuery, parameters, cancellationToken).ConfigureAwait(false);

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

        var (pagedItems, nextCursor) = BuildPagedResult(allItems, pageSize);

        return new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = nextCursor },
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
                LogDescriptorIdMissing(item);
                continue;
            }

            validItems.Add(item);
        }

        return validItems;
    }

    private static (IList<ShellDescriptorData> Items, string? NextCursor) BuildPagedResult(IList<ShellDescriptorData> allItems, int pageSize)
    {
        var hasMore = allItems.Count > pageSize;
        var pagedItems = allItems.Take(pageSize).ToList();

        if (!hasMore)
        {
            return (pagedItems, null);
        }

        var lastItem = pagedItems.LastOrDefault();
        return (pagedItems, lastItem?.Id.EncodeBase64());
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
            LogDescriptorIdMissing(item);
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

    private void LogDescriptorIdMissing(ShellDescriptorData item)
    {
        var globalAssetId = string.IsNullOrWhiteSpace(item.GlobalAssetId) ? "<null>" : item.GlobalAssetId;
        var idShort = string.IsNullOrWhiteSpace(item.IdShort) ? "<null>" : item.IdShort;

        logger.LogError("Metadata-Shell has null or empty Id. GlobalAssetId: {GlobalAssetId}, IdShort: {IdShort}", globalAssetId, idShort);
    }

    public static DbParameter Create(string name, object? value) => new NpgsqlParameter(name, value ?? DBNull.Value);
}
