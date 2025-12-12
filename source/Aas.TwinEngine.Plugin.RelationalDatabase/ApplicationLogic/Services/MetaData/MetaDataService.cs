using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

public class MetaDataService(IQueryProvider queryProvider, IMetaDataProvider metaDataProvider) : IMetaDataService
{
    public Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var sqlQuery = queryProvider.GetQuery("shells");
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new InvalidOperationException($"SQL query not found for: shells");
        }

        var result = metaDataProvider.GetShellDescriptorsAsync(sqlQuery, limit, cursor, cancellationToken);

        return result!;
    }

    public Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        var sqlQuery = queryProvider.GetQuery("shell");
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new InvalidOperationException($"SQL query not found for: shells");
        }
        var result = metaDataProvider.GetShellDescriptorAsync(sqlQuery, aasIdentifier, cancellationToken);
        return result!;
    }

    public Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken)
    {
        var sqlQuery = queryProvider.GetQuery("asset");
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new InvalidOperationException($"SQL query not found for: shells");
        }
        var result = metaDataProvider.GetAssetAsync(sqlQuery, assetIdentifier, cancellationToken);
        return result!;
    }
}
