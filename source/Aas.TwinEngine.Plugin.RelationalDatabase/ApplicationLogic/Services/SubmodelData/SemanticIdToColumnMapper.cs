using System.Text.Json;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SemanticIdToColumnMapper(
    IOptions<Semantics> semanticsOptions,
    ILogger<SemanticIdToColumnMapper> logger) : ISemanticIdToColumnMapper
{
    private readonly string _indexPrefix = semanticsOptions.Value.IndexContextPrefix;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Dictionary<string, string> GetSemanticIdToColumnMapping(SemanticTreeNode requestNode)
    {
        if (requestNode == null)
        {
            return [];
        }

        try
        {
            var mappingData = DeserializeMappingData();
            return BuildSemanticIdToColumnMapping(requestNode, mappingData);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to de-serialize mapping configuration");
            throw new InternalDataProcessingException();
        }
    }

    private List<MappingItem?> DeserializeMappingData()
    {
        var mappingJson = MappingData.MappingJson;
        var mappingData = mappingJson.RootElement.Deserialize<List<MappingItem?>>(_jsonOptions) ?? [];

        if (mappingData.Count == 0)
        {
            logger.LogError("Mapping configuration is empty");
            throw new InternalDataProcessingException();
        }

        return mappingData;
    }

    private Dictionary<string, string> BuildSemanticIdToColumnMapping(SemanticTreeNode root, List<MappingItem?> mappingData)
    {
        var result = new Dictionary<string, string>();
        var queue = new Queue<SemanticTreeNode>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var columnName = ResolveColumn(node.SemanticId, mappingData, node);

            result[node.SemanticId] = columnName;

            if (node is SemanticBranchNode branchNode && branchNode.Children.Count > 0)
            {
                foreach (var child in branchNode.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return result;
    }

    private string ResolveColumn(string semanticId, List<MappingItem?> mappingData, SemanticTreeNode node)
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

        logger.LogError("SemanticId '{SemanticId}' not found in mapping", baseId);
        throw new InvalidUserInputException();
    }

    private (string baseId, string? suffix) SplitSemanticId(string semanticId)
    {
        var index = semanticId.IndexOf(_indexPrefix, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return (semanticId, null);
        }

        return (semanticId[..index], semanticId[index..]);
    }

    private static MappingItem? FindMapping(string semanticId, List<MappingItem?> mappingData)
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
