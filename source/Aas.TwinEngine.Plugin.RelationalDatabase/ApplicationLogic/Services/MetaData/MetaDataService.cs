using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Enums;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

public class MetaDataService(IQueryProvider queryProvider, IMetaDataProvider metaDataProvider, ILogger<MetaDataService> logger) : IMetaDataService
{
    public async Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var query = GetValidatedQuery(MetaDataEndpoints.Shells);
        var result = await metaDataProvider.GetShellDescriptorsAsync(query, limit, cursor, cancellationToken).ConfigureAwait(false);
        if (result?.Result != null)
        {
            return result;
        }

        logger.LogError("Shell descriptors not found for limit: {Limit}, cursor: {Cursor}", limit, cursor);
        throw new ShellMetaDataNotFoundException();
    }

    public async Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        var query = GetValidatedQuery(MetaDataEndpoints.Shell);
        var result = await metaDataProvider.GetShellDescriptorAsync(query, aasIdentifier, cancellationToken).ConfigureAwait(false);
        if (result != null)
        {
            return result;
        }

        logger.LogError("Shell descriptor not found for AAS Identifier: {AasIdentifier}", aasIdentifier);
        throw new ShellMetaDataNotFoundException();
    }

    public async Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken)
    {
        var query = GetValidatedQuery(MetaDataEndpoints.Asset);
        var result = await metaDataProvider.GetAssetAsync(query, assetIdentifier, cancellationToken).ConfigureAwait(false);
        if (result != null)
        {
            return result;
        }

        logger.LogError("Asset not found for Asset Identifier: {AssetIdentifier}", assetIdentifier);
        throw new AssetMetaDataNotFoundException();
    }

    private string GetValidatedQuery(string queryType)
    {
        var query = queryProvider.GetQuery(queryType);
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogError("Query not found for: {QueryType}", queryType);
            throw new QueryNotAvailableException();
        }

        return query;
    }
}
