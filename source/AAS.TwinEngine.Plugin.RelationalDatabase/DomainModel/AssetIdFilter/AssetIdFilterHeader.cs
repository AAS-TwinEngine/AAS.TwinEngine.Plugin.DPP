namespace AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.AssetIdFilter;

public class AssetIdFilterHeader
{
    public required IList<SpecificAssetIdData> Identifiers { get; init; }
}
