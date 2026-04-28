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
    public void ParseJsonSchema_NullSchema_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => JsonSchemaParser.ParseJsonSchema(null!, _logger));

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
            .Properties(new Dictionary<string, JsonSchemaBuilder>())
            .Build();

        Assert.Throws<BadRequestException>(() => JsonSchemaParser.ParseJsonSchema(schema, _logger));
    }

    [Fact]
    public void ParseJsonSchema_StringProperty_ReturnsLeafNodeWithStringType()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer)
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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["price"] = new JsonSchemaBuilder().Type(SchemaValueType.Number)
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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["isActive"] = new JsonSchemaBuilder().Type(SchemaValueType.Boolean)
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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["noType"] = new JsonSchemaBuilder()
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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["person"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchemaBuilder>
                    {
                        ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                    })

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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["person"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchemaBuilder>
                    {
                        ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String),
                        ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer)
                    })

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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["emptyObject"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)

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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["items"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))

            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("items", branchNode.SemanticId);
        Assert.Equal(DataType.Array, branchNode.DataType);
        var itemNode = Assert.IsType<SemanticLeafNode>(Assert.Single(branchNode.Children));
        Assert.Equal("item", itemNode.SemanticId);
        Assert.Equal(DataType.String, itemNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayItemsEmptyObject_ReturnsItemLeaf()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "arr": {
                                    "type": "array",
                                    "items": {}
                                }
                            }
                        }
                        """);

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("arr", branch.SemanticId);
        Assert.Equal(DataType.Array, branch.DataType);
        var itemNode = Assert.IsType<SemanticLeafNode>(Assert.Single(branch.Children));
        Assert.Equal("item", itemNode.SemanticId);
        Assert.Equal(DataType.String, itemNode.DataType);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-07/schema#")]
    [InlineData("https://json-schema.org/draft/2020-12/schema")]
    public void ParseJsonSchema_SimpleSchemaAcrossDrafts_ReturnsLeafNode(string draft)
    {
        var schema = JsonSchema.FromText($$"""
                        {
                            "$schema": "{{draft}}",
                            "type": "object",
                            "properties": {
                                "foo": { "type": "string" }
                            }
                        }
                        """);

        var node = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal("foo", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfPrimitive_ReturnsLeaf()
    {
        var schema = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(new Dictionary<string, JsonSchemaBuilder>
                {
                    ["tags"] = new JsonSchemaBuilder()
                                .Type(SchemaValueType.Array)
                                .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))
                })
                .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children.First());
        Assert.Equal("tags", branch.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_MultipleRootProperties_OnlyFirstIsUsed()
    {
        var schema = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(new Dictionary<string, JsonSchemaBuilder>
                {
                    ["first"] = new JsonSchemaBuilder().Type(SchemaValueType.String),
                    ["second"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer)
                })
                .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("first", leaf.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_InvalidRefFormat_ReturnsUnknown()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "x": { "$ref": "#/invalid/A" }
                            }
                        }
                        """);

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfArray_FlattensInner()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "arr": {
                                    "type": "array",
                                    "items": {
                                        "type": "array",
                                        "items": {
                                            "type": "string"
                                        }
                                    }
                                }
                            }
                        }
                        """);

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children.First());
        Assert.Equal("arr", branch.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_RefToPrimitive_ReturnsLeaf()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "x": { "$ref": "#/$defs/A" }
                            },
                            "$defs": {
                                "A": { "type": "string" }
                            }
                        }
                        """);

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfObjects_ReturnsCorrectStructure()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["users"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder()
                        .Type(SchemaValueType.Object)
                        .Properties(new Dictionary<string, JsonSchemaBuilder>
                        {
                            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                        })
                        )

            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("users", branchNode.SemanticId);
        Assert.Equal(DataType.Array, branchNode.DataType);
        Assert.Single(branchNode.Children);
        var nameChild = Assert.IsType<SemanticLeafNode>(branchNode.Children.First());
        Assert.Equal("name", nameChild.SemanticId);
        Assert.Equal(DataType.String, nameChild.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithoutItemsButWithProperties_ParsesChildrenFromArrayNode()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["users"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Properties(new Dictionary<string, JsonSchemaBuilder>
                    {
                        ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String),
                        ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer)
                    })
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("users", branchNode.SemanticId);
        Assert.Equal(DataType.Array, branchNode.DataType);
        Assert.Equal(2, branchNode.Children.Count);
        Assert.Equal(DataType.String, branchNode.Children.First(c => c.SemanticId == "name").DataType);
        Assert.Equal(DataType.Integer, branchNode.Children.First(c => c.SemanticId == "age").DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithNoItems_ReturnsBranchNodeWithNoChildren()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["emptyArray"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)

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
            .Schema("http://json-schema.org/draft-07/schema#")
            .Type(SchemaValueType.Object)
            .Definitions(new Dictionary<string, JsonSchemaBuilder>
            {
                ["address"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchemaBuilder>
                    {
                        ["street"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                    })

            })
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["homeAddress"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/address")

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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["missingRef"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/nonexistent")

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
            .Schema("http://json-schema.org/draft-07/schema#")
            .Type(SchemaValueType.Object)
            .Definitions(new Dictionary<string, JsonSchemaBuilder>
            {
                ["noTypeDefinition"] = new JsonSchemaBuilder()
            })
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["refWithoutType"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/noTypeDefinition")

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
            .Schema("http://json-schema.org/draft-07/schema#")
            .Type(SchemaValueType.Object)
            .Definitions(new Dictionary<string, JsonSchemaBuilder>
            {
                ["stringType"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.String)

            })
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["stringRef"] = new JsonSchemaBuilder()
                    .Ref("#/definitions/stringType")

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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["level1"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchemaBuilder>
                    {
                        ["level2"] = new JsonSchemaBuilder()
                            .Type(SchemaValueType.Object)
                            .Properties(new Dictionary<string, JsonSchemaBuilder>
                            {
                                ["level3"] = new JsonSchemaBuilder()
                                    .Type(SchemaValueType.String)

                            })

                    })

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
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["root"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchemaBuilder>
                    {
                        ["stringProp"] = new JsonSchemaBuilder().Type(SchemaValueType.String),
                        ["intProp"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer),
                        ["boolProp"] = new JsonSchemaBuilder().Type(SchemaValueType.Boolean),
                        ["numberProp"] = new JsonSchemaBuilder().Type(SchemaValueType.Number),
                        ["objectProp"] = new JsonSchemaBuilder()
                            .Type(SchemaValueType.Object)
                            .Properties(new Dictionary<string, JsonSchemaBuilder>
                            {
                                ["nested"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                            })
                            ,
                        ["arrayProp"] = new JsonSchemaBuilder()
                            .Type(SchemaValueType.Array)
                            .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))

                    })

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
