namespace AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

public class AssetIdFilterHeader
{
    public required IList<SpecificAssetIdsData> Identifiers { get; init; }
}
