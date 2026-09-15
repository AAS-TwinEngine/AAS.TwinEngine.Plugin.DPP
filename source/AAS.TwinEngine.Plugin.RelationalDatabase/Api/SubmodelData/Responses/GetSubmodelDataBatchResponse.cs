using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;

public sealed record GetSubmodelDataBatchResponse(
    [property: JsonPropertyName("SubmodelId")] string SubmodelId,
    [property: JsonPropertyName("Result")] JsonObject Result);
