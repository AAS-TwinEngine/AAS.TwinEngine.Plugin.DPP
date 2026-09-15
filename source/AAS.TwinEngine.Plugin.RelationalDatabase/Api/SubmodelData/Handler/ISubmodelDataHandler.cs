using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public interface ISubmodelDataHandler
{
    Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<GetSubmodelDataBatchResponse>> GetSubmodelDataAsync(
        IReadOnlyCollection<GetSubmodelDataBatchRequest> requests,
        CancellationToken cancellationToken);
}
