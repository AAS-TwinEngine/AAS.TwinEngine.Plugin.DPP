using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public interface IResponseSemanticTreeNodeResolver
{
    ColumnMapping? GetColumnMapping(string semanticId, Dictionary<string, ColumnMapping> columnMapping);

    IList<SemanticLeafNode> FindMatchingLeafNodes(SemanticTreeNode root, string semanticId);

    IList<SemanticBranchNode> FindMatchingBranchNodes(SemanticTreeNode root, string columnName);

    string CreateIndexedSemanticId(string baseId, int index);
}
