using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseLeafNodeProcessor(IResponseSemanticTreeNodeResolver responseSemanticTreeNodeResolver) : IResponseLeafNodeProcessor
{
    public void FillLeafNode(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestLeaf);

        var leafColumnCandidates = GetLeafColumnCandidates(requestLeaf.SemanticId, columnMapping);

        foreach (var columnName in leafColumnCandidates)
        {
            var matchingLeaf = responseSemanticTreeNodeResolver
                .FindMatchingLeafNodes(responseTree, columnName)
                .FirstOrDefault();

            if (matchingLeaf is not null)
            {
                requestLeaf.Value = matchingLeaf.Value ?? string.Empty;
                return;
            }
        }

        requestLeaf.Value = string.Empty;
    }

    private IEnumerable<string> GetLeafColumnCandidates(string semanticId, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        return responseSemanticTreeNodeResolver.GetColumnMapping(semanticId, columnMapping)
            .Select(mapping => mapping.LeafColumn)
            .Where(column => !string.IsNullOrEmpty(column))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
