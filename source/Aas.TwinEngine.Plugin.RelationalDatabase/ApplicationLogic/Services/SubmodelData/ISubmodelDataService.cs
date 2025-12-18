using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISubmodelDataService
{
    Task<SemanticTreeNode> GetValuesBySemanticIds(SemanticTreeNode semanticIds, string submodelId, CancellationToken cancellationToken);
}
