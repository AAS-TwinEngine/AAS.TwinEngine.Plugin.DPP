using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelDataProvider.Helper;

using Microsoft.Extensions.Options;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.SubmodelDataProvider.Helper;

public class JsonResponseParserTests
{
    private readonly IOptions<Semantics> _semanticsOptions;
    private readonly JsonResponseParser _sut;
    private const string IndexPrefix = "_aastwinengine_";

    public JsonResponseParserTests()
    {
        _semanticsOptions = Substitute.For<IOptions<Semantics>>();
        _semanticsOptions.Value.Returns(new Semantics
        {
            IndexContextPrefix = IndexPrefix
        });
        _sut = new JsonResponseParser(_semanticsOptions);
    }

    [Fact]
    public void ParseJson_SimpleStringProperty_ReturnsLeafNode()
    {
        const string json = """{"name": "testValue"}""";

        var result = _sut.ParseJson(json);

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("name", leafNode.SemanticId);
        Assert.Equal(DataType.String, leafNode.DataType);
        Assert.Equal("testValue", leafNode.Value);
    }

    [Fact]
    public void ParseJson_EmptyObject_ReturnsEmptyBranchNode()
    {
        const string json = "{}";

        var result = _sut.ParseJson(json);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(string.Empty, branchNode.SemanticId);
        Assert.Equal(DataType.Unknown, branchNode.DataType);
    }

    [Fact]
    public void ParseJson_NestedStringJson_ParsesCorrectly()
    {
        const string json = """
        "{\"name\": \"nestedValue\"}"
        """;

        var result = _sut.ParseJson(json.Trim());

        var leafNode = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("name", leafNode.SemanticId);
        Assert.Equal("nestedValue", leafNode.Value);
    }

    [Fact]
    public void ParseJson_ObjectWithMultipleProperties_ReturnsBranchWithChildren()
    {
        const string json = """
        {
            "root": {
                "name": "testName",
                "value": "testValue"
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("root", branchNode.SemanticId);
        Assert.Equal(DataType.Object, branchNode.DataType);
        Assert.Equal(2, branchNode.Children.Count);

        var nameChild = branchNode.Children.First(c => c.SemanticId == "name");
        var valueChild = branchNode.Children.First(c => c.SemanticId == "value");

        Assert.IsType<SemanticLeafNode>(nameChild);
        Assert.IsType<SemanticLeafNode>(valueChild);
    }

    [Fact]
    public void ParseJson_NestedObjects_CreatesCorrectHierarchy()
    {
        const string json = """
        {
            "level1": {
                "level2": {
                    "level3": "deepValue"
                }
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var level1 = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("level1", level1.SemanticId);

        var level2 = Assert.IsType<SemanticBranchNode>(level1.Children.First());
        Assert.Equal("level2", level2.SemanticId);

        var level3 = Assert.IsType<SemanticLeafNode>(level2.Children.First());
        Assert.Equal("level3", level3.SemanticId);
        Assert.Equal("deepValue", level3.Value);
    }

    [Fact]
    public void ParseJson_NumberProperty_ReturnsCorrectDataType()
    {
        const string json = """
        {
            "root": {
                "count": 42
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        var countChild = Assert.IsType<SemanticLeafNode>(branchNode.Children.First());
        Assert.Equal("count", countChild.SemanticId);
        Assert.Equal(DataType.Number, countChild.DataType);
        Assert.Equal("42", countChild.Value);
    }

    [Fact]
    public void ParseJson_BooleanTrueProperty_ReturnsCorrectDataType()
    {
        const string json = """
        {
            "root": {
                "isActive": true
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        var boolChild = Assert.IsType<SemanticLeafNode>(branchNode.Children.First());
        Assert.Equal("isActive", boolChild.SemanticId);
        Assert.Equal(DataType.Boolean, boolChild.DataType);
    }

    [Fact]
    public void ParseJson_BooleanFalseProperty_ReturnsCorrectDataType()
    {
        const string json = """
        {
            "root": {
                "isDisabled": false
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        var boolChild = Assert.IsType<SemanticLeafNode>(branchNode.Children.First());
        Assert.Equal(DataType.Boolean, boolChild.DataType);
    }

    [Fact]
    public void ParseJson_NullProperty_ReturnsCorrectDataType()
    {
        const string json = """
        {
            "root": {
                "nullValue": null
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var branchNode = Assert.IsType<SemanticBranchNode>(result);
        var nullChild = Assert.IsType<SemanticLeafNode>(branchNode.Children.First());
        Assert.Equal("nullValue", nullChild.SemanticId);
        Assert.Equal(DataType.Null, nullChild.DataType);
    }

    [Fact]
    public void ParseJson_ArrayWithSingleItem_CreatesNonIndexedBranch()
    {
        const string json = """
        {
            "root": {
                "items": [
                    {"name": "item1"}
                ]
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Single(rootBranch.Children);

        var itemsBranch = Assert.IsType<SemanticBranchNode>(rootBranch.Children.First());
        Assert.Equal("items", itemsBranch.SemanticId);
    }

    [Fact]
    public void ParseJson_ArrayWithMultipleItems_CreatesIndexedBranches()
    {
        const string json = """
        {
            "root": {
                "items": [
                    {"name": "item1"},
                    {"name": "item2"}
                ]
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(2, rootBranch.Children.Count);

        var firstItem = Assert.IsType<SemanticBranchNode>(rootBranch.Children[0]);
        Assert.Equal($"items{IndexPrefix}00", firstItem.SemanticId);

        var secondItem = Assert.IsType<SemanticBranchNode>(rootBranch.Children[1]);
        Assert.Equal($"items{IndexPrefix}01", secondItem.SemanticId);
    }

    [Fact]
    public void ParseJson_ArrayWithThreeItems_CreatesCorrectIndexes()
    {
        const string json = """
        {
            "root": {
                "items": [
                    {"value": "a"},
                    {"value": "b"},
                    {"value": "c"}
                ]
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(3, rootBranch.Children.Count);
        Assert.Equal($"items{IndexPrefix}00", rootBranch.Children[0].SemanticId);
        Assert.Equal($"items{IndexPrefix}01", rootBranch.Children[1].SemanticId);
        Assert.Equal($"items{IndexPrefix}02", rootBranch.Children[2].SemanticId);
    }

    [Fact]
    public void ParseJson_EmptyArray_CreatesNoChildren()
    {
        const string json = """
        {
            "root": {
                "items": []
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Empty(rootBranch.Children);
    }

    [Fact]
    public void ParseJson_MixedPropertyTypes_ParsesAllCorrectly()
    {
        const string json = """
        {
            "root": {
                "stringProp": "text",
                "numberProp": 123,
                "boolProp": true,
                "nullProp": null,
                "objectProp": {
                    "nested": "value"
                }
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(5, rootBranch.Children.Count);
        var stringChild = rootBranch.Children.First(c => c.SemanticId == "stringProp");
        Assert.Equal(DataType.String, stringChild.DataType);
        var numberChild = rootBranch.Children.First(c => c.SemanticId == "numberProp");
        Assert.Equal(DataType.Number, numberChild.DataType);
        var boolChild = rootBranch.Children.First(c => c.SemanticId == "boolProp");
        Assert.Equal(DataType.Boolean, boolChild.DataType);
        var nullChild = rootBranch.Children.First(c => c.SemanticId == "nullProp");
        Assert.Equal(DataType.Null, nullChild.DataType);
        var objectChild = rootBranch.Children.First(c => c.SemanticId == "objectProp");
        Assert.IsType<SemanticBranchNode>(objectChild);
    }

    [Fact]
    public void ParseJson_ArrayOfPrimitives_ParsesCorrectly()
    {
        const string json = """
        {
            "root": {
                "values": [1, 2, 3]
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(3, rootBranch.Children.Count);
    }

    [Fact]
    public void ParseJson_ComplexNestedStructure_ParsesCorrectly()
    {
        const string json = """
        {
            "nameplate": {
                "manufacturer": {
                    "name": "TestCompany",
                    "address": {
                        "street": "123 Main St",
                        "city": "TestCity"
                    }
                },
                "products": [
                    {"id": "P001", "name": "Product1"},
                    {"id": "P002", "name": "Product2"}
                ]
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var nameplate = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("nameplate", nameplate.SemanticId);
        Assert.Equal(DataType.Object, nameplate.DataType);

        var manufacturer = nameplate.Children.FirstOrDefault(c => c.SemanticId == "manufacturer");
        Assert.NotNull(manufacturer);
        Assert.IsType<SemanticBranchNode>(manufacturer);

        var products = nameplate.Children.Where(c => c.SemanticId.StartsWith("products")).ToList();
        Assert.Equal(2, products.Count);
    }

    [Fact]
    public void ParseJson_DeeplyNestedArrays_ParsesCorrectly()
    {
        const string json = """
        {
            "root": {
                "level1": {
                    "items": [
                        {
                            "subItems": [
                                {"value": "deep1"},
                                {"value": "deep2"}
                            ]
                        }
                    ]
                }
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var root = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("root", root.SemanticId);
    }

    [Fact]
    public void ParseJson_DecimalNumber_ParsesCorrectly()
    {
        const string json = """
        {
            "root": {
                "price": 19.99
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        var priceChild = Assert.IsType<SemanticLeafNode>(rootBranch.Children.First());
        Assert.Equal("price", priceChild.SemanticId);
        Assert.Equal(DataType.Number, priceChild.DataType);
        Assert.Equal("19.99", priceChild.Value);
    }

    [Fact]
    public void ParseJson_NegativeNumber_ParsesCorrectly()
    {
        const string json = """
        {
            "root": {
                "temperature": -10
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        var tempChild = Assert.IsType<SemanticLeafNode>(rootBranch.Children.First());
        Assert.Equal("-10", tempChild.Value);
    }

    [Fact]
    public void ParseJson_EmptyString_ParsesCorrectly()
    {
        const string json = """
        {
            "root": {
                "emptyField": ""
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        var emptyChild = Assert.IsType<SemanticLeafNode>(rootBranch.Children.First());
        Assert.Equal("emptyField", emptyChild.SemanticId);
        Assert.Equal(string.Empty, emptyChild.Value);
    }

    [Fact]
    public void ParseJson_SpecialCharactersInString_ParsesCorrectly()
    {
        const string json = """
        {
            "root": {
                "special": "line1\nline2\ttab"
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        var specialChild = Assert.IsType<SemanticLeafNode>(rootBranch.Children.First());
        Assert.Contains("\n", specialChild.Value);
        Assert.Contains("\t", specialChild.Value);
    }

    [Fact]
    public void ParseJson_UnicodeCharacters_ParsesCorrectly()
    {
        const string json = """
        {
            "root": {
                "unicode": "abc"
            }
        }
        """;

        var result = _sut.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        var unicodeChild = Assert.IsType<SemanticLeafNode>(rootBranch.Children.First());
        Assert.Equal("abc", unicodeChild.Value);
    }

    [Fact]
    public void ParseJson_CustomIndexPrefix_UsesConfiguredPrefix()
    {
        const string customPrefix = "_custom_";
        var customOptions = Substitute.For<IOptions<Semantics>>();
        customOptions.Value.Returns(new Semantics { IndexContextPrefix = customPrefix });
        var parser = new JsonResponseParser(customOptions);

        const string json = """
        {
            "root": {
                "items": [
                    {"value": "a"},
                    {"value": "b"}
                ]
            }
        }
        """;

        var result = parser.ParseJson(json);

        var rootBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal($"items{customPrefix}00", rootBranch.Children[0].SemanticId);
        Assert.Equal($"items{customPrefix}01", rootBranch.Children[1].SemanticId);
    }
}
