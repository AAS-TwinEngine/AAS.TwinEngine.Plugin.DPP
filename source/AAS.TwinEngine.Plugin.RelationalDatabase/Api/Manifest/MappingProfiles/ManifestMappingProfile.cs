using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Responses;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.MappingProfiles;

public static class ManifestMappingProfile
{
    public static ManifestDto ToDto(this ManifestData? data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new ManifestDto()
        {
            Capabilities = new CapabilitiesDto()
            {
                HasAssetInformation = data.Capabilities.HasAssetInformation,
                HasShellDescriptor = data.Capabilities.HasShellDescriptor
            },
            SupportedSemanticIds = data.SupportedSemanticIds
        };
    }
}
