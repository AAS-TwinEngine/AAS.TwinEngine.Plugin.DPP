using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;

public interface IMetaDataProvider
{
    Task<ShellDescriptorsData?> GetShellDescriptorsAsync(string query, int? limit, string? cursor, CancellationToken cancellationToken);

    Task<ShellDescriptorData?> GetShellDescriptorAsync(string query, string aasIdentifier, CancellationToken cancellationToken);

    Task<AssetData?> GetAssetAsync(string query, string assetIdentifier, CancellationToken cancellationToken);
}
