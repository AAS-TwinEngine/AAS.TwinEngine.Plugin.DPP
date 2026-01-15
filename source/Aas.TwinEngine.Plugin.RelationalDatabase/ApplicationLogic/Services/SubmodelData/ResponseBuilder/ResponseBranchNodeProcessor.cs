using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseBranchNodeProcessor(IResponseSemanticTreeNodeResolver responseSemanticTreeNodeResolver, IResponseLeafNodeProcessor responseLeafNodeProcessor) : IResponseBranchNodeProcessor
{
    public void FillBranchNode(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping)
    {
        ArgumentNullException.ThrowIfNull(requestBranch);

        var columnName = responseSemanticTreeNodeResolver.GetColumnName(requestBranch.SemanticId, columnMapping);

        if (string.IsNullOrEmpty(columnName))
        {
            FillBranchNodeWithoutColumn(requestBranch, responseTree, columnMapping);
            return;
        }

        var matchingBranches = responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, columnName);
        ProcessBranchBasedOnMatchCount(requestBranch, matchingBranches, columnMapping);
    }

    #region No Column Strategy

    private void FillBranchNodeWithoutColumn(SemanticBranchNode requestBranch, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping)
    {
        var newChildren = requestBranch.Children
            .SelectMany(child => ProcessSingleChildWithoutColumn(child, responseTree, columnMapping))
            .ToList();

        requestBranch.ReplaceChildren(newChildren);
    }

    private List<SemanticTreeNode> ProcessSingleChildWithoutColumn(SemanticTreeNode child, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping)
    {
        var childColumnName = responseSemanticTreeNodeResolver.GetColumnName(child.SemanticId, columnMapping);

        if (NeedsCloning(child, childColumnName, responseTree, out var matchingBranches))
        {
            return ExpandChildIntoMultipleBranches((SemanticBranchNode)child, matchingBranches!, columnMapping);
        }

        FillChildNode(child, responseTree, columnMapping);
        return [child];
    }

    private bool NeedsCloning(SemanticTreeNode child, string? columnName, SemanticTreeNode responseTree, out IList<SemanticBranchNode>? matchingBranches)
    {
        matchingBranches = null;

        if (child is not SemanticBranchNode || string.IsNullOrEmpty(columnName))
        {
            return false;
        }

        matchingBranches = responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, columnName);
        return matchingBranches.Count > 1;
    }

    private List<SemanticTreeNode> ExpandChildIntoMultipleBranches(SemanticBranchNode childBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
    {
        return [.. matchingBranches
            .Select((responseBranch, index) =>
                CreateIndexedAndPopulatedBranch(childBranch, responseBranch, index, columnMapping))
            .Cast<SemanticTreeNode>()];
    }

    #endregion

    #region Match Count Processing

    private void ProcessBranchBasedOnMatchCount(SemanticBranchNode requestBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
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

    private void FillSingleBranchMatch(
        SemanticBranchNode requestBranch,
        SemanticBranchNode responseBranch,
        Dictionary<string, string> columnMapping)
    {
        foreach (var child in requestBranch.Children)
        {
            FillChildNode(child, responseBranch, columnMapping);
        }
    }

    #endregion

    #region Multiple Matches Strategy

    private void FillMultipleBranchMatches(SemanticBranchNode requestBranch, IList<SemanticBranchNode> responseBranches, Dictionary<string, string> columnMapping)
    {
        var newChildren = responseBranches
            .Select((responseBranch, index) =>
                CreateIndexedAndPopulatedBranch(requestBranch, responseBranch, index, columnMapping))
            .Cast<SemanticTreeNode>()
            .ToList();

        requestBranch.ReplaceChildren(newChildren);
    }

    private SemanticBranchNode CreateIndexedAndPopulatedBranch(SemanticBranchNode sourceBranch, SemanticBranchNode responseBranch, int index, Dictionary<string, string> columnMapping)
    {
        var clonedChild = CloneBranchNode(sourceBranch);
        PopulateBranchNodeContent(clonedChild, responseBranch, columnMapping);
        clonedChild.SemanticId = responseSemanticTreeNodeResolver.CreateIndexedSemanticId(sourceBranch.SemanticId, index);
        return clonedChild;
    }

    private void PopulateBranchNodeContent(SemanticBranchNode branchNode, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        var newChildren = branchNode.Children
            .SelectMany(child => ProcessChildForBranchContent(child, responseBranch, columnMapping))
            .ToList();

        branchNode.ReplaceChildren(newChildren);
    }

    private List<SemanticTreeNode> ProcessChildForBranchContent(SemanticTreeNode child, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        return child switch
        {
            SemanticLeafNode leafNode => ProcessLeafInBranch(leafNode, responseBranch, columnMapping),
            SemanticBranchNode childBranch => ProcessBranchInBranch(childBranch, responseBranch, columnMapping),
            _ => [child]
        };
    }

    private List<SemanticTreeNode> ProcessLeafInBranch(SemanticLeafNode leafNode, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        responseLeafNodeProcessor.FillLeafNode(leafNode, responseBranch, columnMapping);
        return [leafNode];
    }

    private List<SemanticTreeNode> ProcessBranchInBranch(SemanticBranchNode childBranch, SemanticBranchNode responseBranch, Dictionary<string, string> columnMapping)
    {
        var columnName = responseSemanticTreeNodeResolver.GetColumnName(childBranch.SemanticId, columnMapping);

        if (string.IsNullOrEmpty(columnName))
        {
            FillBranchNodeWithoutColumn(childBranch, responseBranch, columnMapping);
            return [childBranch];
        }

        var matchingBranches = responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseBranch, columnName);
        return ProcessNestedBranchBasedOnMatchCount(childBranch, matchingBranches, columnMapping);
    }

    private List<SemanticTreeNode> ProcessNestedBranchBasedOnMatchCount(SemanticBranchNode childBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
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
        FillSingleBranchMatch(childBranch, matchingBranch, columnMapping);
        return [childBranch];
    }

    private List<SemanticTreeNode> HandleMultipleMatches(SemanticBranchNode childBranch, IList<SemanticBranchNode> matchingBranches, Dictionary<string, string> columnMapping)
    {
        return [.. matchingBranches
            .Select((match, index) => CreateIndexedAndPopulatedBranch(childBranch, match, index, columnMapping))
            .Cast<SemanticTreeNode>()];
    }

    #endregion

    #region Helper Methods

    private void FillChildNode(SemanticTreeNode child, SemanticTreeNode responseTree, Dictionary<string, string> columnMapping)
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
