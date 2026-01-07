using System.Text.Json;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Manifest;

public class ManifestProvider(ILogger<ManifestProvider> logger) : IManifestProvider
{
    private readonly JsonElement _mappingJson = MappingData.MappingJson;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public IList<string> GetSupportedSemanticIds()
    {
        logger.LogInformation("Starting getting supported semantic ids");

        return GetSemanticIdsOfLeafNode();
    }

    private List<string> GetSemanticIdsOfLeafNode()
    {
        try
        {
            var mapping = _mappingJson.Deserialize<List<MappingItem?>>(_jsonSerializerOptions) ?? [];

            if (mapping.Count == 0)
            {
                return [];
            }

            return [.. mapping
                        .Where(m => m is not null
                        && IsLeafColumnIdentifier(m!.Column)
                        && m!.SemanticId is not null
                        && m!.SemanticId.Count > 0)
                        .SelectMany(m => m!.SemanticId)
                        .Where(sid => !string.IsNullOrWhiteSpace(sid))
                        .Select(sid => sid!.Trim())
                        .Distinct(StringComparer.Ordinal)];
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "Failed to de-serialize mapping.json while extracting semantic ids.");
            throw new ResponseParsingException();
        }
    }

    /// <summary>
    /// Determines whether the provided database column identifier represents a leaf/value node
    /// </summary>
    /// <remarks>
    /// Expected formats:
    /// - "<dbo>.<TableName>.<ColumnName>" =  leaf/value node (returns true).
    /// - "<dbo>.<TableName>" =  branch/table node (returns false).
    /// Null or whitespace inputs are treated as non-matching and return false
    /// </remarks>
    private static bool IsLeafColumnIdentifier(string? value) => !string.IsNullOrWhiteSpace(value) && value.Count(c => c == '.') == 2;
}
