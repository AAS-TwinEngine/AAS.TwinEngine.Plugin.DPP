using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

public class MetaDataService(IQueryProvider queryProvider, IMetaDataProvider metaDataProvider, ILogger<MetaDataService> logger) : IMetaDataService
{
    public async Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var sqlQuery = queryProvider.GetQuery("shells");
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                logger.LogError("SQL query not found for: shells");
                throw new SqlQueryNotFoundException();
            }

            var result = await metaDataProvider.GetShellDescriptorsAsync(sqlQuery, limit, cursor, cancellationToken).ConfigureAwait(false);

            return result!;
        }
        catch (ResourceNotFoundException)
        {
            throw new MetaDataNotFoundException();
        }
    }

    public async Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var sqlQuery = queryProvider.GetQuery("shell");
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                logger.LogError("SQL query not found for: shell");
                throw new SqlQueryNotFoundException();
            }
            var result = await metaDataProvider.GetShellDescriptorAsync(sqlQuery, aasIdentifier, cancellationToken).ConfigureAwait(false);
            return result!;
        }
        catch (ResourceNotFoundException)
        {
            throw new MetaDataNotFoundException();
        }
    }

    public async Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var sqlQuery = queryProvider.GetQuery("asset");
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                logger.LogError("SQL query not found for: asset");
                throw new SqlQueryNotFoundException();
            }
            var result = await metaDataProvider.GetAssetAsync(sqlQuery, assetIdentifier, cancellationToken).ConfigureAwait(false);
            return result!;
        }
        catch (ResourceNotFoundException)
        {
            throw new MetaDataNotFoundException();
        }
    }
}
