using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

public interface IMetaDataService
{
    Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken);

    Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken);
}
