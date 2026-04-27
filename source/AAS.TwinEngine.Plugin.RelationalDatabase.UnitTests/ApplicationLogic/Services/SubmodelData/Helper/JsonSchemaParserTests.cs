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
    public void ParseJsonSchema_NoProperties_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<BadRequestException>(() => JsonSchemaParser.ParseJsonSchema(schema, _logger));
    }

    [Fact]
    public void ParseJsonSchema_StringProperty_ReturnsLeaf()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("name", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-07/schema#")]
    [InlineData("https://json-schema.org/draft/2019-09/schema")]
    [InlineData("https://json-schema.org/draft/2020-12/schema")]
    public void ParseJsonSchema_SimpleSchemaAcrossDrafts_ReturnsLeafNode(string draft)
    {
        var schema = JsonSchema.FromText($@"{{
            ""$schema"": ""{draft}"",
            ""type"": ""object"",
            ""properties"": {{
                ""foo"": {{ ""type"": ""string"" }}
            }}
        }}");

        var node = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal("foo", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ObjectProperty_ReturnsBranch()
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

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("person", branch.SemanticId);
        Assert.Single(branch.Children);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfObjects_Flattened()
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
                        }))
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children.First());

        Assert.Equal("users", branch.SemanticId);
        Assert.Equal("name", child.SemanticId);
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

        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithRef_Flattened()
    {
        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""items"": {
                    ""type"": ""array"",
                    ""items"": { ""$ref"": ""#/$defs/item"" }
                }
            },
            ""$defs"": {
                ""item"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""val"": { ""type"": ""integer"" }
                    }
                }
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children.First());

        Assert.Equal("val", child.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_Draft7ArrayWithDefinitionsRef_ReturnsBranchNodeWithLeafChild()
    {
        var schema = JsonSchema.FromText(@"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""items"": {
                    ""type"": ""array"",
                    ""items"": { ""$ref"": ""#/definitions/ItemDef"" }
                }
            },
            ""definitions"": {
                ""ItemDef"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""value"": { ""type"": ""integer"" }
                    }
                }
            }
        }");

        var node = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("items", branch.SemanticId);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("value", child.SemanticId);
        Assert.Equal(DataType.Integer, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_Draft7SchemaWithDefsKeyword_ReturnsLeafNode()
    {
        var schema = JsonSchema.FromText(@"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""item"": { ""$ref"": ""#/$defs/ItemDef"" }
            },
            ""$defs"": {
                ""ItemDef"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""value"": { ""type"": ""string"" }
                    }
                }
            }
        }");

        var node = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("value", child.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_Draft202012SchemaWithDefinitionsKeyword_ReturnsLeafNode()
    {
        var schema = JsonSchema.FromText(@"{
            ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
            ""type"": ""object"",
            ""properties"": {
                ""item"": { ""$ref"": ""#/definitions/ItemDef"" }
            },
            ""definitions"": {
                ""ItemDef"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""value"": { ""type"": ""integer"" }
                    }
                }
            }
        }");

        var node = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("value", child.SemanticId);
        Assert.Equal(DataType.Integer, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_InvalidRef_ReturnsUnknown()
    {
        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""bad"": { ""$ref"": ""#/$defs/missing"" }
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_DeepNestedDefs_Resolves()
    {
        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""root"": { ""$ref"": ""#/$defs/A"" }
            },
            ""$defs"": {
                ""A"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""child"": { ""$ref"": ""#/$defs/B"" }
                    }
                },
                ""B"": {
                    ""type"": ""string""
                }
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children.First());

        Assert.Equal("child", child.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_MissingType_DefaultsToString()
    {
        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""unknown"": {}
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithoutItems_ReturnsEmptyBranch()
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

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Empty(branch.Children);
    }

    [Fact]
    public void ParseJsonSchema_ObjectWithoutProperties_ReturnsEmptyBranch()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["obj"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
            })
            .Build();

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Empty(branch.Children);
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
    public void ParseJsonSchema_EmptyDefs_RefReturnsUnknown()
    {
        var schema = JsonSchema.FromText(@"{
        ""type"": ""object"",
        ""properties"": {
            ""x"": { ""$ref"": ""#/$defs/A"" }
        },
            ""$defs"": {}
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_InvalidRefFormat_ReturnsUnknown()
    {
        var schema = JsonSchema.FromText(@"{
        ""type"": ""object"",
        ""properties"": {
            ""x"": { ""$ref"": ""#/invalid/A"" }
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_DefsNotObject_ReturnsUnknown()
    {
        var schema = JsonSchema.FromText(@"{
        ""type"": ""object"",
        ""properties"": {
            ""x"": { ""$ref"": ""#/$defs/A"" }
            },
            ""$defs"": { }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayItemsEmptyObject_ReturnsEmptyBranch()
    {
        var schema = JsonSchema.FromText(@"{
        ""type"": ""object"",
        ""properties"": {
            ""arr"": {
                ""type"": ""array"",
                ""items"": {}
                }
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Empty(branch.Children);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfArray_FlattensInner()
    {
        var schema = JsonSchema.FromText(@"{
        ""type"": ""object"",
        ""properties"": {
            ""arr"": {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""string""
                        }
                    }
                }
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children.First());

        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_RefToPrimitive_ReturnsLeaf()
    {
        var schema = JsonSchema.FromText(@"{
        ""type"": ""object"",
        ""properties"": {
            ""x"": { ""$ref"": ""#/$defs/A"" }
        },
        ""$defs"": {
            ""A"": { ""type"": ""string"" }
            }
        }");

        var result = JsonSchemaParser.ParseJsonSchema(schema, _logger);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.String, leaf.DataType);
    }
}
