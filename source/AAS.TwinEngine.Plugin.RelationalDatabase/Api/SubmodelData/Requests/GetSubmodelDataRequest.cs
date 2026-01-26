using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

public record GetSubmodelDataRequest(string submodelId, JsonSchema dataQuery);
