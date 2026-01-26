using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public interface IResponseBranchNodeProcessor
{
    void FillBranchNode(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping);
}
