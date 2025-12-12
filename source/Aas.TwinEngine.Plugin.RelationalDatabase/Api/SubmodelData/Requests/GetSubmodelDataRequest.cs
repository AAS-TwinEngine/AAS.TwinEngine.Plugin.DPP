using Json.Schema;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

public record GetSubmodelDataRequest(string submodelId, JsonSchema dataQuery);
