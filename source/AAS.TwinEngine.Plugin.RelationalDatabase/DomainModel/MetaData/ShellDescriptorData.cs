namespace AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

public class ShellDescriptorData
{
    public string GlobalAssetId { get; set; } = null!;
    public string IdShort { get; set; } = null!;
    public string Id { get; set; } = string.Empty;
    public IList<SpecificAssetIdsData>? SpecificAssetIds { get; init; } = [];
}

public class SpecificAssetIdsData
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}
