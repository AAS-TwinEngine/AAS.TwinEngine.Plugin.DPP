using AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Responses;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.MappingProfiles;

public static class ShellDescriptorProfile
{
    public static ShellDescriptorDto ToDto(this ShellDescriptorData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new ShellDescriptorDto
        {
            GlobalAssetId = data.GlobalAssetId,
            IdShort = data.IdShort,
            Id = data.Id,
            SpecificAssetIds = data.SpecificAssetIds?
                                 .Select(x => new SpecificAssetIdsDto
                                 {
                                     Name = x.Name,
                                     Value = x.Value
                                 })
                                 .ToList()
        };
    }
}
