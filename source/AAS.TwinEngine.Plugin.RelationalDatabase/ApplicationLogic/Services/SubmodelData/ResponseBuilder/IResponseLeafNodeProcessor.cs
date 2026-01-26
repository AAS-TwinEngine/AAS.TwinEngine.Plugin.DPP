using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public interface IResponseLeafNodeProcessor
{
    void FillLeafNode(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping);
}
