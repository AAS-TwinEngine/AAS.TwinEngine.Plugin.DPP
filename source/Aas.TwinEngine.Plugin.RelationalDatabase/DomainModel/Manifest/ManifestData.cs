using System.Text.Json.Serialization;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

public class ManifestData
{
    [JsonPropertyName("supportedSemanticIds")]
    public required IList<string> SupportedSemanticIds { get; init; }

    [JsonPropertyName("capabilities")]
    public required CapabilitiesData Capabilities { get; set; }
}

public class CapabilitiesData
{
    [JsonPropertyName("hasShellDescriptor")]
    public bool HasShellDescriptor { get; set; }

    [JsonPropertyName("hasAssetInformation")]
    public bool HasAssetInformation { get; set; }
}
