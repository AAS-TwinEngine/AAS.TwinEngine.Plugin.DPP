using AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.MappingProfiles;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.MetaData.MappingProfiles;

public class ShellDescriptorsProfileTests
{
    [Fact]
    public void ToDto_MapsAllFieldsCorrectly()
    {
        var shellDescriptorsData = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "cursor-123" },
            Result = new List<ShellDescriptorData>
            {
                new()
                {
                    Id = "shell-001",
                    GlobalAssetId = "asset-001",
                    IdShort = "Shell001",
                    DefaultThumbnail = new DefaultThumbnailData
                    {
                        Path = "/images/thumb.png",
                        ContentType = "image/png"
                    },
                    SpecificAssetIds = [new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN001" }]
                }
            }
        };

        var result = shellDescriptorsData.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.PagingMetaData);
        Assert.Equal("cursor-123", result.PagingMetaData.Cursor);
        Assert.Single(result.Result!);
        Assert.Equal("shell-001", result.Result![0].Id);
        Assert.Equal("asset-001", result.Result[0].GlobalAssetId);
        Assert.Equal("Shell001", result.Result[0].IdShort);
        Assert.NotNull(result.Result[0].DefaultThumbnail);
        Assert.Equal("/images/thumb.png", result.Result[0].DefaultThumbnail!.Path);
        Assert.Equal("image/png", result.Result[0].DefaultThumbnail!.ContentType);
        Assert.Single(result.Result[0]!.SpecificAssetIds!);
        Assert.Equal("SerialNumber", result.Result[0].SpecificAssetIds?[0]!.Name);
        Assert.Equal("SN001", result.Result[0].SpecificAssetIds?[0].Value);
    }

    [Fact]
    public void ToDto_HandlesNullResultList()
    {
        var shellDescriptorsData = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "cursor-456" },
            Result = null
        };

        var result = shellDescriptorsData.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.PagingMetaData);
        Assert.Equal("cursor-456", result.PagingMetaData.Cursor);
        Assert.Null(result.Result);
    }
}
