using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public class JsonSchemaParser(ILogger<JsonSchemaParser> logger, IJsonSchemaValidator jsonSchemaValidator) : IJsonSchemaParser
{
    private const string DefinitionsPrefix = "#/definitions/";

    public SemanticTreeNode ParseJsonSchema(JsonSchema jsonSchema)
    {
        if (jsonSchema == null)
        {
            logger.LogError("Requested schema is null.");
            throw new ArgumentNullException(nameof(jsonSchema));
        }

        jsonSchemaValidator.ValidateRequestSchema(jsonSchema);

        return CreateSemanticTree(jsonSchema);
    }

    private SemanticTreeNode CreateSemanticTree(JsonSchema jsonSchema)
    {
        var propertiesKeyword = jsonSchema.GetKeyword<PropertiesKeyword>();
        if (propertiesKeyword == null || !propertiesKeyword.Properties.Any())
        {
            logger.LogError("Schema does not contain any properties.");
            throw new BadRequestException("Schema must contain at least one property.");
        }

        var rootProperty = propertiesKeyword.Properties.First();
        var definitions = jsonSchema.GetKeyword<DefinitionsKeyword>();
        return ProcessProperty(rootProperty.Key, rootProperty.Value, definitions);
    }

    private SemanticTreeNode ProcessProperty(string schemaPropertyName, JsonSchema property, DefinitionsKeyword definitions)
    {
        var refKeyword = property.GetKeyword<RefKeyword>();
        if (refKeyword != null)
        {
            return HandleReference(schemaPropertyName, refKeyword, definitions);
        }

        var typeKeyword = property.GetKeyword<TypeKeyword>();
        if (typeKeyword == null)
        {
            return new SemanticLeafNode(schemaPropertyName, DataType.String, "");
        }

        var schemaType = GetSchemaType(typeKeyword);

        return schemaType is DataType.Object or DataType.Array
                   ? BuildObjectNode(schemaPropertyName, schemaType, property, definitions)
                   : new SemanticLeafNode(schemaPropertyName, schemaType, string.Empty);
    }

    private SemanticTreeNode HandleReference(string propertyName, RefKeyword refKeyword, DefinitionsKeyword definitions)
    {
        var definitionKey = refKeyword.Reference.ToString().Replace(DefinitionsPrefix, string.Empty, StringComparison.CurrentCulture);

        if (definitions?.Definitions == null || !definitions.Definitions.TryGetValue(definitionKey, out var def))
        {
            return new SemanticLeafNode(propertyName, DataType.Unknown, string.Empty);
        }

        var defTypeKeyword = def.GetKeyword<TypeKeyword>();
        if (defTypeKeyword == null)
        {
            return new SemanticLeafNode(propertyName, DataType.String, string.Empty);
        }

        var schemaType = GetSchemaType(defTypeKeyword);

        return schemaType is DataType.Object or DataType.Array
                   ? BuildObjectNode(propertyName, schemaType, def, definitions)
                   : new SemanticLeafNode(propertyName, schemaType, string.Empty);
    }

    private SemanticBranchNode BuildObjectNode(string schemaPropertyName, DataType dataType, JsonSchema schema, DefinitionsKeyword definitions)
    {
        var branchNode = new SemanticBranchNode(schemaPropertyName, dataType);

        switch (dataType)
        {
            case DataType.Object:
                AddObjectProperties(branchNode, schema, definitions);
                break;
            case DataType.Array:
                AddArrayItems(branchNode, schema, definitions);
                break;
        }

        return branchNode;
    }

    private void AddObjectProperties(SemanticBranchNode branchNode, JsonSchema schema, DefinitionsKeyword definitions)
    {
        var propertiesKeyword = schema.GetKeyword<PropertiesKeyword>();
        if (propertiesKeyword == null)
        {
            logger.LogInformation("No properties found for object node {NodeName} in Requested Schema", branchNode.SemanticId);
            return;
        }

        foreach (var prop in propertiesKeyword.Properties)
        {
            branchNode.AddChild(ProcessProperty(prop.Key, prop.Value, definitions));
        }
    }

    private void AddArrayItems(SemanticBranchNode branchNode, JsonSchema schema, DefinitionsKeyword definitions)
    {
        var itemsKeyword = schema.GetKeyword<ItemsKeyword>();
        if (itemsKeyword?.SingleSchema != null)
        {
            branchNode.AddChild(ProcessProperty("item", itemsKeyword.SingleSchema, definitions));
            return;
        }

        AddObjectProperties(branchNode, schema, definitions);
    }

    private static DataType GetSchemaType(TypeKeyword typeKeyword)
    {
        var t = typeKeyword.Type;

        return t switch
        {
            _ when t.HasFlag(SchemaValueType.Object) => DataType.Object,
            _ when t.HasFlag(SchemaValueType.Array) => DataType.Array,
            _ when t.HasFlag(SchemaValueType.String) => DataType.String,
            _ when t.HasFlag(SchemaValueType.Integer) => DataType.Integer,
            _ when t.HasFlag(SchemaValueType.Number) => DataType.Number,
            _ when t.HasFlag(SchemaValueType.Boolean) => DataType.Boolean,
            _ => DataType.String
        };
    }
}
