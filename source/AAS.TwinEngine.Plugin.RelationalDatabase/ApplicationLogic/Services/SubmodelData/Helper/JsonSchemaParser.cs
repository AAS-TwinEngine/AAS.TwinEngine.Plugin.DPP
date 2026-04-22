using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

public static class JsonSchemaParser
{
    private const string DefinitionsPath = "#/definitions/";
    private const string DefsPath = "#/$defs/";

    public static SemanticTreeNode ParseJsonSchema(JsonSchema schema, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return BuildSemanticTree(GetRootElement(schema), logger);
    }

    private static SemanticTreeNode BuildSemanticTree(JsonElement schemaRoot, ILogger logger)
    {
        if (!TryGetProperties(schemaRoot, out var propertiesElement))
        {
            logger.LogError("Schema does not contain any properties");
            throw new BadRequestException("Schema must contain at least one property.");
        }

        var rootProperty = propertiesElement.EnumerateObject().First();
        var definitions = GetDefinitionsElement(schemaRoot);

        return ConvertPropertyToNode(rootProperty.Name, rootProperty.Value, definitions);
    }

    private static SemanticTreeNode ConvertPropertyToNode(string propertyName, JsonElement propertySchema, JsonElement? definitions)
    {
        if (TryGetReference(propertySchema, out var reference))
        {
            return ResolveReference(propertyName, reference, definitions);
        }

        if (!TryMapSchemaTypeToDataType(propertySchema, out var dataType))
        {
            return CreateLeafNode(propertyName, DataType.String);
        }

        if (IsComplexType(dataType))
        {
            return CreateBranchNode(propertyName, dataType, propertySchema, definitions);
        }

        return CreateLeafNode(propertyName, dataType);
    }

    private static SemanticTreeNode ResolveReference(string propertyName, string schemaReference, JsonElement? definitions)
    {
        var definitionKey = ExtractDefinitionKey(schemaReference);

        if (!TryGetDefinition(definitions, definitionKey, out var definitionSchema))
        {
            return CreateLeafNode(propertyName, DataType.Unknown);
        }

        if (!TryMapSchemaTypeToDataType(definitionSchema, out var dataType))
        {
            return CreateLeafNode(propertyName, DataType.String);
        }

        if (IsComplexType(dataType))
        {
            return CreateBranchNode(propertyName, dataType, definitionSchema, definitions);
        }

        return CreateLeafNode(propertyName, dataType);
    }

    private static string ExtractDefinitionKey(string schemaReference)
    {
        if (schemaReference.StartsWith(DefinitionsPath, StringComparison.Ordinal))
        {
            return schemaReference[DefinitionsPath.Length..];
        }

        if (schemaReference.StartsWith(DefsPath, StringComparison.Ordinal))
        {
            return schemaReference[DefsPath.Length..];
        }

        return schemaReference;
    }

    private static bool TryGetDefinition(JsonElement? definitions, string definitionKey, out JsonElement definitionSchema)
    {
        definitionSchema = default;

        if (!definitions.HasValue || definitions.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!definitions.Value.TryGetProperty(definitionKey, out var found))
        {
            return false;
        }

        definitionSchema = found;
        return true;
    }

    private static SemanticBranchNode CreateBranchNode(string propertyName, DataType dataType, JsonElement schema, JsonElement? definitions)
    {
        var branchNode = new SemanticBranchNode(propertyName, dataType);

        switch (dataType)
        {
            case DataType.Object:
                AddChildPropertiesFromObject(branchNode, schema, definitions);
                break;

            case DataType.Array:
                AddChildPropertiesFromArray(branchNode, schema, definitions);
                break;
        }

        return branchNode;
    }

    private static void AddChildPropertiesFromObject(SemanticBranchNode parentBranch, JsonElement schema, JsonElement? definitions)
    {
        if (!TryGetProperties(schema, out var propertiesElement))
        {
            return;
        }

        foreach (var property in propertiesElement.EnumerateObject())
        {
            var childNode = ConvertPropertyToNode(property.Name, property.Value, definitions);
            parentBranch.AddChild(childNode);
        }
    }

    private static void AddChildPropertiesFromArray(SemanticBranchNode parentBranch, JsonElement schema, JsonElement? definitions)
    {
        if (schema.TryGetProperty("items", out var itemsElement))
        {
            if (itemsElement.ValueKind == JsonValueKind.Object)
            {
                var itemNode = ConvertPropertyToNode("item", itemsElement, definitions);
                parentBranch.AddChild(itemNode);
                return;
            }

            if (itemsElement.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var itemSchema in itemsElement.EnumerateArray())
                {
                    if (itemSchema.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var itemNode = ConvertPropertyToNode($"item{index}", itemSchema, definitions);
                    parentBranch.AddChild(itemNode);
                    index++;
                }

                if (index > 0)
                {
                    return;
                }
            }
        }

        AddChildPropertiesFromObject(parentBranch, schema, definitions);
    }

    private static SemanticLeafNode CreateLeafNode(string propertyName, DataType dataType) => new(propertyName, dataType, string.Empty);

    private static bool IsComplexType(DataType dataType) => dataType is DataType.Object or DataType.Array;

    private static JsonElement GetRootElement(JsonSchema schema)
    {
        if (schema.Root.Source.ValueKind != JsonValueKind.Undefined)
        {
            return schema.Root.Source;
        }

        var schemaText = JsonSerializer.Serialize(schema);
        using var document = JsonDocument.Parse(schemaText);
        return document.RootElement.Clone();
    }

    private static JsonElement? GetDefinitionsElement(JsonElement schema)
    {
        if (schema.TryGetProperty("definitions", out var definitionsElement) && definitionsElement.ValueKind == JsonValueKind.Object)
        {
            return definitionsElement;
        }

        if (schema.TryGetProperty("$defs", out var defsElement) && defsElement.ValueKind == JsonValueKind.Object)
        {
            return defsElement;
        }

        return null;
    }

    private static bool TryGetReference(JsonElement schema, out string reference)
    {
        reference = string.Empty;

        if (!schema.TryGetProperty("$ref", out var refElement) || refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        reference = refElement.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(reference);
    }

    private static bool TryGetProperties(JsonElement schema, out JsonElement properties)
    {
        properties = default;

        return schema.TryGetProperty("properties", out properties)
               && properties.ValueKind == JsonValueKind.Object
               && properties.EnumerateObject().Any();
    }

    private static bool TryMapSchemaTypeToDataType(JsonElement schema, out DataType dataType)
    {
        dataType = DataType.String;

        if (schema.TryGetProperty("type", out var typeElement))
        {
            if (TryMapTypeElement(typeElement, out dataType))
            {
                return true;
            }
        }

        if (schema.TryGetProperty("properties", out var propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
        {
            dataType = DataType.Object;
            return true;
        }

        if (schema.TryGetProperty("items", out var itemsElement)
            && (itemsElement.ValueKind == JsonValueKind.Object || itemsElement.ValueKind == JsonValueKind.Array))
        {
            dataType = DataType.Array;
            return true;
        }

        return false;
    }

    private static bool TryMapTypeElement(JsonElement typeElement, out DataType dataType)
    {
        dataType = DataType.String;

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return TryMapTypeName(typeElement.GetString(), out dataType);
        }

        if (typeElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var typeNameElement in typeElement.EnumerateArray())
        {
            if (typeNameElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var typeName = typeNameElement.GetString();

            if (string.Equals(typeName, "null", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryMapTypeName(typeName, out dataType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryMapTypeName(string? schemaType, out DataType dataType)
    {
        dataType = DataType.String;

        if (string.IsNullOrWhiteSpace(schemaType))
        {
            return false;
        }

        switch (schemaType)
        {
            case "object":
                dataType = DataType.Object;
                return true;
            case "array":
                dataType = DataType.Array;
                return true;
            case "string":
                dataType = DataType.String;
                return true;
            case "integer":
                dataType = DataType.Integer;
                return true;
            case "number":
                dataType = DataType.Number;
                return true;
            case "boolean":
                dataType = DataType.Boolean;
                return true;
            default:
                return false;
        }
    }
}
