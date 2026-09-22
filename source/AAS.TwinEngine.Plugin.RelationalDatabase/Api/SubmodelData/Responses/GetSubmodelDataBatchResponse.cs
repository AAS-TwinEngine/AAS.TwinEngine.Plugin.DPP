using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;

public sealed record GetSubmodelDataBatchResponse(
    [property: JsonPropertyName("submodelId")] string SubmodelId,
    [property: JsonPropertyName("result")] JsonObject Result);
