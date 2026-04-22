using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public class JsonSchemaValidator(IOptions<Semantics> semantics,
                                 ILogger<JsonSchemaValidator> logger,
                                 IJsonSchemaSecurityValidator securityValidator) : IJsonSchemaValidator
{
    private readonly string _contextPrefix = semantics.Value.IndexContextPrefix;
    private const int MaxSchemaSize = 1_048_576; // 1MB
    private const string DefinitionsPrefix = "#/definitions/";
    private const string DefsPrefix = "#/$defs/";

    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public void ValidateRequestSchema(JsonSchema schema)
    {
        if (!TrySerializeSchema(schema!, out var schemaText, out var serializationError))
        {
            LogAndThrowRequestException($"Schema serialization failed: {serializationError}");
        }

        if (schemaText.Length > MaxSchemaSize)
        {
            LogAndThrowRequestException($"Schema size exceeds the maximum allowed size of {MaxSchemaSize} bytes.");
        }

        if (!TryParseSchemaNode(schemaText, out var schemaNode, out var parseError))
        {
            LogAndThrowRequestException($"Schema JSON is invalid: {parseError}");
        }

        if (schemaNode == null)
        {
            LogAndThrowRequestException("Serialized schema resulted in null JsonNode.");
        }

        securityValidator.ValidateSchemaComplexity(schemaNode!);
        securityValidator.ValidateSchemaContent(schemaNode!);

        try
        {
            using var schemaDocument = JsonDocument.Parse(schemaNode.ToJsonString());
            var result = MetaSchemas.Draft7.Evaluate(schemaDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                LogAndThrowRequestException("Schema is not valid against Draft-7.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowRequestException("Draft-7 evaluation failed.", ex);
        }
    }

    public void ValidateResponseContent(string responseJson, JsonSchema requestSchema)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            LogAndThrowResponseException("Response JSON is empty.");
        }

        if (!TryParseJson(responseJson, out var responseDoc, out var parseError))
        {
            LogAndThrowResponseException($"Failed to parse response JSON: {parseError}");
        }

        JsonObject normalizedSchema;

        try
        {
            normalizedSchema = NormalizeSchema(requestSchema);
        }
        catch (Exception ex)
        {
            LogAndThrowResponseException($"Failed to normalize request schema: Schema normalization failed: {ex.Message}");
            return;
        }

        try
        {
            ValidateResponseAgainstSchema(responseDoc!.RootElement, normalizedSchema);
        }
        catch (JsonSchemaException schemaEx) when (schemaEx.Message.Contains("Overwriting registered schemas"))
        {
            try
            {
                ValidateResponseAgainstSchemaWithoutId(responseDoc!.RootElement, normalizedSchema);
            }
            catch (Exception retryEx)
            {
                LogAndThrowResponseException("Exception occurred during response validation.", retryEx);
            }
        }
        catch (Exception ex)
        {
            LogAndThrowResponseException("Exception occurred during response validation.", ex);
        }
    }

    private void ValidateResponseAgainstSchema(JsonElement responseElement, JsonObject schemaNode)
    {
        var schema = JsonSchema.FromText(schemaNode.ToJsonString());
        var result = schema.Evaluate(responseElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!result.IsValid)
        {
            LogAndThrowResponseException("Response did not validate against schema.");
        }
    }

    private void ValidateResponseAgainstSchemaWithoutId(JsonElement responseElement, JsonObject schemaNode)
    {
        var schemaClone = JsonNode.Parse(schemaNode.ToJsonString())?.AsObject();
        if (schemaClone == null)
        {
            LogAndThrowResponseException("Failed to clone normalized schema for response validation retry.");
        }

        _ = schemaClone.Remove("$id");
        ValidateResponseAgainstSchema(responseElement, schemaClone);
    }

    [DoesNotReturn]
    private void LogAndThrowRequestException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, logMessage);
        }
        else
        {
            logger.LogError(logMessage);
        }

        throw new InvalidUserInputException(logMessage);
    }

    [DoesNotReturn]
    private void LogAndThrowResponseException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, logMessage);
        }
        else
        {
            logger.LogError(logMessage);
        }

        throw new NotFoundException(logMessage);
    }

    private static bool TrySerializeSchema(JsonSchema schema, out string schemaText, out string? error)
    {
        error = null;
        schemaText = string.Empty;

        try
        {
            schemaText = JsonSerializer.Serialize(schema, SerializationOptions);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Serialization failed: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseSchemaNode(string schemaText, out JsonNode? node, out string? error)
    {
        error = null;
        node = null;

        try
        {
            node = JsonNode.Parse(schemaText);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryParseJson(string json, out JsonDocument? document, out string? error)
    {
        error = null;
        document = null;

        try
        {
            document = JsonDocument.Parse(json);
            return true;
        }
        catch (Exception ex)
        {
            error = $"JSON parsing failed: {ex.Message}";
            return false;
        }
    }

    private JsonObject NormalizeSchema(JsonSchema schema)
    {
        var json = JsonSerializer.Serialize(schema, SerializationOptions);

        var normalized = JsonNode.Parse(json)?.AsObject()
            ?? throw new ArgumentException("Failed to parse schema JSON.");

        EscapeJsonReferencePointers(normalized);
        normalized["$id"] = normalized["$id"]?.GetValue<string>() ?? $"urn:uuid:{Guid.NewGuid():D}";

        return normalized;
    }

    private void EscapeJsonReferencePointers(JsonNode? currentNode)
    {
        switch (currentNode)
        {
            case JsonObject jsonObjectNode:
                ProcessJsonObjectForEscaping(jsonObjectNode);
                break;

            case JsonArray jsonArrayNode:
                foreach (var arrayElement in jsonArrayNode)
                {
                    EscapeJsonReferencePointers(arrayElement);
                }

                break;
        }
    }

    private void ProcessJsonObjectForEscaping(JsonObject jsonObject)
    {
        RenamePropertiesWithContextSuffix(jsonObject);
        UpdateRequiredPropertiesArray(jsonObject);
        ProcessReferencesAndChildren(jsonObject);
    }

    private void RenamePropertiesWithContextSuffix(JsonObject jsonObject)
    {
        var propertiesToRename = jsonObject
            .Select(property => property.Key)
            .Select(propertyName => (originalName: propertyName, strippedName: RemoveContextSuffix(propertyName)))
            .Where(namePair => namePair.strippedName != namePair.originalName)
            .ToList();

        foreach (var (originalName, strippedName) in propertiesToRename)
        {
            RenameJsonProperty(jsonObject, originalName, strippedName);
        }
    }

    private void UpdateRequiredPropertiesArray(JsonObject jsonObject)
    {
        if (TryGetRequiredPropertiesArray(jsonObject, out var requiredPropertiesArray))
        {
            RemoveContextSuffixFromRequiredProperties(requiredPropertiesArray);
        }
    }

    private static bool TryGetRequiredPropertiesArray(JsonObject jsonObject, [NotNullWhen(true)] out JsonArray? requiredPropertiesArray)
    {
        requiredPropertiesArray = null;
        return jsonObject.TryGetPropertyValue("required", out var node) && 
               node is JsonArray array &&
               (requiredPropertiesArray = array) != null;
    }

    private void ProcessReferencesAndChildren(JsonObject jsonObject)
    {
        foreach (var (propertyName, propertyValue) in jsonObject.ToList())
        {
            if (TryProcessSchemaReference(jsonObject, propertyName, propertyValue))
            {
                continue;
            }

            EscapeJsonReferencePointers(propertyValue);
        }
    }

    private bool TryProcessSchemaReference(JsonObject jsonObject, string propertyName, JsonNode? propertyValue)
    {
        if (!IsReferenceProperty(propertyName, propertyValue, out var referenceString))
        {
            return false;
        }

        if (TryGetReferencePrefix(referenceString, out var prefix))
        {
            jsonObject["$ref"] = BuildEscapedReferencePath(referenceString, prefix);
            return true;
        }

        return false;
    }

    private static bool IsReferenceProperty(string propertyName, JsonNode? propertyValue, [NotNullWhen(true)] out string? referenceString)
    {
        referenceString = null;
        return propertyName == "$ref" &&
               propertyValue is JsonValue referenceValue &&
               referenceValue.TryGetValue(out referenceString);
    }

    private static bool TryGetReferencePrefix(string referenceString, [NotNullWhen(true)] out string? prefix)
    {
        if (referenceString.StartsWith(DefinitionsPrefix, StringComparison.Ordinal))
        {
            prefix = DefinitionsPrefix;
            return true;
        }

        if (referenceString.StartsWith(DefsPrefix, StringComparison.Ordinal))
        {
            prefix = DefsPrefix;
            return true;
        }

        prefix = null;
        return false;
    }

    private string BuildEscapedReferencePath(string originalReferencePath, string prefix)
    {
        var referenceWithoutPrefix = originalReferencePath[prefix.Length..];
        var strippedReference = RemoveContextSuffix(referenceWithoutPrefix);
        var escapedReference = EscapeJsonPointer(strippedReference);

        return prefix + escapedReference;
    }

    private static string EscapeJsonPointer(string value)
        => value
            .Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);

    private void RemoveContextSuffixFromRequiredProperties(JsonArray requiredProperties)
    {
        for (var index = 0; index < requiredProperties.Count; index++)
        {
            if (requiredProperties[index]?.GetValue<string>() is { } propertyName)
            {
                requiredProperties[index] = RemoveContextSuffix(propertyName);
            }
        }
    }

    private string RemoveContextSuffix(string propertyName)
    {
        var suffixIndex = propertyName.IndexOf(_contextPrefix, StringComparison.Ordinal);
        return suffixIndex >= 0 ? propertyName[..suffixIndex] : propertyName;
    }

    private static void RenameJsonProperty(JsonObject jsonObject, string oldPropertyName, string newPropertyName)
    {
        if (oldPropertyName == newPropertyName)
        {
            return;
        }

        var propertyValue = jsonObject[oldPropertyName];
        _ = jsonObject.Remove(oldPropertyName);
        jsonObject[newPropertyName] = propertyValue!;
    }
}
