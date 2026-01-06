using System.Text.Json.Serialization;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Responses;

public class ShellDescriptorDto
{
    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("specificAssetIds")]
    public IList<SpecificAssetIdsDto>? SpecificAssetIds { get; init; }
}

public class SpecificAssetIdsDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

