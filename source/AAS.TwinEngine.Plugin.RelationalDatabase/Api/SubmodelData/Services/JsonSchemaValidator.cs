using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public class JsonSchemaValidator(IOptions<Semantics> semantics, ILogger<JsonSchemaValidator> logger) : IJsonSchemaValidator
{
    private readonly string _contextPrefix = semantics.Value.IndexContextPrefix;
    private const string DefinitionsPrefix = "#/definitions/";
    private const int MaxSchemaDepth = 10;
    private const int MaxSchemaSize = 1_048_576; // 1MB
    private const int MaxProperties = 1000;

    private static readonly JsonSerializerOptions Serialization = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public void ValidateRequestSchema(JsonSchema schema)
    {
        if (!TrySerializeSchema(schema!, out var schemaText, out var serializationError))
        {
            LogAndThrowException($"Schema serialization failed: {serializationError}");
        }

        if (schemaText.Length > MaxSchemaSize)
        {
            LogAndThrowException($"Schema size exceeds the maximum allowed size of {MaxSchemaSize} bytes.");
        }

        if (!TryParseSchemaNode(schemaText, out var schemaNode, out var parseError))
        {
            LogAndThrowException($"Schema JSON is invalid: {parseError}");
        }

        if (schemaNode == null)
        {
            LogAndThrowException("Serialized schema resulted in null JsonNode.");
        }

        ValidateSchemaComplexity(schemaNode!);

        try
        {
            var result = MetaSchemas.Draft7.Evaluate(schemaNode, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                LogAndThrowException("Schema is not valid against Draft-7.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowException("Draft-7 evaluation failed.", ex);
        }
    }

    public void ValidateResponseContent(string responseJson, JsonSchema requestSchema)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            LogAndThrowException("Response JSON is empty.");
        }

        if (!TryParseJson(responseJson, out var responseDoc, out var parseError))
        {
            LogAndThrowException($"Failed to parse response JSON: {parseError}");
        }

        if (!TryNormalizeSchema(requestSchema, out var normalizedSchema, out var normalizeError))
        {
            LogAndThrowException($"Failed to normalize request schema: {normalizeError}");
        }

        if (!TryRegisterJsonSchema(normalizedSchema, out var registerError))
        {
            LogAndThrowException($"Failed to register schema: {registerError}");
        }

        try
        {
            var schema = JsonSchema.FromText(normalizedSchema.ToJsonString());
            var result = schema.Evaluate(responseDoc!.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                LogAndThrowException("Response did not validate against schema.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowException("Exception occurred during response validation.", ex);
        }
    }

    private void LogAndThrowException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, logMessage);
        }
        else
        {
            logger.LogError(logMessage);
        }

        throw new NotFoundException();
    }

    private static bool TrySerializeSchema(JsonSchema schema, out string schemaText, out string? error)
    {
        error = null;
        schemaText = string.Empty;

        try
        {
            schemaText = JsonSerializer.Serialize(schema, Serialization);
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
            var json = JsonSerializer.Serialize(schema, Serialization);

            normalized = JsonNode.Parse(json)?.AsObject()
            ?? throw new ArgumentException("Failed to parse schema JSON.");

            EscapeJsonReferencePointers(normalized);
            normalized["$id"] = normalized["$id"]?.GetValue<string>() ?? $"urn:uuid:{Guid.NewGuid():D}";

            return true;
        }
        catch (Exception ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
    }

    private static bool TryRegisterJsonSchema(JsonObject schemaJsonObject, out string? registrationErrorMessage)
    {
        registrationErrorMessage = null;

        try
        {
            var jsonSchema = JsonSchema.FromText(schemaJsonObject.ToJsonString());
            var schemaIdentifierUri = new Uri(schemaJsonObject["$id"]!.GetValue<string>()!);
            SchemaRegistry.Global.Register(schemaIdentifierUri, jsonSchema);
            return true;
        }
        catch (Exception exception)
        {
            registrationErrorMessage = $"Schema registration failed: {exception.Message}";
            return false;
        }
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
        var propertiesToRename = jsonObject
            .Select(property => property.Key)
            .Select(propertyName => (originalName: propertyName, strippedName: RemoveContextSuffix(propertyName)))
            .Where(namePair => namePair.strippedName != namePair.originalName)
            .ToList();

        foreach (var (originalName, strippedName) in propertiesToRename)
        {
            RenameJsonProperty(jsonObject, originalName, strippedName);
        }

        if (jsonObject.TryGetPropertyValue("required", out var requiredPropertiesNode) &&
            requiredPropertiesNode is JsonArray requiredPropertiesArray)
        {
            RemoveContextSuffixFromRequiredProperties(requiredPropertiesArray);
        }

        foreach (var property in jsonObject.ToList())
        {
            var propertyName = property.Key;
            var propertyValue = property.Value;

            if (propertyName == "$ref" &&
                propertyValue is JsonValue referenceValue &&
                referenceValue.TryGetValue<string>(out var referenceString) &&
                referenceString.StartsWith(DefinitionsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                jsonObject["$ref"] = BuildEscapedReferencePath(referenceString);
            }
            else
            {
                EscapeJsonReferencePointers(propertyValue);
            }
        }
    }

    private static void ValidateSchemaComplexity(JsonNode root)
    {
        var stack = new Stack<(JsonNode node, int depth)>();
        stack.Push((root, 0));

        var totalPropertiesCount = 0;

        while (stack.Count > 0)
        {
            var (current, depth) = stack.Pop();

            if (depth > MaxSchemaDepth)
            {
                throw new BadRequestException($"Schema nesting too deep. Maximum allowed depth is {MaxSchemaDepth}.");
            }

            switch (current)
            {
                case JsonObject obj:
                    if (obj.TryGetPropertyValue("properties", out var propsNode) && propsNode is JsonObject propsObj)
                    {
                        totalPropertiesCount += propsObj.Count;
                        if (totalPropertiesCount > MaxProperties)
                        {
                            throw new BadRequestException($"Schema contains too many properties. Maximum allowed is {MaxProperties}.");
                        }
                    }

                    foreach (var kv in obj)
                    {
                        if (kv.Value != null)
                        {
                            stack.Push((kv.Value, depth + 1));
                        }
                    }
                    break;

                case JsonArray arr:
                    foreach (var item in arr)
                    {
                        if (item != null)
                        {
                            stack.Push((item, depth + 1));
                        }
                    }
                    break;
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

    private string BuildEscapedReferencePath(string originalReferencePath)
    {
        var referenceWithoutPrefix = originalReferencePath[DefinitionsPrefix.Length..];

        var strippedReference = RemoveContextSuffix(referenceWithoutPrefix);

        var escapedReference = strippedReference.Replace("~", "~0", StringComparison.OrdinalIgnoreCase).Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

        return DefinitionsPrefix + escapedReference;
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
