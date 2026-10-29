using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;

public sealed class CreateProductResponse
{
    [JsonPropertyName("productId")]
    public required string ProductId { get; init; }

    [JsonPropertyName("idShort")]
    public required string IdShort { get; init; }

    [JsonPropertyName("aasId")]
    public required string AasId { get; init; }

    [JsonPropertyName("globalAssetId")]
    public required string GlobalAssetId { get; init; }
}
