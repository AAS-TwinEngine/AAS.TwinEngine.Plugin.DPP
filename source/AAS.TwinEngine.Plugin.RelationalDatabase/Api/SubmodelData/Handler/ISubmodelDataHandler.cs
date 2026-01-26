using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public interface ISubmodelDataHandler
{
    Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken);
}
