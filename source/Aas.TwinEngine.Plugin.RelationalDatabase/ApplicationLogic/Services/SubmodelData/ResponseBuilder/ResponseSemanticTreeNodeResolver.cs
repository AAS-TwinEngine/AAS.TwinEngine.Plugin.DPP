using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseSemanticTreeNodeResolver(IOptions<Semantics> semanticsOptions) : IResponseSemanticTreeNodeResolver
{
    private readonly string _indexPrefix = semanticsOptions.Value.IndexContextPrefix;

    public string? GetColumnName(string semanticId, Dictionary<string, string> columnMapping)
    {
        ArgumentNullException.ThrowIfNull(columnMapping);

        if (string.IsNullOrEmpty(semanticId))
        {
            return null;
        }

        _ = columnMapping.TryGetValue(semanticId, out var columnName);

        return columnName;
    }

    public IList<SemanticLeafNode> FindMatchingLeafNodes(SemanticTreeNode root, string semanticId)
    {
        var matches = new List<SemanticLeafNode>();

        if (root.SemanticId.Equals(semanticId, StringComparison.OrdinalIgnoreCase) &&
            root is SemanticLeafNode leafNode)
        {
            matches.Add(leafNode);
        }

        if (root is SemanticBranchNode branchNode)
        {
            foreach (var child in branchNode.Children)
            {
                matches.AddRange(FindMatchingLeafNodes(child, semanticId));
            }
        }

        return matches;
    }

    public IList<SemanticBranchNode> FindMatchingBranchNodes(SemanticTreeNode root, string columnName)
    {
        var matches = new List<SemanticBranchNode>();

        if (root is not SemanticBranchNode branchNode)
        {
            return matches;
        }

        if (IsBranchMatchingColumnName(branchNode, columnName))
        {
            matches.Add(branchNode);
        }

        foreach (var child in branchNode.Children)
        {
            matches.AddRange(FindMatchingBranchNodes(child, columnName));
        }

        return matches;
    }

    private bool IsBranchMatchingColumnName(SemanticBranchNode branchNode, string columnName)
    {
        var branchId = columnName.Contains(_indexPrefix, StringComparison.OrdinalIgnoreCase)
            ? branchNode.SemanticId
            : StripIndexPrefix(branchNode.SemanticId);

        return branchId.Equals(columnName, StringComparison.OrdinalIgnoreCase);
    }

    public string StripIndexPrefix(string semanticId)
    {
        var prefixIndex = semanticId.IndexOf(_indexPrefix, StringComparison.OrdinalIgnoreCase);
        return prefixIndex >= 0 ? semanticId[..prefixIndex] : semanticId;
    }

    public string CreateIndexedSemanticId(string baseId, int index) => $"{baseId}{_indexPrefix}{index}";
}
