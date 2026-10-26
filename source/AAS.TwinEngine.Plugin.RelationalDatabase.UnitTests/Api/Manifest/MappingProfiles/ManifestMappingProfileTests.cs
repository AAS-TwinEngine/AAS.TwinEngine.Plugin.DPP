using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.MappingProfiles;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.Manifest.MappingProfiles;

public class ManifestMappingProfileTests
{
    [Fact]
    public void ToDto_ShouldMapManifestDataToManifestDtoCorrectly()
    {
        var manifestData = new ManifestData
        {
            Capabilities = new CapabilitiesData
            {
                HasAssetInformation = true,
                HasShellDescriptor = false,
                HasAssetIdSearch = true,
                HasAssetKindTypeFilter = false,
                HasSubmodelBatch = true
            },
            SupportedSemanticIds = ["semantic1", "semantic2"]
        };

        var result = manifestData.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.Capabilities);
        Assert.Equal(manifestData.Capabilities.HasAssetInformation, result.Capabilities.HasAssetInformation);
        Assert.Equal(manifestData.Capabilities.HasShellDescriptor, result.Capabilities.HasShellDescriptor);
        Assert.Equal(manifestData.Capabilities.HasAssetIdSearch, result.Capabilities.HasAssetIdSearch);
        Assert.Equal(manifestData.Capabilities.HasAssetKindTypeFilter, result.Capabilities.HasAssetKindTypeFilter);
        Assert.Equal(manifestData.Capabilities.HasSubmodelBatch, result.Capabilities.HasSubmodelBatch);
        Assert.Equal(manifestData.SupportedSemanticIds, result.SupportedSemanticIds);
    }

    [Fact]
    public void ToDto_ShouldThrowException_WhenManifestDataIsNull()
    {
        ManifestData? manifestData = null;

        Assert.Throws<ArgumentNullException>(() => manifestData!.ToDto());
    }
}
