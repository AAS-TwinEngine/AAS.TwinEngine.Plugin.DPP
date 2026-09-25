using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseLeafNodeProcessor(IResponseSemanticTreeNodeResolver responseSemanticTreeNodeResolver) : IResponseLeafNodeProcessor
{
    public void FillLeafNode(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestLeaf);

        foreach (var columnName in GetLeafColumnCandidates(requestLeaf.SemanticId, columnMapping))
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

    // A semanticId can map to several tables (e.g. shared IDTA properties reused across
    // MaintenanceTool/Consumable/SparePart); try each known column name and keep whichever
    // one actually exists in this branch of the response tree.
    private IEnumerable<string> GetLeafColumnCandidates(string semanticId, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        return responseSemanticTreeNodeResolver.GetColumnMapping(semanticId, columnMapping)
            .Select(mapping => mapping.LeafColumn)
            .Where(column => !string.IsNullOrEmpty(column))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
