using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.MappingProfiles;

public static class AssetMappingProfile
{
    public static AssetDto ToDto(this AssetData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new AssetDto
        {
            GlobalAssetId = data.GlobalAssetId,
            SpecificAssetIds = data.SpecificAssetIds?.Select(id => new SpecificAssetIdsDto
            {
                Name = id.Name,
                Value = id.Value
            }).ToList(),
            DefaultThumbnail = data.DefaultThumbnail == null
                                   ? null
                                   : new DefaultThumbnailDto
                                   {
                                       ContentType = data.DefaultThumbnail.ContentType,
                                       Path = data.DefaultThumbnail.Path
                                   }
        };
    }
}
