using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public interface IResponseLeafNodeProcessor
{
    void FillLeafNode(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping);
}
