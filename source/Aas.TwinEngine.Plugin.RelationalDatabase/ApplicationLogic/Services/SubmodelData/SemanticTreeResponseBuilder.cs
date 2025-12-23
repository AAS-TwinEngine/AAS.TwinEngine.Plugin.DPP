using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SemanticTreeResponseBuilder(IOptions<Semantics> semanticsOptions) : ISemanticTreeResponseBuilder
{
    private const string TemporaryBranchId = "__INDEX_CONTAINER__";
    private readonly string _indexPrefix = semanticsOptions.Value.IndexContextPrefix;
    private Dictionary<string, string> _columnMapping = [];

    public SemanticTreeNode BuildResponse(SemanticTreeNode requestNode, SemanticTreeNode responseNode, Dictionary<string, string> semanticIdToColumnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestNode);

        _columnMapping = semanticIdToColumnMapping;

        var mappedTreeNode = responseNode == null
            ? requestNode
            : MapTreeNode(requestNode, responseNode);

        RemoveIndexPrefix(mappedTreeNode);

        return mappedTreeNode;
    }

    private void RemoveIndexPrefix(SemanticTreeNode treeNode)
    {
        var prefixIndex = treeNode.SemanticId.IndexOf(_indexPrefix, StringComparison.Ordinal);
        if (prefixIndex >= 0)
        {
            treeNode.SemanticId = treeNode.SemanticId[..prefixIndex];
        }

        if (treeNode is SemanticBranchNode branchNode)
        {
            foreach (var child in branchNode.Children)
            {
                RemoveIndexPrefix(child);
            }
        }
    }

    private SemanticTreeNode MapTreeNode(SemanticTreeNode request, SemanticTreeNode response)
    {
        var baseId = StripIndexPrefix(request.SemanticId);
        _ = _columnMapping.TryGetValue(baseId, out var columnName);

        return request switch
        {
            SemanticLeafNode leafNode => MapLeafNode(leafNode, response, columnName),
            SemanticBranchNode branchNode => MapBranchNode(branchNode, response, columnName),
            _ => request
        };
    }

    private static SemanticLeafNode MapLeafNode(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, string? columnName)
    {
        var value = string.Empty;

        if (!string.IsNullOrEmpty(columnName))
        {
            var matchingLeaf = FindMatchingLeafNodes(responseTree, columnName)
                .OfType<SemanticLeafNode>()
                .FirstOrDefault();

            value = matchingLeaf?.Value ?? string.Empty;
        }

        return new SemanticLeafNode(requestLeaf.SemanticId, requestLeaf.DataType, value);
    }

    private SemanticTreeNode MapBranchNode(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return MapBranchNodeWithoutColumn(requestBranch, responseTree);
        }

        var matchingBranches = FindMatchingBranchNodes(responseTree, columnName);

        return matchingBranches.Count switch
        {
            0 => CreateEmptyBranchNode(requestBranch),
            1 => MapSingleBranchNode(requestBranch, matchingBranches[0]),
            _ => MapMultipleBranchNodes(requestBranch, matchingBranches)
        };
    }

    private SemanticBranchNode MapBranchNodeWithoutColumn(SemanticBranchNode requestBranch, SemanticTreeNode responseTree)
    {
        var result = new SemanticBranchNode(requestBranch.SemanticId, requestBranch.DataType);

        foreach (var child in requestBranch.Children)
        {
            AddNormalizedChild(result, MapTreeNode(child, responseTree));
        }

        return result;
    }

    private SemanticBranchNode MapSingleBranchNode(SemanticBranchNode requestBranch, SemanticBranchNode responseBranch)
    {
        var result = new SemanticBranchNode(requestBranch.SemanticId, requestBranch.DataType);

        foreach (var child in requestBranch.Children)
        {
            AddNormalizedChild(result, MapTreeNode(child, responseBranch));
        }

        return result;
    }

    private SemanticBranchNode MapMultipleBranchNodes(SemanticBranchNode requestBranch, List<SemanticBranchNode> responseBranches)
    {
        var temporaryBranchNode = new SemanticBranchNode(TemporaryBranchId, requestBranch.DataType);

        for (var i = 0; i < responseBranches.Count; i++)
        {
            var indexedBranch = CreateIndexedBranch(requestBranch, i);

            foreach (var child in requestBranch.Children)
            {
                AddNormalizedChild(indexedBranch, MapTreeNode(child, responseBranches[i]));
            }

            temporaryBranchNode.AddChild(indexedBranch);
        }

        return temporaryBranchNode;
    }

    private SemanticBranchNode CreateIndexedBranch(SemanticBranchNode source, int index)
    {
        var indexedId = $"{source.SemanticId}{_indexPrefix}{index:00}";
        return new SemanticBranchNode(indexedId, source.DataType);
    }

    private static void AddNormalizedChild(SemanticBranchNode parent, SemanticTreeNode child)
    {
        if (child is SemanticBranchNode { SemanticId: TemporaryBranchId } container)
        {
            foreach (var nestedChild in container.Children)
            {
                parent.AddChild(nestedChild);
            }
        }
        else
        {
            parent.AddChild(child);
        }
    }

    private List<SemanticBranchNode> FindMatchingBranchNodes(SemanticTreeNode root, string columnName)
    {
        var matches = new List<SemanticBranchNode>();

        if (root is SemanticBranchNode branchNode)
        {
            var branchId = StripIndexPrefix(branchNode.SemanticId);
            if (branchId.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(branchNode);
            }

            foreach (var child in branchNode.Children)
            {
                matches.AddRange(FindMatchingBranchNodes(child, columnName));
            }
        }

        return matches;
    }

    private string StripIndexPrefix(string semanticId)
    {
        var prefixIndex = semanticId.IndexOf(_indexPrefix, StringComparison.OrdinalIgnoreCase);
        return prefixIndex >= 0 ? semanticId[..prefixIndex] : semanticId;
    }

    private static SemanticTreeNode CreateEmptyBranchNode(SemanticTreeNode source)
    {
        if (source is SemanticLeafNode leafNode)
        {
            return new SemanticLeafNode(leafNode.SemanticId, leafNode.DataType, string.Empty);
        }

        var branchNode = (SemanticBranchNode)source;
        var emptyBranch = new SemanticBranchNode(branchNode.SemanticId, branchNode.DataType);

        foreach (var child in branchNode.Children)
        {
            emptyBranch.AddChild(CreateEmptyBranchNode(child));
        }

        return emptyBranch;
    }

    private static List<SemanticTreeNode> FindMatchingLeafNodes(SemanticTreeNode root, string semanticId)
    {
        var matches = new List<SemanticTreeNode>();

        if (root.SemanticId.Equals(semanticId, StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(root);
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
}
