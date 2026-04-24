using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

public static class JsonSchemaParser
{
    public static SemanticTreeNode ParseJsonSchema(JsonSchema schema, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var json = ConvertSchemaToJson(schema);
        return BuildSemanticTree(json, logger);
    }

    private static JsonObject ConvertSchemaToJson(JsonSchema schema)
    {
        return JsonSerializer.SerializeToNode(schema)?.AsObject()
            ?? throw new BadRequestException("Invalid schema format.");
    }

    private static SemanticTreeNode BuildSemanticTree(JsonObject schema, ILogger logger)
    {
        var rootProperty = ExtractRootProperty(schema, logger);
        return ConvertPropertyToNode(rootProperty.Key, rootProperty.Value!, schema);
    }

    private static KeyValuePair<string, JsonNode?> ExtractRootProperty(JsonObject schema, ILogger logger)
    {
        if (!schema.TryGetPropertyValue("properties", out var propsNode) || propsNode is not JsonObject props || props.Count == 0)
        {
            logger.LogError("Schema does not contain any properties");
            throw new BadRequestException("Schema must contain at least one property.");
        }

        return props.First();
    }

    private static SemanticTreeNode ConvertPropertyToNode(string name, JsonNode node, JsonObject root)
    {
        var obj = node.AsObject();

        if (TryHandleReference(name, obj, root, out var refNode))
        {
            return refNode;
        }

        return CreateNodeByType(name, obj, root);
    }

    private static bool TryHandleReference(string name, JsonObject obj, JsonObject root, out SemanticTreeNode result)
    {
        result = null!;

        if (!obj.TryGetPropertyValue("$ref", out var refNode))
        {
            return false;
        }

        result = ResolveReference(name, refNode!.GetValue<string>(), root);
        return true;
    }

    private static SemanticTreeNode CreateNodeByType(string name, JsonObject obj, JsonObject root)
    {
        var type = GetType(obj);

        return type switch
        {
            DataType.Object => BuildObjectNode(name, obj, root),
            DataType.Array => BuildArrayNode(name, obj, root),
            _ => CreateLeafNode(name, type)
        };
    }

    private static SemanticLeafNode CreateLeafNode(string name, DataType type) => new(name, type, string.Empty);

    private static SemanticTreeNode ResolveReference(string name, string reference, JsonObject root)
    {
        if (!reference.StartsWith("#/$defs/", StringComparison.OrdinalIgnoreCase))
        {
            return CreateLeafNode(name, DataType.Unknown);
        }

        var key = reference.Replace("#/$defs/", "", StringComparison.OrdinalIgnoreCase);

        if (!TryGetDefinition(root, key, out var defNode))
        {
            return CreateLeafNode(name, DataType.Unknown);
        }

        return ConvertPropertyToNode(name, defNode!, root);
    }

    private static bool TryGetDefinition(JsonObject root, string key, out JsonNode? defNode)
    {
        defNode = null;

        if (!root.TryGetPropertyValue("$defs", out var defsNode) ||
            defsNode is not JsonObject defs)
        {
            return false;
        }

        return defs.TryGetPropertyValue(key, out defNode);
    }

    private static SemanticBranchNode BuildObjectNode(string name, JsonObject obj, JsonObject root)
    {
        var branch = new SemanticBranchNode(name, DataType.Object);

        if (!TryGetProperties(obj, out var props))
        {
            return branch;
        }

        AddObjectChildren(branch, props, root);
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

    private static void AddObjectChildren(SemanticBranchNode branch, JsonObject props, JsonObject root)
    {
        foreach (var prop in props)
        {
            branch.AddChild(ConvertPropertyToNode(prop.Key, prop.Value!, root));
        }
    }

    private static SemanticBranchNode BuildArrayNode(string name, JsonObject obj, JsonObject root)
    {
        var branch = new SemanticBranchNode(name, DataType.Array);

        if (!TryGetItems(obj, out var itemObj))
        {
            return branch;
        }

        ProcessArrayItems(branch, name, itemObj, root);
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

    private static void ProcessArrayItems(SemanticBranchNode branch, string name, JsonObject itemObj, JsonObject root)
    {
        if (itemObj.Count == 0)
        {
            return;
        }

        var itemType = GetType(itemObj);

        if (itemType == DataType.Array)
        {
            HandleNestedArray(branch, name, itemObj, root);
            return;
        }

        if (itemType == DataType.Object && TryGetProperties(itemObj, out var props))
        {
            AddObjectChildren(branch, props, root);
            return;
        }

        if (TryHandleReference(name, itemObj, root, out var refNode))
        {
            AddReferenceChildren(branch, refNode);
            return;
        }

        if (itemObj.TryGetPropertyValue("type", out _))
        {
            AddPrimitiveArray(branch, name, itemType);
        }
    }

    private static void HandleNestedArray(SemanticBranchNode branch, string name, JsonObject itemObj, JsonObject root)
    {
        if (TryGetItems(itemObj, out var nested))
        {
            ProcessArrayItems(branch, name, nested, root);
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
}
