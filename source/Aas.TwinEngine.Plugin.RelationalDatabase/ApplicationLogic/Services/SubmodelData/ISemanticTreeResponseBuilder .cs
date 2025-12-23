using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISemanticTreeResponseBuilder
{
    SemanticTreeNode BuildResponse(SemanticTreeNode requestNode, SemanticTreeNode responseNode, Dictionary<string, string> semanticIdToColumnMapping);
}
