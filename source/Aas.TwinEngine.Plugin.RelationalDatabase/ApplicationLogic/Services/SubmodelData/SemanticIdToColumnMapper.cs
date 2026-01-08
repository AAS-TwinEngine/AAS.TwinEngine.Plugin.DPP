using System.Text.Json;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SemanticIdToColumnMapper : ISemanticIdToColumnMapper
{
    private readonly string _indexPrefix;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ILogger<SemanticIdToColumnMapper> _logger;
    private const int MaxNodeCount = 10000;
    private readonly Lazy<List<MappingItem>> _cachedMappingData;

    public SemanticIdToColumnMapper(IOptions<Semantics> semanticsOptions, ILogger<SemanticIdToColumnMapper> logger)
    {
        ArgumentNullException.ThrowIfNull(semanticsOptions);

        _indexPrefix = semanticsOptions.Value.IndexContextPrefix;
        _logger = logger;
        _cachedMappingData = new Lazy<List<MappingItem>>(LoadMappingData, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Dictionary<string, string> GetSemanticIdToColumnMapping(SemanticTreeNode requestNode)
    {
        ArgumentNullException.ThrowIfNull(requestNode);

        var mappingData = _cachedMappingData.Value;
        return BuildSemanticIdToColumnMapping(requestNode, mappingData);
    }

    private List<MappingItem> LoadMappingData()
    {
        var mappingJson = MappingData.MappingJson;
        var items = mappingJson.Deserialize<List<MappingItem?>>(_jsonOptions)?
                               .Where(item => item != null)
                               .Select(item => item!)
                               .ToList() ?? [];

        if (items.Count != 0)
        {
            return items;
        }

        _logger.LogError("Mapping configuration is empty or contains only null items");
        throw new InternalDataProcessingException();
    }

    private Dictionary<string, string> BuildSemanticIdToColumnMapping(SemanticTreeNode root, IList<MappingItem> mappingData)
    {
        var result = new Dictionary<string, string>();
        var queue = new Queue<SemanticTreeNode>();
        var processedCount = 0;

        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            if (++processedCount > MaxNodeCount)
            {
                _logger.LogError("Exceeded maximum node count ({MaxCount}). Possible circular reference or malicious payload", MaxNodeCount);
                throw new InvalidUserInputException();
            }

            var node = queue.Dequeue();
            var columnName = ResolveColumn(node.SemanticId, mappingData, node);

            result[node.SemanticId] = columnName;

            if (node is not SemanticBranchNode { Children.Count: > 0 } branchNode)
            {
                continue;
            }

            foreach (var child in branchNode.Children)
            {
                queue.Enqueue(child);
            }
        }

        return result;
    }

    private string ResolveColumn(string semanticId, IList<MappingItem> mappingData, SemanticTreeNode node)
    {
        var (baseId, suffix) = SplitSemanticId(semanticId);
        var mappingItem = FindMapping(baseId, mappingData);

        if (mappingItem != null)
        {
            return ExtractColumnName(mappingItem.Column) + (suffix ?? string.Empty);
        }

        if (node is SemanticBranchNode)
        {
            return string.Empty;
        }

        _logger.LogError("SemanticId '{SemanticId}' not found in mapping", baseId);
        throw new InvalidUserInputException();
    }

    private (string baseId, string? suffix) SplitSemanticId(string semanticId)
    {
        var index = semanticId.IndexOf(_indexPrefix, StringComparison.OrdinalIgnoreCase);

        return index < 0 ? (semanticId, null) : (semanticId[..index], semanticId[index..]);
    }

    private static MappingItem? FindMapping(string semanticId, IList<MappingItem> mappingData)
    {
        return mappingData.FirstOrDefault(m =>
            m?.SemanticId != null &&
            m.SemanticId.Any(id => string.Equals(id, semanticId, StringComparison.OrdinalIgnoreCase)));
    }

    private static string ExtractColumnName(string? column)
    {
        if (string.IsNullOrEmpty(column))
        {
            return string.Empty;
        }

        return column.Split('.').LastOrDefault() ?? column;
    }
}
