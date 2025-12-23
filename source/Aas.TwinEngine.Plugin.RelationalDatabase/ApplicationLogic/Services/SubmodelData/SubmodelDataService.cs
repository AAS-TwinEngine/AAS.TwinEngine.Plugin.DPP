using System.Text.Json;
using System.Text.RegularExpressions;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

using Microsoft.Extensions.Options;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Sharad.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SubmodelDataService(IOptions<ExtractionRules> options,
    IOptions<Semantics> semanticsOptions,
    IQueryProvider queryProvider,
    ISubmodelDataProvider submodelDataProvider,
    ILogger<SubmodelDataService> _logger) : ISubmodelDataService
{
    private readonly IList<ProductIdExtractionRules> _productIdExtractionRules = options.Value.ProductIdExtractionRules;
    private readonly IList<SubmodelNameExtractionRules> _submodelNameExtractionRules = options.Value.SubmodelNameExtractionRules;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _indexContextPrefix = semanticsOptions.Value.IndexContextPrefix;
    private Dictionary<string, string> _semanticIdToColumnMapping = [];
    private const string IndexContainerId = "__INDEX_CONTAINER__";
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public async Task<SemanticTreeNode> GetValuesBySemanticIds(SemanticTreeNode semanticIds, string submodelId, CancellationToken cancellationToken)
    {
        var extractionResult = ExtractSubmodelMetadata(submodelId);

        _semanticIdToColumnMapping = GetSemanticIdToColumnMapping(semanticIds);

        var sqlQuery = GetSqlQueryForSubmodel(extractionResult.SubmodelName.ToString());

        var responseSemanticTreeNode = await submodelDataProvider.GetSubmodelValuesAsync(sqlQuery, extractionResult.ProductId, cancellationToken).ConfigureAwait(false);

        var result = BuildResponseFromRequest(semanticIds, responseSemanticTreeNode);

        RemoveIndexPrefix(result);

        return result;
    }

    private SubmodelExtractionResult ExtractSubmodelMetadata(string submodelId)
    {
        var productId = ExtractProductId(submodelId);
        var submodelName = ExtractSubmodelName(submodelId);

        if (!Enum.TryParse<SubmodelName>(submodelName, ignoreCase: true, result: out var parsedSubmodelName))
        {
            _logger.LogError("Submodel name '{SubmodelName}' is not recognized.", submodelName);
            throw new NotFoundException($"Submodel name '{submodelName}' is not recognized.");
        }

        return new SubmodelExtractionResult(productId, submodelName, parsedSubmodelName);
    }

    private string ExtractProductId(string submodelId)
    {
        var productId = _productIdExtractionRules
            .Select(rule => new
            {
                Rule = rule,
                Parts = submodelId?.Split(rule.Separator)
            })
            .Where(x => x.Parts is { Length: >= 1 } && x.Rule.Index > 0 && x.Parts.Length >= x.Rule.Index)
            .Select(x => x.Parts![x.Rule.Index - 1])
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(productId))
        {
            return productId;
        }

        _logger.LogError("ProductId could not be extracted from the provided submodel Identifier.");
        throw new NotFoundException();
    }

    private string ExtractSubmodelName(string submodelId)
    {
        var submodelName = _submodelNameExtractionRules
            .Where(pattern => pattern.Pattern
                .Any(p => Regex.IsMatch(submodelId, p, RegexOptions.IgnoreCase | RegexOptions.Compiled, _regexTimeout)))
            .Select(templatePattern => templatePattern.SubmodelName)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(submodelName))
        {
            return submodelName;
        }

        _logger.LogError("Submodel Name could not be extracted from the provided submodel Identifier.");
        throw new NotFoundException();
    }

    private string GetSqlQueryForSubmodel(string submodelName)
    {
        var sqlQuery = queryProvider.GetQuery(submodelName);
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new InvalidOperationException($"SQL query not found for: {submodelName}");
        }

        return sqlQuery;
    }

    private Dictionary<string, string> GetSemanticIdToColumnMapping(SemanticTreeNode requestedSemanticTreeNode)
    {
        if (requestedSemanticTreeNode == null)
        {
            return [];
        }

        var mappingJson = MappingData.MappingJson;

        try
        {
            var mapping = mappingJson.RootElement.Deserialize<List<MappingItem?>>(_jsonSerializerOptions) ?? [];

            if (mapping.Count == 0)
            {
                _logger.LogError("Failed to get column name mapping for request");
                throw new InternalDataProcessingException();
            }

            var result = new Dictionary<string, string>();
            var nodesToProcess = new Queue<SemanticTreeNode>();
            nodesToProcess.Enqueue(requestedSemanticTreeNode);

            while (nodesToProcess.Count > 0)
            {
                var node = nodesToProcess.Dequeue();
                var semanticId = node.SemanticId;

                var columnName = ResolveColumnNameForSemanticId(semanticId, mapping, node);

                result[semanticId] = columnName;

                if (node is SemanticBranchNode branchNode && branchNode.Children is { Count: > 0 })
                {
                    foreach (var child in branchNode.Children)
                    {
                        nodesToProcess.Enqueue(child);
                    }
                }
            }

            return result;
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Failed to deserialize mapping.json while extracting semantic ids.");
            throw new InternalDataProcessingException();
        }
    }

    private string ResolveColumnNameForSemanticId(string semanticId, List<MappingItem?> mapping, SemanticTreeNode node)
    {
        var indexPrefixIndex = semanticId.IndexOf(_indexContextPrefix, StringComparison.OrdinalIgnoreCase);
        var semanticIdForMapping = semanticId;
        string? suffix = null;

        if (indexPrefixIndex >= 0)
        {
            semanticIdForMapping = semanticId[..indexPrefixIndex];
            suffix = semanticId[indexPrefixIndex..];
        }

        var mappingItem = mapping.FirstOrDefault(m =>
            m?.SemanticId != null &&
            m.SemanticId.Any(id => string.Equals(id, semanticIdForMapping, StringComparison.OrdinalIgnoreCase)));

        if (mappingItem != null)
        {
            var lastSegment = mappingItem.Column?.Split('.').LastOrDefault() ?? mappingItem.Column;
            var columnName = lastSegment;

            if (suffix != null)
            {
                columnName += suffix;
            }

            return columnName!;
        }

        if (node is SemanticBranchNode)
        {
            return string.Empty;
        }

        _logger.LogError("SemanticId '{SemanticId}' not found in mapping.json.", semanticIdForMapping);
        throw new InvalidUserInputException();
    }

    private SemanticTreeNode BuildResponseFromRequest(SemanticTreeNode requestNode, SemanticTreeNode responseNode)
    {
        ArgumentNullException.ThrowIfNull(requestNode);

        return responseNode == null
            ? requestNode
            : MapRecursive(requestNode, responseNode);
    }

    private SemanticTreeNode MapRecursive(
        SemanticTreeNode requestNode,
        SemanticTreeNode responseScope)
    {
        var baseSemanticId = RemoveIndexContext(requestNode.SemanticId);
        _ = _semanticIdToColumnMapping.TryGetValue(baseSemanticId, out var columnName);

        if (requestNode is SemanticLeafNode requestLeaf)
        {
            return MapLeafNode(requestLeaf, responseScope, columnName);
        }

        var requestBranch = (SemanticBranchNode)requestNode;
        return MapBranchNode(requestBranch, responseScope, columnName);
    }

    private static SemanticLeafNode MapLeafNode(
        SemanticLeafNode requestLeaf,
        SemanticTreeNode responseScope,
        string? columnName)
    {
        SemanticLeafNode? responseLeaf = null;

        if (!string.IsNullOrEmpty(columnName))
        {
            responseLeaf = FindMatchingNodes(responseScope, columnName)
                .OfType<SemanticLeafNode>()
                .FirstOrDefault();
        }

        return new SemanticLeafNode(
            requestLeaf.SemanticId,
            requestLeaf.DataType,
            responseLeaf?.Value ?? string.Empty);
    }

    private SemanticTreeNode MapBranchNode(
        SemanticBranchNode requestBranch,
        SemanticTreeNode responseScope,
        string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return HandleBranchNoMapping(requestBranch, responseScope);
        }

        var responseBranches = FindMatchingResponseBranches(responseScope, columnName);

        if (responseBranches.Count == 0)
        {
            return CreateEmptyNode(requestBranch);
        }

        if (responseBranches.Count == 1)
        {
            return HandleBranchSingleMapping(requestBranch, responseBranches[0]);
        }

        return HandleBranchMultipleMapping(requestBranch, responseBranches);
    }

    private SemanticBranchNode HandleBranchNoMapping(
        SemanticBranchNode requestBranch,
        SemanticTreeNode responseScope)
    {
        var passthrough = new SemanticBranchNode(
            requestBranch.SemanticId,
            requestBranch.DataType);

        foreach (var child in requestBranch.Children)
        {
            AddFlattened(passthrough, MapRecursive(child, responseScope));
        }

        return passthrough;
    }

    private SemanticBranchNode HandleBranchSingleMapping(
        SemanticBranchNode requestBranch,
        SemanticBranchNode responseBranch)
    {
        var branch = new SemanticBranchNode(
            requestBranch.SemanticId,
            requestBranch.DataType);

        foreach (var child in requestBranch.Children)
        {
            AddFlattened(branch, MapRecursive(child, responseBranch));
        }

        return branch;
    }

    private SemanticTreeNode HandleBranchMultipleMapping(
        SemanticBranchNode requestBranch,
        List<SemanticBranchNode> responseBranches)
    {
        var container = new SemanticBranchNode(IndexContainerId, requestBranch.DataType);

        for (int i = 0; i < responseBranches.Count; i++)
        {
            var indexedBranch = new SemanticBranchNode(
                $"{requestBranch.SemanticId}{_indexContextPrefix}{i:00}",
                requestBranch.DataType);

            foreach (var child in requestBranch.Children)
            {
                AddFlattened(indexedBranch, MapRecursive(child, responseBranches[i]));
            }

            container.AddChild(indexedBranch);
        }

        return container;
    }

    private static void AddFlattened(SemanticBranchNode parent, SemanticTreeNode node)
    {
        if (node is SemanticBranchNode branch && branch.SemanticId == IndexContainerId)
        {
            foreach (var child in branch.Children)
            {
                parent.AddChild(child);
            }
        }
        else
        {
            parent.AddChild(node);
        }
    }

    private void RemoveIndexPrefix(SemanticTreeNode node)
    {
        var id = node.SemanticId;
        var idx = id.IndexOf(_indexContextPrefix, StringComparison.Ordinal);
        if (idx >= 0)
        {
            node.SemanticId = id[..idx];
        }

        if (node is not SemanticBranchNode branch)
        {
            return;
        }

        foreach (var child in branch.Children)
        {
            RemoveIndexPrefix(child);
        }
    }

    private List<SemanticBranchNode> FindMatchingResponseBranches(
        SemanticTreeNode responseRoot,
        string columnName)
    {
        var result = new List<SemanticBranchNode>();

        if (responseRoot is SemanticBranchNode branch)
        {
            if (RemoveIndexContext(branch.SemanticId)
                .Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(branch);
            }

            foreach (var child in branch.Children)
            {
                result.AddRange(FindMatchingResponseBranches(child, columnName));
            }
        }

        return result;
    }

    private string RemoveIndexContext(string semanticId)
    {
        var idx = semanticId.IndexOf(_indexContextPrefix, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? semanticId[..idx] : semanticId;
    }

    private static SemanticTreeNode CreateEmptyNode(SemanticTreeNode node)
    {
        if (node is SemanticLeafNode leaf)
        {
            return new SemanticLeafNode(leaf.SemanticId, leaf.DataType, string.Empty);
        }

        var branchNode = (SemanticBranchNode)node;
        var emptyBranchNode = new SemanticBranchNode(branchNode.SemanticId, branchNode.DataType);

        foreach (var child in branchNode.Children)
        {
            emptyBranchNode.AddChild(CreateEmptyNode(child));
        }

        return emptyBranchNode;
    }

    private static List<SemanticTreeNode> FindMatchingNodes(
        SemanticTreeNode root,
        string semanticId)
    {
        var result = new List<SemanticTreeNode>();

        if (root.SemanticId.Equals(semanticId, StringComparison.OrdinalIgnoreCase))
        {
            result.Add(root);
        }

        if (root is SemanticBranchNode branch)
        {
            foreach (var child in branch.Children)
            {
                result.AddRange(FindMatchingNodes(child, semanticId));
            }
        }

        return result;
    }

    private class SubmodelExtractionResult
    {
        public string ProductId { get; }
        public string SubmodelNameString { get; }
        public SubmodelName SubmodelName { get; }

        public SubmodelExtractionResult(string productId, string submodelNameString, SubmodelName submodelName)
        {
            ProductId = productId;
            SubmodelNameString = submodelNameString;
            SubmodelName = submodelName;
        }
    }
}
