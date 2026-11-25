using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.ContentModifiers;

/// <summary>
/// Derives the expected content-modifier representations ($metadata, $reference, $path, $value)
/// from the normal representation of the same live resource.
/// </summary>
internal static partial class ContentModifierExpectations
{
    private static readonly Dictionary<string, string[]> ValueBearingFields = new(StringComparer.Ordinal)
    {
        ["SubmodelElementCollection"] = ["value"],
        ["SubmodelElementList"] = ["value"],
        ["Entity"] = ["statements", "globalAssetId", "specificAssetIds"],
        ["BasicEventElement"] = ["observed"],
        ["Property"] = ["value", "valueId"],
        ["MultiLanguageProperty"] = ["value", "valueId"],
        ["Range"] = ["min", "max"],
        ["ReferenceElement"] = ["value"],
        ["RelationshipElement"] = ["first", "second"],
        ["AnnotatedRelationshipElement"] = ["first", "second", "annotations"],
        ["Blob"] = ["value", "contentType"],
        ["File"] = ["value", "contentType"]
    };

    private static readonly HashSet<string> NumericValueTypes = new(StringComparer.Ordinal)
    {
        "xs:decimal", "xs:integer", "xs:double", "xs:float", "xs:byte", "xs:short", "xs:int", "xs:long",
        "xs:unsignedByte", "xs:unsignedShort", "xs:unsignedInt", "xs:unsignedLong",
        "xs:positiveInteger", "xs:nonNegativeInteger", "xs:negativeInteger", "xs:nonPositiveInteger"
    };

    // ---------- decoding / parsing helpers ----------

    public static string DecodeBase64Url(string encoded)
    {
        var base64 = encoded.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    /// <summary>Bracket idShortPaths are double-encoded in the URL (e.g. <c>Documents%255B0%255D</c>).</summary>
    public static string DecodeIdShortPath(string urlIdShortPath)
        => Uri.UnescapeDataString(Uri.UnescapeDataString(urlIdShortPath));

    public static JsonNode? ParseNode(string content) => JsonNode.Parse(content);

    /// <summary>Asserts the paged envelope and returns its <c>result</c>.</summary>
    public static JsonNode UnwrapPagedResult(JsonNode? root)
    {
        var envelope = Assert.IsAssignableFrom<JsonObject>(root);
        Assert.True(envelope.ContainsKey("paging_metadata"), "Response misses 'paging_metadata'.");
        Assert.True(envelope.ContainsKey("result"), "Response misses 'result'.");
        return Assert.IsAssignableFrom<JsonNode>(envelope["result"]);
    }

    public static JsonArray UnwrapPagedArray(JsonNode? root) => Assert.IsAssignableFrom<JsonArray>(UnwrapPagedResult(root));

    public static void AssertJsonEquals(JsonNode? expected, JsonNode? actual, string context)
        => Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"{context}{Environment.NewLine}Expected: {expected?.ToJsonString() ?? "null"}{Environment.NewLine}Actual:   {actual?.ToJsonString() ?? "null"}");

    public static string ModelType(JsonNode element)
        => element["modelType"]?.GetValue<string>() ?? throw new InvalidOperationException("Element has no modelType.");

    public static string? IdShort(JsonNode element) => element["idShort"]?.GetValue<string>();

    public static JsonArray? Children(JsonNode element) => ModelType(element) switch
    {
        "SubmodelElementCollection" or "SubmodelElementList" => element["value"] as JsonArray,
        "Entity" => element["statements"] as JsonArray,
        "AnnotatedRelationshipElement" => element["annotations"] as JsonArray,
        _ => null
    };

    public static bool SupportsPath(JsonNode element)
        => ModelType(element) is "SubmodelElementCollection" or "SubmodelElementList" or "Entity" or "AnnotatedRelationshipElement";

    // ---------- $metadata ----------

    public static JsonNode SubmodelMetadata(JsonNode normalSubmodel)
    {
        var metadata = normalSubmodel.DeepClone().AsObject();
        metadata.Remove("submodelElements");
        return metadata;
    }

    public static JsonNode ElementMetadata(JsonNode normalElement)
    {
        var metadata = normalElement.DeepClone().AsObject();
        if (ValueBearingFields.TryGetValue(ModelType(normalElement), out var fields))
        {
            foreach (var field in fields)
            {
                metadata.Remove(field);
            }
        }

        return metadata;
    }

    public static JsonArray SubmodelsMetadata(JsonArray normalSubmodels)
        => new([.. normalSubmodels.Select(submodel => SubmodelMetadata(submodel!))]);

    public static JsonArray ElementsMetadata(JsonArray normalElements)
        => new([.. normalElements.Select(element => ElementMetadata(element!))]);

    // ---------- $reference ----------

    public static JsonNode SubmodelReference(string decodedSubmodelId)
        => ModelReference(("Submodel", decodedSubmodelId));

    public static JsonNode ShellReference(string decodedAasId)
        => ModelReference(("AssetAdministrationShell", decodedAasId));

    public static JsonArray SubmodelsReference(JsonArray normalSubmodels)
        => new([.. normalSubmodels.Select(submodel => SubmodelReference(submodel!["id"]!.GetValue<string>()))]);

    public static JsonArray ShellsReference(JsonArray normalShells)
        => new([.. normalShells.Select(shell => ShellReference(shell!["id"]!.GetValue<string>()))]);

    public static JsonArray ElementsReference(string decodedSubmodelId, JsonArray normalElements)
        => new([.. normalElements.Select(element => ModelReference(("Submodel", decodedSubmodelId), (ModelType(element!), IdShort(element!)!)))]);

    /// <summary>
    /// Submodel key plus one key per path segment; a list index segment yields the key of the list
    /// followed by a key of the item's type whose value is the index.
    /// </summary>
    public static JsonNode ElementReference(string decodedSubmodelId, JsonNode normalSubmodel, string decodedIdShortPath)
    {
        var keys = new List<(string Type, string Value)> { ("Submodel", decodedSubmodelId) };
        var rootElements = normalSubmodel["submodelElements"] as JsonArray ?? [];

        JsonNode? current = null;
        foreach (Match segment in PathSegmentRegex().Matches(decodedIdShortPath))
        {
            if (segment.Groups["name"].Success)
            {
                var name = segment.Groups["name"].Value;
                var siblings = current is null ? rootElements : Children(current);
                current = siblings?.FirstOrDefault(child => IdShort(child!) == name);
                Assert.True(current is not null, $"Element '{name}' of path '{decodedIdShortPath}' not found in the normal representation.");
                keys.Add((ModelType(current), name));
            }
            else
            {
                var index = int.Parse(segment.Groups["index"].Value, CultureInfo.InvariantCulture);
                var items = current is null ? null : Children(current);
                Assert.True(items is not null && index < items.Count, $"List item [{index}] of path '{decodedIdShortPath}' not found in the normal representation.");
                current = items[index];
                keys.Add((ModelType(current!), index.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return ModelReference([.. keys]);
    }

    private static JsonObject ModelReference(params (string Type, string Value)[] keys)
        => new()
        {
            ["type"] = "ModelReference",
            ["keys"] = new JsonArray([.. keys.Select(key => (JsonNode)new JsonObject { ["type"] = key.Type, ["value"] = key.Value })])
        };

    // ---------- $path ----------

    /// <summary>Pre-order paths of all elements of a submodel (the submodel's own idShort is not included).</summary>
    public static List<string> SubmodelPaths(JsonNode normalSubmodel)
        => ElementsPaths(normalSubmodel["submodelElements"] as JsonArray);

    public static List<string> ElementsPaths(JsonArray? normalElements)
    {
        var paths = new List<string>();
        foreach (var element in OrderByIdShort(normalElements))
        {
            AppendWithDescendants(element, IdShort(element)!, paths);
        }

        return paths;
    }

    public static List<string> ElementPaths(JsonNode normalElement, string decodedIdShortPath)
    {
        var paths = new List<string>();
        AppendWithDescendants(normalElement, decodedIdShortPath, paths);
        return paths;
    }

    public static JsonArray ToJsonArray(IEnumerable<string> values)
        => new([.. values.Select(value => (JsonNode)JsonValue.Create(value)!)]);

    private static void AppendWithDescendants(JsonNode element, string path, List<string> paths)
    {
        paths.Add(path);
        var children = Children(element);

        if (ModelType(element) == "SubmodelElementList")
        {
            for (var index = 0; index < (children?.Count ?? 0); index++)
            {
                AppendWithDescendants(children![index]!, $"{path}[{index.ToString(CultureInfo.InvariantCulture)}]", paths);
            }

            return;
        }

        foreach (var child in OrderByIdShort(children))
        {
            AppendWithDescendants(child, $"{path}.{IdShort(child)}", paths);
        }
    }

    private static IEnumerable<JsonNode> OrderByIdShort(JsonArray? elements)
        => (elements ?? []).Where(element => element is not null && IdShort(element) is not null)
                           .Select(element => element!)
                           .OrderBy(element => IdShort(element), StringComparer.Ordinal);

    // ---------- $value ----------

    /// <summary>The value-only submodel is an unnamed object: it contains the elements only.</summary>
    public static void AssertSubmodelValueOnly(JsonNode? actual, JsonNode normalSubmodel)
    {
        var value = Assert.IsAssignableFrom<JsonObject>(actual);
        foreach (var metadataKey in new[] { "idShort", "modelType", "id", "submodelElements" })
        {
            Assert.False(value.ContainsKey(metadataKey), $"Value-only submodel must not contain '{metadataKey}'.");
        }

        AssertElementsValueOnly(value, normalSubmodel["submodelElements"] as JsonArray, "submodel");
    }

    /// <summary>Object whose keys are the idShorts (ordinal order) of the children that have a value.</summary>
    public static void AssertElementsValueOnly(JsonNode? actual, JsonArray? normalChildren, string context)
    {
        var value = Assert.IsAssignableFrom<JsonObject>(actual);
        var supportedChildren = OrderByIdShort(normalChildren).Where(IsValueSupported).ToList();

        var expectedKeys = supportedChildren.Where(HasValue).Select(child => IdShort(child)!).ToList();
        Assert.Equal(expectedKeys, [.. value.Select(property => property.Key)]);

        foreach (var child in supportedChildren.Where(HasValue))
        {
            var key = IdShort(child)!;
            AssertValueOnly(value[key], child, isListItem: false, $"{context}.{key}");
        }
    }

    public static void AssertValueOnly(JsonNode? actual, JsonNode normalElement, bool isListItem, string context)
    {
        var modelType = ModelType(normalElement);

        if (!HasValue(normalElement))
        {
            if (isListItem && modelType == "MultiLanguageProperty")
            {
                var empty = Assert.IsAssignableFrom<JsonArray>(actual);
                Assert.Empty(empty);
            }
            else
            {
                Assert.True(actual is null, $"'{context}' has no value and must be serialized as null, but was {actual?.ToJsonString()}.");
            }

            return;
        }

        switch (modelType)
        {
            case "Property":
                AssertTypedValue(actual, normalElement["value"]!.GetValue<string>(), Str(normalElement, "valueType"), context);
                break;
            case "MultiLanguageProperty":
                AssertLangStrings(actual, normalElement["value"] as JsonArray, context);
                break;
            case "SubmodelElementCollection":
                AssertElementsValueOnly(actual, Children(normalElement), context);
                break;
            case "SubmodelElementList":
                AssertListValueOnly(actual, Children(normalElement), context);
                break;
            case "File":
            case "Blob":
                AssertFileOrBlob(actual, normalElement);
                break;
            case "Range":
                AssertRange(actual, normalElement, context);
                break;
            case "ReferenceElement":
                AssertJsonEquals(normalElement["value"], actual, $"'{context}' reference mismatch.");
                break;
            case "RelationshipElement":
            case "AnnotatedRelationshipElement":
                AssertRelationship(actual, normalElement, context);
                break;
            case "Entity":
                AssertEntity(actual, normalElement, context);
                break;
            case "BasicEventElement":
                var basicEvent = Assert.IsAssignableFrom<JsonObject>(actual);
                AssertJsonEquals(normalElement["observed"], basicEvent["observed"], $"'{context}' observed mismatch.");
                break;
            default:
                Assert.Fail($"'{context}': unexpected modelType '{modelType}' for a value-only element.");
                break;
        }
    }

    public static bool IsValueSupported(JsonNode element) => ModelType(element) is not ("Operation" or "Capability");

    public static bool HasValue(JsonNode element) => ModelType(element) switch
    {
        "Property" or "ReferenceElement" => element["value"] is not null,
        "MultiLanguageProperty" => element["value"] is JsonArray { Count: > 0 },
        "File" or "Blob" => element["value"] is not null || element["contentType"] is not null,
        "Range" => element["min"] is not null || element["max"] is not null,
        "RelationshipElement" => element["first"] is not null || element["second"] is not null,
        "AnnotatedRelationshipElement" => element["first"] is not null || element["second"] is not null || element["annotations"] is JsonArray { Count: > 0 },
        "BasicEventElement" => element["observed"] is not null,
        "Entity" or "SubmodelElementCollection" or "SubmodelElementList" => true,
        _ => false
    };

    private static void AssertListValueOnly(JsonNode? actual, JsonArray? normalItems, string context)
    {
        var items = Assert.IsAssignableFrom<JsonArray>(actual);
        Assert.Equal(normalItems?.Count ?? 0, items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            AssertValueOnly(items[index], normalItems![index]!, isListItem: true, $"{context}[{index.ToString(CultureInfo.InvariantCulture)}]");
        }
    }

    private static void AssertTypedValue(JsonNode? actual, string value, string? valueType, string context)
    {
        var scalar = Assert.IsAssignableFrom<JsonValue>(actual);
        var kind = scalar.GetValueKind();
        var trimmed = value.Trim();

        if (valueType == "xs:boolean" && trimmed is "true" or "1" or "false" or "0")
        {
            var expectedKind = trimmed is "true" or "1" ? System.Text.Json.JsonValueKind.True : System.Text.Json.JsonValueKind.False;
            Assert.Equal(expectedKind, kind);
            return;
        }

        if (valueType is not null && NumericValueTypes.Contains(valueType)
            && double.TryParse(trimmed.TrimStart('+'), NumberStyles.Float, CultureInfo.InvariantCulture, out var expectedNumber)
            && double.IsFinite(expectedNumber))
        {
            Assert.True(kind == System.Text.Json.JsonValueKind.Number, $"'{context}' ({valueType}) must be a JSON number but was {scalar.ToJsonString()}.");
            Assert.Equal(expectedNumber, double.Parse(scalar.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture));
            return;
        }

        Assert.True(kind == System.Text.Json.JsonValueKind.String, $"'{context}' ({valueType}) must be a JSON string but was {scalar.ToJsonString()}.");
        Assert.Equal(value, scalar.GetValue<string>());
    }

    private static void AssertLangStrings(JsonNode? actual, JsonArray? normalLangStrings, string context)
    {
        var entries = Assert.IsAssignableFrom<JsonArray>(actual);
        var expected = (normalLangStrings ?? []).OrderBy(entry => entry!["language"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(expected.Count, entries.Count);

        for (var index = 0; index < expected.Count; index++)
        {
            var entry = Assert.IsAssignableFrom<JsonObject>(entries[index]);
            var language = expected[index]!["language"]!.GetValue<string>();
            Assert.True(entry.Count == 1 && entry.ContainsKey(language), $"'{context}' entry {index} must be a single-key object for language '{language}'.");
            Assert.Equal(expected[index]!["text"]!.GetValue<string>(), entry[language]!.GetValue<string>());
        }
    }

    private static void AssertFileOrBlob(JsonNode? actual, JsonNode normalElement)
    {
        var value = Assert.IsAssignableFrom<JsonObject>(actual);
        Assert.Equal(Str(normalElement, "contentType"), Str(value, "contentType"));

        if (ModelType(normalElement) == "File")
        {
            Assert.Equal(Str(normalElement, "value"), Str(value, "value"));
        }
        else
        {
            Assert.Equal(normalElement["value"] is not null, value.ContainsKey("value"));
        }
    }

    private static void AssertRange(JsonNode? actual, JsonNode normalElement, string context)
    {
        var value = Assert.IsAssignableFrom<JsonObject>(actual);
        var valueType = Str(normalElement, "valueType");

        foreach (var bound in new[] { "min", "max" })
        {
            var expected = Str(normalElement, bound);
            if (expected is null)
            {
                Assert.False(value.ContainsKey(bound), $"'{context}' must not contain '{bound}'.");
            }
            else
            {
                AssertTypedValue(value[bound], expected, valueType, $"{context}.{bound}");
            }
        }
    }

    private static void AssertRelationship(JsonNode? actual, JsonNode normalElement, string context)
    {
        var value = Assert.IsAssignableFrom<JsonObject>(actual);
        AssertJsonEquals(normalElement["first"], value["first"], $"'{context}' first mismatch.");
        AssertJsonEquals(normalElement["second"], value["second"], $"'{context}' second mismatch.");

        if (ModelType(normalElement) == "AnnotatedRelationshipElement" && value.ContainsKey("annotations"))
        {
            AssertElementsValueOnly(value["annotations"], normalElement["annotations"] as JsonArray, $"{context}.annotations");
        }
    }

    private static void AssertEntity(JsonNode? actual, JsonNode normalElement, string context)
    {
        var value = Assert.IsAssignableFrom<JsonObject>(actual);
        var statements = Children(normalElement);
        var hasStatementValues = OrderByIdShort(statements).Where(IsValueSupported).Any(HasValue);

        if (hasStatementValues)
        {
            AssertElementsValueOnly(value["statements"], statements, $"{context}.statements");
        }
        else
        {
            Assert.False(value.ContainsKey("statements"), $"'{context}' must not contain empty 'statements'.");
        }

        Assert.Equal(Str(normalElement, "entityType"), Str(value, "entityType"));
    }

    private static string? Str(JsonNode? node, string property) => node?[property]?.GetValue<string>();

    [GeneratedRegex(@"(?<name>[^.\[\]]+)|\[(?<index>\d+)\]")]
    private static partial Regex PathSegmentRegex();
}
