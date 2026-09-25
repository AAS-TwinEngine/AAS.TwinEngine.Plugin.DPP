using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseBranchNodeProcessor(IResponseSemanticTreeNodeResolver responseSemanticTreeNodeResolver, IResponseLeafNodeProcessor responseLeafNodeProcessor) : IResponseBranchNodeProcessor
{
    public void FillBranchNode(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestBranch);

        var matchingBranches = ResolveMatchingBranches(requestBranch.SemanticId, responseTree, columnMapping, out var hasBranchColumn);

        if (!hasBranchColumn)
        {
            FillBranchNodeWithoutColumn(requestBranch, responseTree, columnMapping);
            return;
        }

        ProcessBranchBasedOnMatchCount(requestBranch, matchingBranches, columnMapping);
    }

    #region No Column Strategy

    private void FillBranchNodeWithoutColumn(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        var newChildren = requestBranch.Children
            .SelectMany(child => ProcessSingleChildWithoutColumn(child, responseTree, columnMapping))
            .ToList();

        requestBranch.ReplaceChildren(newChildren);
    }

    private List<SemanticTreeNode> ProcessSingleChildWithoutColumn(SemanticTreeNode child, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        if (NeedsCloning(child, responseTree, columnMapping, out var matchingBranches))
        {
            return ExpandChildIntoMultipleBranches((SemanticBranchNode)child, matchingBranches!, columnMapping);
        }

        FillChildNode(child, responseTree, columnMapping);
        return [child];
    }

    private bool NeedsCloning(SemanticTreeNode child, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping, out IList<SemanticBranchNode>? matchingBranches)
    {
        matchingBranches = null;

        if (child is not SemanticBranchNode)
        {
            return false;
        }

        var matches = ResolveMatchingBranches(child.SemanticId, responseTree, columnMapping, out var hasBranchColumn);

        if (!hasBranchColumn)
        {
            return false;
        }

        matchingBranches = matches;
        return matches.Count > 1;
    }

    private List<SemanticTreeNode> ExpandChildIntoMultipleBranches(SemanticBranchNode childBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        return [.. matchingBranches
            .Select((responseBranch, index) =>
                CreateIndexedAndPopulatedBranch(childBranch, responseBranch, index, columnMapping))
            .Cast<SemanticTreeNode>()];
    }

    #endregion

    #region Match Count Processing

    private void ProcessBranchBasedOnMatchCount(SemanticBranchNode requestBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        switch (matchingBranches.Count)
        {
            case 0:
                SetBranchToEmpty(requestBranch);
                break;
            case 1:
                FillSingleBranchMatch(requestBranch, matchingBranches[0], columnMapping);
                break;
            default:
                FillMultipleBranchMatches(requestBranch, matchingBranches, columnMapping);
                break;
        }
    }

    #endregion

    #region No Match Strategy

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

    #endregion

    #region Single Match Strategy

    private void FillSingleBranchMatch(SemanticBranchNode requestBranch, SemanticBranchNode responseBranch, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        foreach (var child in requestBranch.Children)
        {
            FillChildNode(child, responseBranch, columnMapping);
        }
    }

    #endregion

    #region Multiple Matches Strategy

    private void FillMultipleBranchMatches(SemanticBranchNode requestBranch, IList<SemanticBranchNode> responseBranches, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        var newChildren = responseBranches
            .Select((responseBranch, index) =>
                CreateIndexedAndPopulatedBranch(requestBranch, responseBranch, index, columnMapping))
            .Cast<SemanticTreeNode>()
            .ToList();

        requestBranch.ReplaceChildren(newChildren);
    }

    private SemanticBranchNode CreateIndexedAndPopulatedBranch(SemanticBranchNode sourceBranch, SemanticBranchNode responseBranch, int index, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        var clonedChild = CloneBranchNode(sourceBranch);
        PopulateBranchNodeContent(clonedChild, responseBranch, columnMapping);
        clonedChild.SemanticId = responseSemanticTreeNodeResolver.CreateIndexedSemanticId(sourceBranch.SemanticId, index);
        return clonedChild;
    }

    private void PopulateBranchNodeContent(SemanticBranchNode branchNode, SemanticBranchNode responseBranch, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        var newChildren = branchNode.Children
            .SelectMany(child => ProcessChildForBranchContent(child, responseBranch, columnMapping))
            .ToList();

        branchNode.ReplaceChildren(newChildren);
    }

    private List<SemanticTreeNode> ProcessChildForBranchContent(SemanticTreeNode child, SemanticBranchNode responseBranch, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        return child switch
        {
            SemanticLeafNode leafNode => ProcessLeafInBranch(leafNode, responseBranch, columnMapping),
            SemanticBranchNode childBranch => ProcessBranchInBranch(childBranch, responseBranch, columnMapping),
            _ => [child]
        };
    }

    private List<SemanticTreeNode> ProcessLeafInBranch(SemanticLeafNode leafNode, SemanticBranchNode responseBranch, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        responseLeafNodeProcessor.FillLeafNode(leafNode, responseBranch, columnMapping);
        return [leafNode];
    }

    private List<SemanticTreeNode> ProcessBranchInBranch(SemanticBranchNode childBranch, SemanticBranchNode responseBranch, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        var matchingBranches = ResolveMatchingBranches(childBranch.SemanticId, responseBranch, columnMapping, out var hasBranchColumn);

        if (!hasBranchColumn)
        {
            FillBranchNodeWithoutColumn(childBranch, responseBranch, columnMapping);
            return [childBranch];
        }

        return ProcessNestedBranchBasedOnMatchCount(childBranch, matchingBranches, columnMapping);
    }

    private List<SemanticTreeNode> ProcessNestedBranchBasedOnMatchCount(SemanticBranchNode childBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, List<ColumnMapping>> columnMapping)
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

    private List<SemanticTreeNode> HandleSingleMatch(SemanticBranchNode childBranch, SemanticBranchNode matchingBranch, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        FillSingleBranchMatch(childBranch, matchingBranch, columnMapping);
        return [childBranch];
    }

    private List<SemanticTreeNode> HandleMultipleMatches(SemanticBranchNode childBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        return [.. matchingBranches
            .Select((match, index) => CreateIndexedAndPopulatedBranch(childBranch, match, index, columnMapping))
            .Cast<SemanticTreeNode>()];
    }

    #endregion

    #region Helper Methods

    private IList<SemanticBranchNode> ResolveMatchingBranches(string semanticId, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping, out bool hasBranchColumn)
    {
        var candidates = responseSemanticTreeNodeResolver.GetColumnMapping(semanticId, columnMapping)
            .Select(mapping => mapping.BranchColumn)
            .Where(column => !string.IsNullOrEmpty(column))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        hasBranchColumn = candidates.Count > 0;

        foreach (var candidate in candidates)
        {
            var matches = responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, candidate);
            if (matches.Count > 0)
            {
                return matches;
            }
        }

        return [];
    }

    private void FillChildNode(SemanticTreeNode child, SemanticTreeNode responseTree, Dictionary<string, List<ColumnMapping>> columnMapping)
    {
        switch (child)
        {
            case SemanticLeafNode leafNode:
                responseLeafNodeProcessor.FillLeafNode(leafNode, responseTree, columnMapping);
                break;
            case SemanticBranchNode branchNode:
                FillBranchNode(branchNode, responseTree, columnMapping);
                break;
        }
    }

    private SemanticBranchNode CloneBranchNode(SemanticBranchNode source)
    {
        var cloned = new SemanticBranchNode(source.SemanticId, source.DataType);

        var clonedChildren = source.Children
            .Select(CloneNode)
            .ToList();

        cloned.ReplaceChildren(clonedChildren);

        return cloned;
    }

    private SemanticTreeNode CloneNode(SemanticTreeNode node)
    {
        return node switch
        {
            SemanticLeafNode leafNode => new SemanticLeafNode(leafNode.SemanticId, leafNode.DataType, leafNode.Value),
            SemanticBranchNode branchNode => CloneBranchNode(branchNode),
            _ => node
        };
    }

    #endregion
}
