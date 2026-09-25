using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseLeafNodeProcessor(IResponseSemanticTreeNodeResolver responseSemanticTreeNodeResolver) : IResponseLeafNodeProcessor
{
    public void FillLeafNode(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, Dictionary<string, ColumnMapping> columnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestLeaf);
        var mapping = responseSemanticTreeNodeResolver.GetColumnMapping(requestLeaf.SemanticId, columnMapping);

        foreach (var columnName in GetLeafColumnCandidates(mapping))
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

    private static IEnumerable<string> GetLeafColumnCandidates(ColumnMapping? mapping)
    {
        if (mapping is null)
        {
            yield break;
        }

        foreach (var column in new[] { mapping.LeafColumn }
            .Concat(mapping.AlternateLeafColumns ?? [])
            .Where(x => !string.IsNullOrEmpty(x)))
        {
            yield return column;
        }
    }
}
