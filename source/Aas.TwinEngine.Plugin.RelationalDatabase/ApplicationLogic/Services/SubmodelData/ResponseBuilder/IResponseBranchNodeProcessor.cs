using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public interface IResponseBranchNodeProcessor
{
    void FillBranchNode(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping);
}
