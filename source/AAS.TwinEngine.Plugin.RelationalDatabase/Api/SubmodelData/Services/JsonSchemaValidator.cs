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
    private const string DefsPrefix = "#/$defs/";

    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly EvaluationOptions _evaluationOptions = new()
    {
        OutputFormat = OutputFormat.List
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
            var result = MetaSchemas.Draft202012.Evaluate(schemaDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                LogAndThrowRequestException("Schema is not valid against Draft-7.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowRequestException("Draft-2020-12 evaluation failed.", ex);
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

        if (!TryNormalizeSchema(requestSchema, out var normalizedSchema, out var normalizeError))
        {
            LogAndThrowResponseException($"Failed to normalize request schema: {normalizeError}");
        }

        try
        {
            var schema = JsonSchema.FromText(normalizedSchema.ToJsonString());

            var result = schema.Evaluate(responseDoc!.RootElement, _evaluationOptions);

            if (!result.IsValid)
            {
                LogAndThrowResponseException("Response did not validate against schema.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowResponseException("Exception occurred during response validation.", ex);
        }
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

    private bool TryNormalizeSchema(JsonSchema schema, out JsonObject normalized, out string? error)
    {
        error = null;
        normalized = [];

        try
        {
            var json = JsonSerializer.Serialize(schema, SerializationOptions);

            normalized = JsonNode.Parse(json)?.AsObject()
                ?? throw new ArgumentException("Failed to parse schema JSON.");

            EscapeJsonReferencePointers(normalized);

            normalized["$schema"] ??= "https://json-schema.org/draft/2020-12/schema";

            return true;
        }
        catch (Exception ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
    }

    private void EscapeJsonReferencePointers(JsonNode? currentNode)
    {
        switch (currentNode)
        {
            case JsonObject obj:
                ProcessJsonObjectForEscaping(obj);
                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    EscapeJsonReferencePointers(item);
                }
                break;
        }
    }

    private void ProcessJsonObjectForEscaping(JsonObject jsonObject)
    {
        var propertiesToRename = jsonObject
            .Select(p => p.Key)
            .Select(name => (original: name, stripped: RemoveContextSuffix(name)))
            .Where(x => x.original != x.stripped)
            .ToList();

        foreach (var (original, stripped) in propertiesToRename)
        {
            RenameJsonProperty(jsonObject, original, stripped);
        }

        if (jsonObject.TryGetPropertyValue("required", out var requiredNode) &&
            requiredNode is JsonArray requiredArray)
        {
            RemoveContextSuffixFromRequiredProperties(requiredArray);
        }

        foreach (var property in jsonObject.ToList())
        {
            if (property.Key == "$ref" &&
                property.Value is JsonValue value &&
                value.TryGetValue<string>(out var reference))
            {
                if (reference.StartsWith(DefsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject["$ref"] = BuildEscapedReferencePath(reference);
                }
            }
            else
            {
                EscapeJsonReferencePointers(property.Value);
            }
        }
    }

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

    private string BuildEscapedReferencePath(string reference)
    {
        if (!reference.StartsWith(DefsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return reference;
        }

        var body = reference[DefsPrefix.Length..];

        var stripped = RemoveContextSuffix(body);

        var escaped = stripped
            .Replace("~", "~0", StringComparison.OrdinalIgnoreCase)
            .Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

        return DefsPrefix + escaped;
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
