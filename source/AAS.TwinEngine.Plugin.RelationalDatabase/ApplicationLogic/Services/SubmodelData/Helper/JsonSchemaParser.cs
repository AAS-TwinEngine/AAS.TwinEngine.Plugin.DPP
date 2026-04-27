using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

public static class JsonSchemaParser
{
    private const string DefsRefPrefix = "#/$defs/";
    private const string DefinitionsRefPrefix = "#/definitions/";
    private const string DefsProperty = "$defs";
    private const string DefinitionsProperty = "definitions";
    private const string Draft7Schema = "http://json-schema.org/draft-07/schema#";
    private const string Draft7SchemaHttps = "https://json-schema.org/draft-07/schema#";
    private const string Draft201909Schema = "https://json-schema.org/draft/2019-09/schema";
    private const string Draft202012Schema = "https://json-schema.org/draft/2020-12/schema";

    public static SemanticTreeNode ParseJsonSchema(JsonSchema schema, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var json = ConvertSchemaToJson(schema);
        var draft = GetSchemaDraft(json);
        return BuildSemanticTree(json, logger, draft);
    }

    private static JsonObject ConvertSchemaToJson(JsonSchema schema)
    {
        return JsonSerializer.SerializeToNode(schema)?.AsObject()
            ?? throw new BadRequestException("Invalid schema format.");
    }

    private static SemanticTreeNode BuildSemanticTree(JsonObject schema, ILogger logger, SchemaDraft draft)
    {
        var rootProperty = ExtractRootProperty(schema, logger);
        return ConvertPropertyToNode(rootProperty.Key, rootProperty.Value!, schema, draft);
    }

    private static KeyValuePair<string, JsonNode?> ExtractRootProperty(JsonObject schema, ILogger logger)
    {
        if (!schema.TryGetPropertyValue("properties", out var propsNode) || propsNode is not JsonObject props || props.Count == 0)
        {
            logger.LogError("Schema does not contain any properties");
            throw new BadRequestException("Schema must contain at least one property.");
        }

        foreach (var prop in props)
        {
            return prop;
        }

        throw new BadRequestException("Schema must contain at least one property.");
    }

    private static SemanticTreeNode ConvertPropertyToNode(string name, JsonNode node, JsonObject root, SchemaDraft draft)
    {
        var obj = node.AsObject();

        if (TryHandleReference(name, obj, root, draft, out var refNode))
        {
            return refNode;
        }

        return CreateNodeByType(name, obj, root, draft);
    }

    private static bool TryHandleReference(string name, JsonObject obj, JsonObject root, SchemaDraft draft, out SemanticTreeNode result)
    {
        result = null!;

        if (!obj.TryGetPropertyValue("$ref", out var refNode))
        {
            return false;
        }

        result = ResolveReference(name, refNode!.GetValue<string>(), root, draft);
        return true;
    }

    private static SemanticTreeNode CreateNodeByType(string name, JsonObject obj, JsonObject root, SchemaDraft draft)
    {
        var type = GetType(obj);

        return type switch
        {
            DataType.Object => BuildObjectNode(name, obj, root, draft),
            DataType.Array => BuildArrayNode(name, obj, root, draft),
            _ => CreateLeafNode(name, type)
        };
    }

    private static SemanticLeafNode CreateLeafNode(string name, DataType type) => new(name, type, string.Empty);

    private static SemanticTreeNode ResolveReference(string name, string reference, JsonObject root, SchemaDraft draft)
    {
        if (!TryResolveReference(reference, root, draft, out var defNode))
        {
            return CreateLeafNode(name, DataType.Unknown);
        }

        return ConvertPropertyToNode(name, defNode!, root, draft);
    }

    private static bool TryResolveReference(string reference, JsonObject root, SchemaDraft draft, out JsonNode? definitionNode)
    {
        definitionNode = null;
        if (!TryGetReferenceKey(reference, out var key))
        {
            return false;
        }

        var preferredDefinitionsProperty = GetPreferredDefinitionsProperty(draft);
        var fallbackDefinitionsProperty = preferredDefinitionsProperty == DefsProperty ? DefinitionsProperty : DefsProperty;

        if (TryGetDefinition(root, preferredDefinitionsProperty, key, out definitionNode))
        {
            return true;
        }

        if (TryGetDefinition(root, fallbackDefinitionsProperty, key, out definitionNode))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetReferenceKey(string reference, out string key)
    {
        key = string.Empty;

        var referencePrefix = GetReferencePrefix(reference);
        if (referencePrefix == null)
        {
            return false;
        }

        key = DecodeJsonPointerToken(reference[referencePrefix.Length..]);
        return !string.IsNullOrWhiteSpace(key);
    }

    private static string? GetReferencePrefix(string reference)
    {
        if (reference.StartsWith(DefsRefPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return DefsRefPrefix;
        }

        if (reference.StartsWith(DefinitionsRefPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return DefinitionsRefPrefix;
        }

        return null;
    }

    private static bool TryGetDefinition(JsonObject root, string definitionsProperty, string key, out JsonNode? defNode)
    {
        defNode = null;

        if (!root.TryGetPropertyValue(definitionsProperty, out var defsNode) ||
            defsNode is not JsonObject defs)
        {
            return false;
        }

        return defs.TryGetPropertyValue(key, out defNode);
    }

    private static string DecodeJsonPointerToken(string token)
    {
        return token
            .Replace("~1", "/", StringComparison.OrdinalIgnoreCase)
            .Replace("~0", "~", StringComparison.OrdinalIgnoreCase);
    }

    private static SemanticBranchNode BuildObjectNode(string name, JsonObject obj, JsonObject root, SchemaDraft draft)
    {
        var branch = new SemanticBranchNode(name, DataType.Object);

        if (!TryGetProperties(obj, out var props))
        {
            return branch;
        }

        AddObjectChildren(branch, props, root, draft);
        return branch;
    }

    private static bool TryGetProperties(JsonObject obj, out JsonObject props)
    {
        props = null!;

        if (!obj.TryGetPropertyValue("properties", out var propsNode) ||
            propsNode is not JsonObject jsonProps)
        {
            return false;
        }

        props = jsonProps;
        return true;
    }

    private static void AddObjectChildren(SemanticBranchNode branch, JsonObject props, JsonObject root, SchemaDraft draft)
    {
        foreach (var prop in props)
        {
            branch.AddChild(ConvertPropertyToNode(prop.Key, prop.Value!, root, draft));
        }
    }

    private static SemanticBranchNode BuildArrayNode(string name, JsonObject obj, JsonObject root, SchemaDraft draft)
    {
        var branch = new SemanticBranchNode(name, DataType.Array);

        if (!TryGetItems(obj, out var itemObj))
        {
            return branch;
        }

        ProcessArrayItems(branch, name, itemObj, root, draft);
        return branch;
    }

    private static bool TryGetItems(JsonObject obj, out JsonObject itemObj)
    {
        itemObj = null!;

        if (!obj.TryGetPropertyValue("items", out var itemsNode) ||
            itemsNode is not JsonObject jsonItems)
        {
            return false;
        }

        itemObj = jsonItems;
        return true;
    }

    private static void ProcessArrayItems(SemanticBranchNode branch, string name, JsonObject itemObj, JsonObject root, SchemaDraft draft)
    {
        if (itemObj.Count == 0)
        {
            return;
        }

        var itemType = GetType(itemObj);

        if (itemType == DataType.Array)
        {
            HandleNestedArray(branch, name, itemObj, root, draft);
            return;
        }

        if (itemType == DataType.Object && TryGetProperties(itemObj, out var props))
        {
            AddObjectChildren(branch, props, root, draft);
            return;
        }

        if (TryHandleReference(name, itemObj, root, draft, out var refNode))
        {
            AddReferenceChildren(branch, refNode);
            return;
        }

        if (itemObj.TryGetPropertyValue("type", out _))
        {
            AddPrimitiveArray(branch, name, itemType);
        }
    }

    private static void HandleNestedArray(SemanticBranchNode branch, string name, JsonObject itemObj, JsonObject root, SchemaDraft draft)
    {
        if (TryGetItems(itemObj, out var nested))
        {
            ProcessArrayItems(branch, name, nested, root, draft);
        }
    }

    private static void AddReferenceChildren(SemanticBranchNode branch, SemanticTreeNode refNode)
    {
        if (refNode is SemanticBranchNode refBranch)
        {
            foreach (var child in refBranch.Children)
            {
                branch.AddChild(child);
            }
        }
        else
        {
            branch.AddChild(refNode);
        }
    }

    private static void AddPrimitiveArray(SemanticBranchNode branch, string name, DataType type) => branch.AddChild(new SemanticLeafNode(name, type, string.Empty));

    private static DataType GetType(JsonObject obj)
    {
        if (!obj.TryGetPropertyValue("type", out var typeNode))
        {
            return DataType.String;
        }

        return typeNode!.ToString() switch
        {
            "object" => DataType.Object,
            "array" => DataType.Array,
            "string" => DataType.String,
            "integer" => DataType.Integer,
            "number" => DataType.Number,
            "boolean" => DataType.Boolean,
            _ => DataType.String
        };
    }

    private static SchemaDraft GetSchemaDraft(JsonObject schema)
    {
        if (!schema.TryGetPropertyValue("$schema", out var schemaNode) || schemaNode == null)
        {
            return SchemaDraft.Draft202012;
        }

        var raw = schemaNode.GetValue<string>().Trim();

        return raw switch
        {
            Draft7Schema or Draft7SchemaHttps => SchemaDraft.Draft7,
            Draft201909Schema => SchemaDraft.Draft201909,
            Draft202012Schema => SchemaDraft.Draft202012,
            _ => SchemaDraft.Draft202012
        };
    }

    private static string GetPreferredDefinitionsProperty(SchemaDraft draft)
    {
        return draft == SchemaDraft.Draft7 ? DefinitionsProperty : DefsProperty;
    }

    private enum SchemaDraft
    {
        Draft7,
        Draft201909,
        Draft202012
    }
}
