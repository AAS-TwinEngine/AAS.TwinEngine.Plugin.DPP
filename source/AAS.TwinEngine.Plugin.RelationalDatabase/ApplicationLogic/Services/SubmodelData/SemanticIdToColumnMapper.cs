using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

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

    public Dictionary<string, ColumnMapping> GetSemanticIdToColumnMapping(SemanticTreeNode requestNode)
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

    private Dictionary<string, ColumnMapping> BuildSemanticIdToColumnMapping(SemanticTreeNode root, IList<MappingItem> mappingData)
    {
        var result = new Dictionary<string, ColumnMapping>();
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
            var columnMapping = ResolveColumn(node.SemanticId, mappingData, node);

            result[node.SemanticId] = columnMapping;

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

    private ColumnMapping ResolveColumn(string semanticId, IList<MappingItem> mappingData, SemanticTreeNode node)
    {
        var (baseId, suffix) = SplitSemanticId(semanticId);
        var mappingItem = FindMapping(baseId, mappingData);

        if (mappingItem != null)
        {
            var baseMapping = CreateColumnMapping(mappingItem.Column);
            return AppendSuffix(baseMapping, suffix);
        }

        if (node is SemanticBranchNode)
        {
            return new ColumnMapping(string.Empty, string.Empty);
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

    private static ColumnMapping CreateColumnMapping(string? column)
    {
        if (string.IsNullOrEmpty(column))
        {
            return new ColumnMapping(string.Empty, string.Empty);
        }

        var segments = column.Split('.', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length switch
        {
            0 => new ColumnMapping(string.Empty, string.Empty),
            1 => new ColumnMapping(segments[0], string.Empty),
            2 => new ColumnMapping(segments[1], string.Empty),
            _ => new ColumnMapping(segments[^2], segments[^1])
        };
    }

    private static ColumnMapping AppendSuffix(ColumnMapping mapping, string? suffix) => string.IsNullOrEmpty(suffix) ? mapping : new ColumnMapping(BranchColumn: mapping.BranchColumn + suffix, LeafColumn: mapping.LeafColumn + suffix);
}
