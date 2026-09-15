using System.Text.Json.Serialization;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

public sealed record GetSubmodelDataBatchRequest(
    [property: JsonPropertyName("SubmodelIds")] IReadOnlyCollection<string> SubmodelIds,
    [property: JsonPropertyName("Schema")] JsonSchema Schema);
