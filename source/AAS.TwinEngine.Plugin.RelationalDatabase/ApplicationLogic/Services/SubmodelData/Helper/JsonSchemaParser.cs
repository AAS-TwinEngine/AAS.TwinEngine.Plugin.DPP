using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

public static class JsonSchemaParser
{
    private const string DefinitionsPath = "#/definitions/";

    public static SemanticTreeNode ParseJsonSchema(JsonSchema schema, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return BuildSemanticTree(schema, logger);
    }

    private static SemanticTreeNode BuildSemanticTree(JsonSchema schema, ILogger logger)
    {
        var properties = schema.GetKeyword<PropertiesKeyword>();

        if (properties == null || !properties.Properties.Any())
        {
            logger.LogError("Schema does not contain any properties");
            throw new BadRequestException("Schema must contain at least one property.");
        }

        var rootProperty = properties.Properties.First();
        var definitions = schema.GetKeyword<DefinitionsKeyword>();

        return ConvertPropertyToNode(rootProperty.Key, rootProperty.Value, definitions);
    }

    private static SemanticTreeNode ConvertPropertyToNode(string propertyName, JsonSchema propertySchema, DefinitionsKeyword? definitions)
    {
        var propertyReference = propertySchema.GetKeyword<RefKeyword>();
        if (propertyReference != null)
        {
            return ResolveReference(propertyName, propertyReference, definitions);
        }

        var propertyType = propertySchema.GetKeyword<TypeKeyword>();
        if (propertyType == null)
        {
            return CreateLeafNode(propertyName, DataType.String);
        }

        var dataType = MapSchemaTypeToDataType(propertyType);

        if (IsComplexType(dataType))
        {
            return CreateBranchNode(propertyName, dataType, propertySchema, definitions);
        }

        return CreateLeafNode(propertyName, dataType);
    }

    private static SemanticTreeNode ResolveReference(string propertyName, RefKeyword schemaReference, DefinitionsKeyword? definitions)
    {
        var definitionKey = ExtractDefinitionKey(schemaReference);

        if (!TryGetDefinition(definitions, definitionKey, out var definitionSchema))
        {
            return CreateLeafNode(propertyName, DataType.Unknown);
        }

        var typeDefinition = definitionSchema!.GetKeyword<TypeKeyword>();
        if (typeDefinition == null)
        {
            return CreateLeafNode(propertyName, DataType.String);
        }

        var dataType = MapSchemaTypeToDataType(typeDefinition);

        if (IsComplexType(dataType))
        {
            return CreateBranchNode(propertyName, dataType, definitionSchema, definitions);
        }

        return CreateLeafNode(propertyName, dataType);
    }

    private static string ExtractDefinitionKey(RefKeyword schemaReference) => schemaReference.Reference.ToString().Replace(DefinitionsPath, string.Empty, StringComparison.Ordinal);

    private static bool TryGetDefinition(DefinitionsKeyword? definitions, string definitionKey, out JsonSchema? definitionSchema)
    {
        definitionSchema = null;

        if (definitions?.Definitions == null)
        {
            return false;
        }

        return definitions.Definitions.TryGetValue(definitionKey, out definitionSchema);
    }

    private static SemanticBranchNode CreateBranchNode(string propertyName, DataType dataType, JsonSchema schema, DefinitionsKeyword? definitions)
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

    private static void AddChildPropertiesFromObject(SemanticBranchNode parentBranch, JsonSchema schema, DefinitionsKeyword? definitions)
    {
        var schemaProperties = schema.GetKeyword<PropertiesKeyword>();

        if (schemaProperties == null)
        {
            return;
        }

        foreach (var property in schemaProperties.Properties)
        {
            var childNode = ConvertPropertyToNode(property.Key, property.Value, definitions);
            parentBranch.AddChild(childNode);
        }
    }

    private static void AddChildPropertiesFromArray(SemanticBranchNode parentBranch, JsonSchema schema, DefinitionsKeyword? definitions)
    {
        var schemaItem = schema.GetKeyword<ItemsKeyword>();

        if (schemaItem?.SingleSchema != null)
        {
            var itemNode = ConvertPropertyToNode("item", schemaItem.SingleSchema, definitions);
            parentBranch.AddChild(itemNode);
            return;
        }

        AddChildPropertiesFromObject(parentBranch, schema, definitions);
    }

    private static SemanticLeafNode CreateLeafNode(string propertyName, DataType dataType) => new(propertyName, dataType, string.Empty);

    private static bool IsComplexType(DataType dataType) => dataType is DataType.Object or DataType.Array;

    private static DataType MapSchemaTypeToDataType(TypeKeyword typeDefinition)
    {
        var schemaType = typeDefinition.Type;

        return schemaType switch
        {
            _ when schemaType.HasFlag(SchemaValueType.Object) => DataType.Object,
            _ when schemaType.HasFlag(SchemaValueType.Array) => DataType.Array,
            _ when schemaType.HasFlag(SchemaValueType.String) => DataType.String,
            _ when schemaType.HasFlag(SchemaValueType.Integer) => DataType.Integer,
            _ when schemaType.HasFlag(SchemaValueType.Number) => DataType.Number,
            _ when schemaType.HasFlag(SchemaValueType.Boolean) => DataType.Boolean,
            _ => DataType.String
        };
    }
}
