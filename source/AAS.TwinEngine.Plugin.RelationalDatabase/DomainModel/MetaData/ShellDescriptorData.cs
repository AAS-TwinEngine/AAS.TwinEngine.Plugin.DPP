namespace AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

public class ShellDescriptorData
{
    public string GlobalAssetId { get; set; } = null!;
    public string IdShort { get; set; } = null!;
    public string Id { get; set; } = null!;
    public IList<SpecificAssetIdsData>? SpecificAssetIds { get; init; } = [];
}

public class SpecificAssetIdsData
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}
