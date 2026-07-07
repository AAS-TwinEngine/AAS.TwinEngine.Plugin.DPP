using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseLeafNodeProcessor(IResponseSemanticTreeNodeResolver responseSemanticTreeNodeResolver) : IResponseLeafNodeProcessor
{
    public void FillLeafNode(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, Dictionary<string, ColumnMapping> columnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestLeaf);
        var columnName = responseSemanticTreeNodeResolver.GetColumnMapping(requestLeaf.SemanticId, columnMapping)?.LeafColumn;

        if (string.IsNullOrEmpty(columnName))
        {
            requestLeaf.Value = string.Empty;
            return;
        }

        var matchingLeaf = responseSemanticTreeNodeResolver
            .FindMatchingLeafNodes(responseTree, columnName)
            .FirstOrDefault();

        requestLeaf.Value = matchingLeaf?.Value ?? string.Empty;
    }
}
