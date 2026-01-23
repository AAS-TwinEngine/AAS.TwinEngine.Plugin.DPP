using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISubmodelDataService
{
    Task<SemanticTreeNode> GetValuesBySemanticIds(JsonSchema jsonSchema, string submodelId, CancellationToken cancellationToken);
}
