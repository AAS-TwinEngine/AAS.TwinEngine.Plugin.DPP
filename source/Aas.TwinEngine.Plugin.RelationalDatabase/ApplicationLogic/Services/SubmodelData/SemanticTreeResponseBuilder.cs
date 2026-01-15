using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SemanticTreeResponseBuilder(IOptions<Semantics> semanticsOptions, IResponseLeafNodeProcessor responseLeafNodeProcessor, IResponseBranchNodeProcessor responseBranchNodeProcessor) : ISemanticTreeResponseBuilder
{
    private readonly string _indexPrefix = semanticsOptions.Value.IndexContextPrefix;

    public SemanticTreeNode BuildResponse(SemanticTreeNode requestNode, SemanticTreeNode? responseNode, Dictionary<string, string> semanticIdToColumnMapping)
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

    private void FillRequestNodeFromResponse(SemanticTreeNode requestNode, SemanticTreeNode responseNode, Dictionary<string, string> columnMapping)
    {
        switch (requestNode)
        {
            case SemanticLeafNode leafNode:
                responseLeafNodeProcessor.FillLeafNode(leafNode, responseNode, columnMapping);
                break;

            case SemanticBranchNode branchNode:
                responseBranchNodeProcessor.FillBranchNode(branchNode, responseNode, columnMapping);
                break;
        }
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
}
