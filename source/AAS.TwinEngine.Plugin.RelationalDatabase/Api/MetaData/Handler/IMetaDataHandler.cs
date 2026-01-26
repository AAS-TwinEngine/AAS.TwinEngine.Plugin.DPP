using AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Responses;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Handler;

public interface IMetaDataHandler
{
    Task<ShellDescriptorsDto> GetShellDescriptors(GetShellDescriptorsRequest request, CancellationToken cancellationToken);

    Task<ShellDescriptorDto> GetShellDescriptor(GetShellDescriptorRequest request, CancellationToken cancellationToken);

    Task<AssetDto> GetAsset(GetAssetRequest request, CancellationToken cancellationToken);
}
