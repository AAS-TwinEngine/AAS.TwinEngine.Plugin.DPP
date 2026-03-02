using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public class JsonSchemaSecurityValidator(IOptions<Semantics> semantics, ILogger<JsonSchemaSecurityValidator> logger) : IJsonSchemaSecurityValidator
{
    private readonly string _contextPrefix = semantics.Value.IndexContextPrefix;

    private const int MaxSchemaDepth = 10;
    private const int MaxProperties = 1000;
    private const int MaxPropertyNameLength = 256;
    private const int MaxStringValueLength = 2048;
    private const int MaxPatternLength = 512;
    private const int MaxUriLength = 2048;

    private static readonly TimeSpan RegexValidationTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly HashSet<string> AllowedSchemaKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "$schema", "$id", "$ref", "$comment",
        "type", "properties", "required", "items", "definitions", "$defs",
        "title", "description", "default", "examples",
        "enum", "const",
        "minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum",
        "minLength", "maxLength", "pattern", "format",
        "minItems", "maxItems", "uniqueItems",
        "minProperties", "maxProperties",
        "additionalProperties", "patternProperties",
        "allOf", "anyOf", "oneOf", "not",
        "multipleOf", "minContains", "maxContains"
    };

    private static readonly HashSet<string> AllowedUriSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "urn"
    };

    public void ValidateSchemaComplexity(JsonNode rootNode)
    {
        var stack = new Stack<(JsonNode node, int depth)>();
        stack.Push((rootNode, 0));

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

    public void ValidateSchemaContent(JsonNode rootNode)
    {
        var stack = new Stack<JsonNode>();
        stack.Push(rootNode);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case JsonObject obj:
                    ValidateJsonObject(obj);
                    foreach (var property in obj)
                    {
                        if (property.Value != null)
                        {
                            stack.Push(property.Value);
                        }
                    }

                    break;

                case JsonArray arr:
                    foreach (var item in arr)
                    {
                        if (item != null)
                        {
                            stack.Push(item);
                        }
                    }

                    break;

                case JsonValue value:
                    ValidateJsonValue(value);
                    break;
            }
        }
    }

    private void ValidateJsonObject(JsonObject obj)
    {
        foreach (var property in obj)
        {
            var propertyName = property.Key;

            if (propertyName.Length > MaxPropertyNameLength)
            {
                ThrowBadRequest($"Property name exceeds maximum length of {MaxPropertyNameLength} characters: {propertyName[..Math.Min(50, propertyName.Length)]}...");
            }

            if (!AllowedSchemaKeywords.Contains(propertyName) &&
                !propertyName.StartsWith('$') &&
                !propertyName.Contains(_contextPrefix, StringComparison.Ordinal))
            {
                var cleanedName = RemoveContextSuffix(propertyName);
                if (!cleanedName.IsValidIdentifier())
                {
                    ThrowBadRequest($"Property name contains potentially malicious patterns: {propertyName}");
                }
            }

            switch (propertyName)
            {
                case "$ref":
                case "$id":
                case "$schema":
                    if (property.Value is JsonValue refValue && refValue.TryGetValue<string>(out var uriString))
                    {
                        ValidateUri(uriString, propertyName);
                    }

                    break;

                case "pattern":
                    if (property.Value is JsonValue patternValue && patternValue.TryGetValue<string>(out var pattern))
                    {
                        ValidateRegexPattern(pattern);
                    }

                    break;
            }
        }
    }

    private void ValidateJsonValue(JsonValue value)
    {
        if (!value.TryGetValue<string>(out var stringValue))
        {
            return;
        }

        if (stringValue.Length > MaxStringValueLength)
        {
            ThrowBadRequest($"String value exceeds maximum length of {MaxStringValueLength} characters.");
        }

        if (stringValue.Contains('\0', StringComparison.Ordinal) ||
            stringValue.Contains("%00", StringComparison.OrdinalIgnoreCase))
        {
            ThrowBadRequest("String value contains null byte characters.");
        }
    }

    private void ValidateUri(string uriString, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(uriString))
        {
            return;
        }

        if (uriString.Length > MaxUriLength)
        {
            ThrowBadRequest($"URI in '{propertyName}' exceeds maximum length of {MaxUriLength} characters.");
        }

        if (!uriString.IsValidIdentifier())
        {
            ThrowBadRequest($"URI in '{propertyName}' contains potentially malicious patterns: {uriString[..Math.Min(50, uriString.Length)]}");
        }

        if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            if (!AllowedUriSchemes.Contains(uri.Scheme))
            {
                ThrowBadRequest($"URI scheme '{uri.Scheme}' is not allowed in '{propertyName}'. Allowed schemes: {string.Join(", ", AllowedUriSchemes)}");
            }
        }
        else if (uriString.Contains("..", StringComparison.Ordinal) &&
            !uriString.StartsWith("#/", StringComparison.Ordinal))
        {
            ThrowBadRequest($"URI in '{propertyName}' contains potential path traversal pattern.");
        }
    }

    private void ValidateRegexPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        if (pattern.Length > MaxPatternLength)
        {
            ThrowBadRequest($"Regex pattern exceeds maximum length of {MaxPatternLength} characters.");
        }

        if (ContainsDangerousRegexPattern(pattern))
        {
            ThrowBadRequest("Regex pattern contains potentially dangerous constructs that could cause ReDoS attacks.");
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.None, RegexValidationTimeout);
        }
        catch (ArgumentException ex)
        {
            ThrowBadRequest($"Invalid regex pattern: {ex.Message}");
        }
        catch (RegexMatchTimeoutException)
        {
            ThrowBadRequest("Regex pattern is too complex and could cause performance issues.");
        }
    }

    private static bool ContainsDangerousRegexPattern(string pattern)
    {
        var dangerousPatterns = new[]
        {
            @"\([^)]*\+[^)]*\)\+",
            @"\([^)]*\*[^)]*\)\*",
            @"\([^)]*\+[^)]*\)\*",
            @"\([^)]*\*[^)]*\)\+",
            @"\([^)]*\{[0-9]+,\}[^)]*\)\+",
            @"\([^)]*\{[0-9]+,\}[^)]*\)\*"
        };

        foreach (var dangerousPattern in dangerousPatterns)
        {
            try
            {
                if (Regex.IsMatch(pattern, dangerousPattern, RegexOptions.None, TimeSpan.FromMilliseconds(50)))
                {
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return true;
            }
        }

        return false;
    }

    private string RemoveContextSuffix(string propertyName)
    {
        var suffixIndex = propertyName.IndexOf(_contextPrefix, StringComparison.Ordinal);
        return suffixIndex >= 0 ? propertyName[..suffixIndex] : propertyName;
    }

    private void ThrowBadRequest(string message)
    {
        logger.LogError("Validation error: {Message}", message);
        throw new BadRequestException();
    }
}
