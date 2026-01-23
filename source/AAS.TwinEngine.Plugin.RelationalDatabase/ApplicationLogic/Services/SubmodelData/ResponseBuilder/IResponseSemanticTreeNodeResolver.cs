using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public interface IResponseSemanticTreeNodeResolver
{
    string? GetColumnName(string semanticId, Dictionary<string, string> columnMapping);

    IList<SemanticLeafNode> FindMatchingLeafNodes(SemanticTreeNode root, string semanticId);

    IList<SemanticBranchNode> FindMatchingBranchNodes(SemanticTreeNode root, string columnName);

    string CreateIndexedSemanticId(string baseId, int index);
}
