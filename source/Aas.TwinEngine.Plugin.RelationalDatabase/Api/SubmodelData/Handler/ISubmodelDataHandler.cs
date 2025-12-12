using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public interface ISubmodelDataHandler
{
    Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken);
}
