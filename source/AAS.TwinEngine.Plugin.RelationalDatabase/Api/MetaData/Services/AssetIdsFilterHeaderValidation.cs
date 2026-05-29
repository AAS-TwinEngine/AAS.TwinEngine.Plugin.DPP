using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.AssetIdFilter;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Services;

public class AssetIdsFilterHeaderValidation(ILogger<AssetIdsFilterHeaderValidation> logger) : IAssetIdsFilterHeaderValidation
{
    public AssetIdFilterHeader? ParseToDomainModel(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        if (!TryParseHeader(headerValue, out var filter, out var parseError))
        {
            logger.LogError("Invalid asset id filter header provided: {ParseError}", parseError);
            throw new InvalidUserInputException($"Invalid aastwinengine-assetids header: {parseError}");
        }

        return filter;
    }

    private static bool TryParseHeader(string headerValue, out AssetIdFilterHeader? filter, out string? error)
    {
        filter = null;
        error = null;

        try
        {
            using var jsonDocument = JsonDocument.Parse(headerValue);
            var root = jsonDocument.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                error = "Header value must be a JSON array of SpecificAssetId objects";
                return false;
            }

            var identifiers = new List<SpecificAssetIdData>();
            foreach (var element in root.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    error = "Each element in the array must be a JSON object";
                    return false;
                }

                var identifier = ParseIdentifier(element, out var parseError);
                if (identifier == null)
                {
                    error = parseError;
                    return false;
                }

                identifiers.Add(identifier);
            }

            filter = new AssetIdFilterHeader
            {
                Identifiers = identifiers
            };

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid JSON in header: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Unexpected error parsing header: {ex.Message}";
            return false;
        }
    }

    private static SpecificAssetIdData? ParseIdentifier(JsonElement element, out string? error)
    {
        error = null;

        var unsupportedProperty = element.EnumerateObject()
            .FirstOrDefault(property =>
            !string.Equals(property.Name, "name", StringComparison.Ordinal) &&
            !string.Equals(property.Name, "value", StringComparison.Ordinal));

        if (unsupportedProperty.Value.ValueKind != JsonValueKind.Undefined)
        {
            error = $"Unsupported property '{unsupportedProperty.Name}'. Only 'name' and 'value' are allowed";
            return null;
        }

        if (!element.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            error = "SpecificAssetId must have a 'name' property (string)";
            return null;
        }

        if (!element.TryGetProperty("value", out var valueElement) || valueElement.ValueKind != JsonValueKind.String)
        {
            error = "SpecificAssetId must have a 'value' property (string)";
            return null;
        }

        var name = nameElement.GetString();
        var value = valueElement.GetString();

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "SpecificAssetId 'name' must not be empty";
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "SpecificAssetId 'value' must not be empty";
            return null;
        }

        return new SpecificAssetIdData
        {
            Name = name,
            Value = value
        };
    }
}
