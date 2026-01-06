using System.Data;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SemanticTreeResponseBuilder(IOptions<Semantics> semanticsOptions) : ISemanticTreeResponseBuilder
{
    private readonly string _indexPrefix = semanticsOptions.Value.IndexContextPrefix;

    public SemanticTreeNode BuildResponse(
        SemanticTreeNode requestNode,
        SemanticTreeNode? responseNode,
        Dictionary<string, string> semanticIdToColumnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestNode);
        ArgumentNullException.ThrowIfNull(semanticIdToColumnMapping);
        if (responseNode is not null)
        {
            FillRequestNodeFromResponse(requestNode, responseNode, semanticIdToColumnMapping);
        }

        RemoveIndexPrefixFromTree(requestNode);
        return requestNode;
    }

    private void RemoveIndexPrefixFromTree(SemanticTreeNode treeNode)
    {
        treeNode.SemanticId = StripIndexPrefixFromId(treeNode.SemanticId);

        if (treeNode is not SemanticBranchNode branchNode)
        {
            return;
        }

        foreach (var child in branchNode.Children)
        {
            RemoveIndexPrefixFromTree(child);
        }
    }

    private string StripIndexPrefixFromId(string semanticId)
    {
        var prefixIndex = semanticId.IndexOf(_indexPrefix, StringComparison.Ordinal);
        return prefixIndex >= 0 ? semanticId[..prefixIndex] : semanticId;
    }

    private void FillRequestNodeFromResponse(SemanticTreeNode requestNode, SemanticTreeNode responseNode, Dictionary<string, string> columnMapping)
    {
        var columnName = GetColumnName(requestNode.SemanticId, columnMapping);

        switch (requestNode)
        {
            case SemanticLeafNode leafNode:
                FillLeafNodeFromResponse(leafNode, responseNode, columnName);
                break;
            case SemanticBranchNode branchNode:
                FillBranchNodeFromResponse(branchNode, responseNode, columnName, columnMapping);
                break;
        }
    }

    private static string? GetColumnName(string semanticId, Dictionary<string, string> columnMapping)
    {
        _ = columnMapping.TryGetValue(semanticId, out var columnName);
        return columnName;
    }

    private static void FillLeafNodeFromResponse(SemanticLeafNode requestLeaf, SemanticTreeNode responseTree, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            requestLeaf.Value = string.Empty;
            return;
        }

        var matchingLeaf = FindMatchingLeafNodes(responseTree, columnName)
            .OfType<SemanticLeafNode>()
            .FirstOrDefault();

        requestLeaf.Value = matchingLeaf?.Value ?? string.Empty;
    }

    private void FillBranchNodeFromResponse(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, string? columnName, Dictionary<string, string> columnMapping)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            FillBranchNodeWithoutColumn(requestBranch, responseTree, columnMapping);
            return;
        }

        var matchingBranches = FindMatchingBranchNodes(responseTree, columnName);
        ProcessBranchBasedOnMatchCount(requestBranch, matchingBranches, columnMapping);
    }

    private void ProcessBranchBasedOnMatchCount(SemanticBranchNode requestBranch, List<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
    {
        switch (matchingBranches.Count)
        {
            case 0:
                SetBranchToEmpty(requestBranch);
                break;
            case 1:
                FillSingleBranchNodeFromResponse(requestBranch, matchingBranches[0], columnMapping);
                break;
            default:
                FillMultipleBranchNodes(requestBranch, matchingBranches, columnMapping);
                break;
        }
    }

    private void FillBranchNodeWithoutColumn(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping)
    {
        var childrenToProcess = requestBranch.Children.ToList();
        var newChildren = ProcessChildrenForBranchWithoutColumn(childrenToProcess, responseTree, columnMapping);
        requestBranch.ReplaceChildren(newChildren);
    }

    private List<SemanticTreeNode> ProcessChildrenForBranchWithoutColumn(List<SemanticTreeNode> children, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping)
    {
        var newChildren = new List<SemanticTreeNode>();

        foreach (var processedChildren in children.Select(child => ProcessSingleChildWithoutColumn(child, responseTree, columnMapping)))
        {
            newChildren.AddRange(processedChildren);
        }

        return newChildren;
    }

    private List<SemanticTreeNode> ProcessSingleChildWithoutColumn(SemanticTreeNode child, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping)
    {
        var childColumnName = GetColumnName(child.SemanticId, columnMapping);

        if (NeedsCloning(child, childColumnName, responseTree, out var matchingBranches))
        {
            return ExpandChildIntoMultipleBranches((SemanticBranchNode)child, matchingBranches!, columnMapping);
        }

        FillRequestNodeFromResponse(child, responseTree, columnMapping);
        return [child];
    }

    private bool NeedsCloning(SemanticTreeNode child, string? columnName, SemanticTreeNode responseTree, out List<SemanticBranchNode>? matchingBranches)
    {
        matchingBranches = null;
        if (child is not SemanticBranchNode || string.IsNullOrEmpty(columnName))
        {
            return false;
        }

        matchingBranches = FindMatchingBranchNodes(responseTree, columnName);
        return matchingBranches.Count > 1;
    }

    private List<SemanticTreeNode> ExpandChildIntoMultipleBranches(SemanticBranchNode childBranch, List<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
    {
        return matchingBranches
            .Select((responseBranch, index) => CreateIndexedAndPopulatedBranch(childBranch, responseBranch, index, columnMapping))
            .Cast<SemanticTreeNode>()
            .ToList();
    }

    private SemanticBranchNode CreateIndexedAndPopulatedBranch(SemanticBranchNode sourceBranch, SemanticBranchNode responseBranch, int index, Dictionary<string, string> columnMapping)
    {
        var clonedChild = CloneBranchNode(sourceBranch);
        PopulateBranchNodeContent(clonedChild, responseBranch, columnMapping);
        clonedChild.SemanticId = CreateIndexedSemanticId(sourceBranch.SemanticId, index);
        return clonedChild;
    }

    private string CreateIndexedSemanticId(string baseId, int index) => $"{baseId}{_indexPrefix}{index}";

    private void FillSingleBranchNodeFromResponse(SemanticBranchNode requestBranch, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        foreach (var child in requestBranch.Children)
        {
            FillRequestNodeFromResponse(child, responseBranch, columnMapping);
        }
    }

    private void FillMultipleBranchNodes(SemanticBranchNode requestBranch, List<SemanticBranchNode> responseBranches, Dictionary<string, string> columnMapping)
    {
        var newChildren = responseBranches
            .Select((responseBranch, index) => CreateIndexedAndPopulatedBranch(requestBranch, responseBranch, index, columnMapping))
            .Cast<SemanticTreeNode>()
            .ToList();

        requestBranch.ReplaceChildren(newChildren);
    }

    private void PopulateBranchNodeContent(SemanticBranchNode branchNode, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        var childrenToProcess = branchNode.Children.ToList();
        var newChildren = ProcessChildrenForBranchContent(childrenToProcess, responseBranch, columnMapping);
        branchNode.ReplaceChildren(newChildren);
    }

    private List<SemanticTreeNode> ProcessChildrenForBranchContent(List<SemanticTreeNode> children, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        var newChildren = new List<SemanticTreeNode>();

        foreach (var processedChildren in children.Select(child => ProcessChildForBranchContent(child, responseBranch, columnMapping)))
        {
            newChildren.AddRange(processedChildren);
        }

        return newChildren;
    }

    private List<SemanticTreeNode> ProcessChildForBranchContent(SemanticTreeNode child, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        return child switch
        {
            SemanticLeafNode leafNode => ProcessLeafInBranchContent(leafNode, responseBranch, columnMapping),
            SemanticBranchNode childBranch => ProcessBranchInBranchContent(childBranch, responseBranch, columnMapping),
            _ => [child]
        };
    }

    private List<SemanticTreeNode> ProcessLeafInBranchContent(SemanticLeafNode leafNode, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        var columnName = GetColumnName(leafNode.SemanticId, columnMapping);
        FillLeafNodeFromResponse(leafNode, responseBranch, columnName);
        return [leafNode];
    }

    private List<SemanticTreeNode> ProcessBranchInBranchContent(SemanticBranchNode childBranch, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        var columnName = GetColumnName(childBranch.SemanticId, columnMapping);

        if (string.IsNullOrEmpty(columnName))
        {
            FillBranchNodeWithoutColumn(childBranch, responseBranch, columnMapping);
            return [childBranch];
        }

        var matchingBranches = FindMatchingBranchNodes(responseBranch, columnName);
        return ProcessNestedBranchBasedOnMatchCount(childBranch, matchingBranches, columnMapping);
    }

    private List<SemanticTreeNode> ProcessNestedBranchBasedOnMatchCount(SemanticBranchNode childBranch, List<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
    {
        return matchingBranches.Count switch
        {
            0 => HandleNoMatches(childBranch),
            1 => HandleSingleMatch(childBranch, matchingBranches[0], columnMapping),
            _ => HandleMultipleMatches(childBranch, matchingBranches, columnMapping)
        };
    }

    private static List<SemanticTreeNode> HandleNoMatches(SemanticBranchNode childBranch)
    {
        SetBranchToEmpty(childBranch);
        return [childBranch];
    }

    private List<SemanticTreeNode> HandleSingleMatch(SemanticBranchNode childBranch, SemanticBranchNode matchingBranch, Dictionary<string, string> columnMapping)
    {
        FillSingleBranchNodeFromResponse(childBranch, matchingBranch, columnMapping);
        return [childBranch];
    }

    private List<SemanticTreeNode> HandleMultipleMatches(SemanticBranchNode childBranch, List<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
    {
        return matchingBranches
            .Select((match, index) => CreateIndexedAndPopulatedBranch(childBranch, match, index, columnMapping))
            .Cast<SemanticTreeNode>()
            .ToList();
    }

    private SemanticTreeNode CloneNode(SemanticTreeNode node)
    {
        return node switch
        {
            SemanticLeafNode leafNode => CloneLeafNode(leafNode),
            SemanticBranchNode branchNode => CloneBranchNode(branchNode),
            _ => node
        };
    }

    private static SemanticLeafNode CloneLeafNode(SemanticLeafNode source) => new(source.SemanticId, source.DataType, source.Value);

    private SemanticBranchNode CloneBranchNode(SemanticBranchNode source)
    {
        var cloned = new SemanticBranchNode(source.SemanticId, source.DataType);

        var clonedChildren = source.Children
            .Select(CloneNode)
            .ToList();

        cloned.ReplaceChildren(clonedChildren);

        return cloned;
    }

    private static void SetBranchToEmpty(SemanticBranchNode branchNode)
    {
        foreach (var child in branchNode.Children)
        {
            switch (child)
            {
                case SemanticLeafNode leafNode:
                    leafNode.Value = string.Empty;
                    break;
                case SemanticBranchNode childBranch:
                    SetBranchToEmpty(childBranch);
                    break;
            }
        }
    }

    private List<SemanticBranchNode> FindMatchingBranchNodes(SemanticTreeNode root, string columnName)
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

        var childMatches = branchNode.Children
            .SelectMany(child => FindMatchingBranchNodes(child, columnName));

        matches.AddRange(childMatches);

        return matches;
    }

    private bool IsBranchMatchingColumnName(SemanticBranchNode branchNode, string columnName)
    {
        var branchId = columnName.Contains(_indexPrefix, StringComparison.OrdinalIgnoreCase)
            ? branchNode.SemanticId
            : StripIndexPrefix(branchNode.SemanticId);

        return branchId.Equals(columnName, StringComparison.OrdinalIgnoreCase);
    }

    private string StripIndexPrefix(string semanticId)
    {
        var prefixIndex = semanticId.IndexOf(_indexPrefix, StringComparison.OrdinalIgnoreCase);
        return prefixIndex >= 0 ? semanticId[..prefixIndex] : semanticId;
    }

    private static List<SemanticTreeNode> FindMatchingLeafNodes(SemanticTreeNode root, string semanticId)
    {
        var matches = new List<SemanticTreeNode>();

        if (root.SemanticId.Equals(semanticId, StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(root);
        }

        if (root is not SemanticBranchNode branchNode)
        {
            return matches;
        }

        var childMatches = branchNode.Children
                                     .SelectMany(child => FindMatchingLeafNodes(child, semanticId));

        matches.AddRange(childMatches);

        return matches;
    }
}
