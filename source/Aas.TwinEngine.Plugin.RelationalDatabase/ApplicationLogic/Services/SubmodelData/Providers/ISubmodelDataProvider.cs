using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;

public interface ISubmodelDataProvider
{
    Task<SemanticTreeNode> GetValuesBySemanticIds(string sqlQuery, SemanticTreeNode semanticIds, string submodelId);
}
