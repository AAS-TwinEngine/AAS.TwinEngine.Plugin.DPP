namespace AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

public class AssetData
{
    public string? AssetKind { get; set; }

    public string? AssetType { get; set; }

    public string? GlobalAssetId { get; set; }

    public IList<SpecificAssetIdsData>? SpecificAssetIds { get; init; } = [];

    public DefaultThumbnailData? DefaultThumbnail { get; set; }
}

public class DefaultThumbnailData
{
    public string? Path { get; set; }

    public string? ContentType { get; set; }
}
