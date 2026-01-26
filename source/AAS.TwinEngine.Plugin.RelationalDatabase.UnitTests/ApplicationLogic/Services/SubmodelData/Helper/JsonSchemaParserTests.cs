using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData.Helper;

public class JsonSchemaParserTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void ParseJsonSchema_NullSchema_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JsonSchemaParser.ParseJsonSchema(null!, _logger));
    }

    [Fact]
    public void ParseJsonSchema_EmptySchema_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder().Build();

        Assert.Throws<BadRequestException>(() => JsonSchemaParser.ParseJsonSchema(schema, _logger));
    }

    [Fact]
    public void ParseJsonSchema_SchemaWithNoProperties_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<BadRequestException>(() => JsonSchemaParser.ParseJsonSchema(schema, _logger));
    }

    [Fact]
    public void ParseJsonSchema_SchemaWithEmptyProperties_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>())
            .Build();

        Assert.Throws<BadRequestException>(() => JsonSchemaParser.ParseJsonSchema(schema, _logger));
    }

    [Fact]
    public void ParseJsonSchema_StringProperty_ReturnsLeafNodeWithStringType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("name", leafNode.SemanticId);
        Assert.Equal(DataType.String, leafNode.DataType);
        Assert.Equal(string.Empty, leafNode.Value);
    }

    [Fact]
    public void ParseJsonSchema_IntegerProperty_ReturnsLeafNodeWithIntegerType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("age", leafNode.SemanticId);
        Assert.Equal(DataType.Integer, leafNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_NumberProperty_ReturnsLeafNodeWithNumberType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["price"] = new JsonSchemaBuilder().Type(SchemaValueType.Number).Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("price", leafNode.SemanticId);
        Assert.Equal(DataType.Number, leafNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_BooleanProperty_ReturnsLeafNodeWithBooleanType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["isActive"] = new JsonSchemaBuilder().Type(SchemaValueType.Boolean).Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("isActive", leafNode.SemanticId);
        Assert.Equal(DataType.Boolean, leafNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_PropertyWithoutType_ReturnsLeafNodeWithStringTypeAsDefault()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["noType"] = new JsonSchemaBuilder().Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("noType", leafNode.SemanticId);
        Assert.Equal(DataType.String, leafNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ObjectProperty_ReturnsBranchNodeWithObjectType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["person"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                    })
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("person", branchNode.SemanticId);
        Assert.Equal(DataType.Object, branchNode.DataType);
        Assert.Single(branchNode.Children);
    }

    [Fact]
    public void ParseJsonSchema_NestedObjectProperty_ReturnsCorrectStructure()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["person"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                        ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build()
                    })
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("person", branchNode.SemanticId);
        Assert.Equal(2, branchNode.Children.Count);

        var nameChild = branchNode.Children.First(c => c.SemanticId == "name");
        var ageChild = branchNode.Children.First(c => c.SemanticId == "age");

        Assert.IsType<SemanticLeafNode>(nameChild);
        Assert.Equal(DataType.String, nameChild.DataType);

        Assert.IsType<SemanticLeafNode>(ageChild);
        Assert.Equal(DataType.Integer, ageChild.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ObjectWithNoChildProperties_ReturnsBranchNodeWithNoChildren()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["emptyObject"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("emptyObject", branchNode.SemanticId);
        Assert.Equal(DataType.Object, branchNode.DataType);
        Assert.Empty(branchNode.Children);
    }

    [Fact]
    public void ParseJsonSchema_ArrayProperty_ReturnsBranchNodeWithArrayType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["items"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("items", branchNode.SemanticId);
        Assert.Equal(DataType.Array, branchNode.DataType);
        Assert.Single(branchNode.Children);
        Assert.Equal("item", branchNode.Children.First().SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfObjects_ReturnsCorrectStructure()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["users"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder()
                        .Type(SchemaValueType.Object)
                        .Properties(new Dictionary<string, JsonSchema>
                        {
                            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                        })
                        .Build())
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("users", branchNode.SemanticId);
        Assert.Equal(DataType.Array, branchNode.DataType);

        var itemChild = Assert.IsType<SemanticBranchNode>(branchNode.Children.First());
        Assert.Equal("item", itemChild.SemanticId);
        Assert.Equal(DataType.Object, itemChild.DataType);
        Assert.Single(itemChild.Children);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithNoItems_ReturnsBranchNodeWithNoChildren()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["emptyArray"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("emptyArray", branchNode.SemanticId);
        Assert.Equal(DataType.Array, branchNode.DataType);
        Assert.Empty(branchNode.Children);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceToDefinition_ResolvesCorrectly()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Definitions(new Dictionary<string, JsonSchema>
            {
                ["address"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["street"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                    })
                    .Build()
            })
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["homeAddress"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/address")
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("homeAddress", branchNode.SemanticId);
        Assert.Equal(DataType.Object, branchNode.DataType);
        Assert.Single(branchNode.Children);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceToNonExistentDefinition_ReturnsLeafNodeWithUnknownType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["missingRef"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/nonexistent")
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("missingRef", leafNode.SemanticId);
        Assert.Equal(DataType.Unknown, leafNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceToDefinitionWithoutType_ReturnsLeafNodeWithStringType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Definitions(new Dictionary<string, JsonSchema>
            {
                ["noTypeDefinition"] = new JsonSchemaBuilder().Build()
            })
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["refWithoutType"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/noTypeDefinition")
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("refWithoutType", leafNode.SemanticId);
        Assert.Equal(DataType.String, leafNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceToStringDefinition_ReturnsLeafNode()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Definitions(new Dictionary<string, JsonSchema>
            {
                ["stringType"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.String)
                    .Build()
            })
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["stringRef"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/stringType")
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("stringRef", leafNode.SemanticId);
        Assert.Equal(DataType.String, leafNode.DataType);
    }


    [Fact]
    public void ParseJsonSchema_DeeplyNestedSchema_ReturnsCorrectStructure()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["level1"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["level2"] = new JsonSchemaBuilder()
                            .Type(SchemaValueType.Object)
                            .Properties(new Dictionary<string, JsonSchema>
                            {
                                ["level3"] = new JsonSchemaBuilder()
                                    .Type(SchemaValueType.String)
                                    .Build()
                            })
                            .Build()
                    })
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var level1 = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("level1", level1.SemanticId);

        var level2 = Assert.IsType<SemanticBranchNode>(level1.Children.First());
        Assert.Equal("level2", level2.SemanticId);

        var level3 = Assert.IsType<SemanticLeafNode>(level2.Children.First());
        Assert.Equal("level3", level3.SemanticId);
        Assert.Equal(DataType.String, level3.DataType);
    }

    [Fact]
    public void ParseJsonSchema_MixedPropertyTypes_ReturnsCorrectStructure()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["root"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["stringProp"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                        ["intProp"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build(),
                        ["boolProp"] = new JsonSchemaBuilder().Type(SchemaValueType.Boolean).Build(),
                        ["numberProp"] = new JsonSchemaBuilder().Type(SchemaValueType.Number).Build(),
                        ["objectProp"] = new JsonSchemaBuilder()
                            .Type(SchemaValueType.Object)
                            .Properties(new Dictionary<string, JsonSchema>
                            {
                                ["nested"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                            })
                            .Build(),
                        ["arrayProp"] = new JsonSchemaBuilder()
                            .Type(SchemaValueType.Array)
                            .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                            .Build()
                    })
                    .Build()
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var rootNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("root", rootNode.SemanticId);
        Assert.Equal(6, rootNode.Children.Count);

        var stringChild = rootNode.Children.First(c => c.SemanticId == "stringProp");
        Assert.Equal(DataType.String, stringChild.DataType);

        var intChild = rootNode.Children.First(c => c.SemanticId == "intProp");
        Assert.Equal(DataType.Integer, intChild.DataType);

        var boolChild = rootNode.Children.First(c => c.SemanticId == "boolProp");
        Assert.Equal(DataType.Boolean, boolChild.DataType);

        var numberChild = rootNode.Children.First(c => c.SemanticId == "numberProp");
        Assert.Equal(DataType.Number, numberChild.DataType);

        var objectChild = Assert.IsType<SemanticBranchNode>(rootNode.Children.First(c => c.SemanticId == "objectProp"));
        Assert.Equal(DataType.Object, objectChild.DataType);

        var arrayChild = Assert.IsType<SemanticBranchNode>(rootNode.Children.First(c => c.SemanticId == "arrayProp"));
        Assert.Equal(DataType.Array, arrayChild.DataType);
    }


    [Fact]
    public void ParseJsonSchema_EmptySchema_LogsError()
    {
        var schema = new JsonSchemaBuilder().Build();

        Assert.Throws<BadRequestException>(() => JsonSchemaParser.ParseJsonSchema(schema, _logger));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Schema does not contain any properties")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

}
