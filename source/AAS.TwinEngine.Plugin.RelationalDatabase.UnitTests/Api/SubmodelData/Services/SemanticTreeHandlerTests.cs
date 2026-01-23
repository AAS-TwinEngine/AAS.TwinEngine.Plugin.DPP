using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.SubmodelData.Services;

public class SemanticTreeHandlerTests
{
    private readonly IJsonSchemaValidator _jsonSchemaValidator;
    private readonly SemanticTreeHandler _sut;
    private readonly JsonSchema _testSchema;

    public SemanticTreeHandlerTests()
    {
        _jsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        _sut = new SemanticTreeHandler(_jsonSchemaValidator);
        _testSchema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
    }

    [Fact]
    public void GetJson_WithLeafNode_ReturnsWrappedJsonObject()
    {
        var leafNode = new SemanticLeafNode("testProperty", DataType.String, "testValue");

        var result = _sut.GetJson(leafNode, _testSchema);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("testProperty"));
        Assert.Equal("testValue", result["testProperty"]?.GetValue<string>());
        _jsonSchemaValidator.Received(1).ValidateResponseContent(Arg.Any<string>(), _testSchema);
    }

    [Fact]
    public void GetJson_WithBranchNode_ReturnsNestedJsonObject()
    {
        var branchNode = new SemanticBranchNode("parent", DataType.Object);
        var childLeaf = new SemanticLeafNode("child", DataType.String, "childValue");
        branchNode.AddChild(childLeaf);

        var result = _sut.GetJson(branchNode, _testSchema);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("parent"));
        var parentObject = result["parent"]?.AsObject();
        Assert.NotNull(parentObject);
        Assert.True(parentObject.ContainsKey("child"));
        Assert.Equal("childValue", parentObject["child"]?.GetValue<string>());
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public void GetJson_WithBooleanLeaf_ParsesCorrectly(string value, bool expected)
    {
        var leafNode = new SemanticLeafNode("boolProp", DataType.Boolean, value);

        var result = _sut.GetJson(leafNode, _testSchema);

        var jsonValue = result["boolProp"];
        Assert.Equal(expected, jsonValue?.GetValue<bool>());
    }

    [Fact]
    public void GetJson_WithBooleanLeaf_InvalidValue_ReturnsAsString()
    {
        var leafNode = new SemanticLeafNode("boolProp", DataType.Boolean, "notABoolean");

        var result = _sut.GetJson(leafNode, _testSchema);

        var jsonValue = result["boolProp"];
        Assert.Equal("notABoolean", jsonValue?.GetValue<string>());
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    [InlineData("-123", -123)]
    public void GetJson_WithIntegerLeaf_ParsesCorrectly(string value, int expected)
    {
        var leafNode = new SemanticLeafNode("intProp", DataType.Integer, value);

        var result = _sut.GetJson(leafNode, _testSchema);

        var jsonValue = result["intProp"];
        Assert.Equal(expected, jsonValue?.GetValue<int>());
    }

    [Fact]
    public void GetJson_WithIntegerLeaf_InvalidValue_ReturnsAsString()
    {
        var leafNode = new SemanticLeafNode("intProp", DataType.Integer, "notAnInteger");

        var result = _sut.GetJson(leafNode, _testSchema);

        var jsonValue = result["intProp"];
        Assert.Equal("notAnInteger", jsonValue?.GetValue<string>());
    }

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData("0.0", 0.0)]
    [InlineData("-123.456", -123.456)]
    public void GetJson_WithNumberLeaf_ParsesCorrectly(string value, double expected)
    {
        var leafNode = new SemanticLeafNode("numProp", DataType.Number, value);

        var result = _sut.GetJson(leafNode, _testSchema);

        var jsonValue = result["numProp"];
        Assert.Equal(expected, jsonValue?.GetValue<double>());
    }

    [Fact]
    public void GetJson_WithNumberLeaf_InvalidValue_ReturnsAsString()
    {
        var leafNode = new SemanticLeafNode("numProp", DataType.Number, "notANumber");

        var result = _sut.GetJson(leafNode, _testSchema);

        var jsonValue = result["numProp"];
        Assert.Equal("notANumber", jsonValue?.GetValue<string>());
    }

    [Fact]
    public void GetJson_WithArrayOfBranches_ReturnsSameSemanticId_CreatesJsonArray()
    {
        var arrayBranch = new SemanticBranchNode("items", DataType.Array);
        var item1 = new SemanticBranchNode("item", DataType.Object);
        item1.AddChild(new SemanticLeafNode("name", DataType.String, "Item1"));
        var item2 = new SemanticBranchNode("item", DataType.Object);
        item2.AddChild(new SemanticLeafNode("name", DataType.String, "Item2"));
        arrayBranch.AddChild(item1);
        arrayBranch.AddChild(item2);

        var result = _sut.GetJson(arrayBranch, _testSchema);

        Assert.NotNull(result);
        var itemsArray = result["items"]?.AsArray();
        Assert.NotNull(itemsArray);
        Assert.Equal(2, itemsArray.Count);
        Assert.Equal("Item1", itemsArray[0]?["name"]?.GetValue<string>());
        Assert.Equal("Item2", itemsArray[1]?["name"]?.GetValue<string>());
    }

    [Fact]
    public void GetJson_WithArrayOfLeaves_SameSemanticId_CreatesJsonArray()
    {
        var arrayBranch = new SemanticBranchNode("tags", DataType.Array);
        var leaf1 = new SemanticLeafNode("tag", DataType.String, "tag1");
        var leaf2 = new SemanticLeafNode("tag", DataType.String, "tag2");
        arrayBranch.AddChild(leaf1);
        arrayBranch.AddChild(leaf2);

        var result = _sut.GetJson(arrayBranch, _testSchema);

        Assert.NotNull(result);
        var tagsArray = result["tags"]?.AsArray();
        Assert.NotNull(tagsArray);
        Assert.Equal(2, tagsArray.Count);
        Assert.Equal("tag1", tagsArray[0]?["tag"]?.GetValue<string>());
        Assert.Equal("tag2", tagsArray[1]?["tag"]?.GetValue<string>());
    }

    [Fact]
    public void GetJson_WithSingleChildInArray_WrapsInArray()
    {
        var arrayBranch = new SemanticBranchNode("items", DataType.Array);
        var singleChild = new SemanticLeafNode("value", DataType.String, "singleValue");
        arrayBranch.AddChild(singleChild);

        var result = _sut.GetJson(arrayBranch, _testSchema);

        Assert.NotNull(result);
        var itemsArray = result["items"]?.AsArray();
        Assert.NotNull(itemsArray);
        Assert.Single(itemsArray);
    }

    [Fact]
    public void GetJson_WithMultipleChildrenSameSemanticId_CreatesArray()
    {
        var parentBranch = new SemanticBranchNode("parent", DataType.Object);
        var child1 = new SemanticLeafNode("duplicate", DataType.String, "value1");
        var child2 = new SemanticLeafNode("duplicate", DataType.String, "value2");
        parentBranch.AddChild(child1);
        parentBranch.AddChild(child2);

        var result = _sut.GetJson(parentBranch, _testSchema);

        Assert.NotNull(result);
        var parentObject = result["parent"]?.AsObject();
        Assert.NotNull(parentObject);
        var duplicateArray = parentObject["duplicate"]?.AsArray();
        Assert.NotNull(duplicateArray);
        Assert.Equal(2, duplicateArray.Count);
        Assert.Equal("value1", duplicateArray[0]?.GetValue<string>());
        Assert.Equal("value2", duplicateArray[1]?.GetValue<string>());
    }

    [Fact]
    public void GetJson_WithNestedBranches_CreatesNestedStructure()
    {
        var rootBranch = new SemanticBranchNode("root", DataType.Object);
        var childBranch = new SemanticBranchNode("child", DataType.Object);
        var leafNode = new SemanticLeafNode("value", DataType.String, "nestedValue");
        childBranch.AddChild(leafNode);
        rootBranch.AddChild(childBranch);

        var result = _sut.GetJson(rootBranch, _testSchema);

        Assert.NotNull(result);
        var rootObject = result["root"]?.AsObject();
        Assert.NotNull(rootObject);
        var childObject = rootObject["child"]?.AsObject();
        Assert.NotNull(childObject);
        Assert.Equal("nestedValue", childObject["value"]?.GetValue<string>());
    }

    [Fact]
    public void GetJson_ValidatesResponse_WithJsonSchemaValidator()
    {
        var leafNode = new SemanticLeafNode("test", DataType.String, "value");

        _sut.GetJson(leafNode, _testSchema);

        _jsonSchemaValidator.Received(1).ValidateResponseContent(
            Arg.Is<string>(s => s.Contains("test") && s.Contains("value")),
            _testSchema);
    }

    [Fact]
    public void GetJson_WhenValidationFails_PropagatesException()
    {
        var leafNode = new SemanticLeafNode("test", DataType.String, "value");
        _jsonSchemaValidator.When(x => x.ValidateResponseContent(Arg.Any<string>(), Arg.Any<JsonSchema>()))
            .Do(_ => throw new InvalidOperationException("Validation failed"));

        Assert.Throws<InvalidOperationException>(() => _sut.GetJson(leafNode, _testSchema));
    }

    [Fact]
    public void GetJson_WithEmptyBranch_ReturnsEmptyObject()
    {
        var emptyBranch = new SemanticBranchNode("empty", DataType.Object);

        var result = _sut.GetJson(emptyBranch, _testSchema);

        Assert.NotNull(result);
        var emptyObject = result["empty"]?.AsObject();
        Assert.NotNull(emptyObject);
        Assert.Empty(emptyObject);
    }

    [Fact]
    public void GetJson_WithMixedDataTypes_HandlesCorrectly()
    {
        var parentBranch = new SemanticBranchNode("mixed", DataType.Object);
        parentBranch.AddChild(new SemanticLeafNode("stringProp", DataType.String, "text"));
        parentBranch.AddChild(new SemanticLeafNode("intProp", DataType.Integer, "42"));
        parentBranch.AddChild(new SemanticLeafNode("boolProp", DataType.Boolean, "true"));
        parentBranch.AddChild(new SemanticLeafNode("numberProp", DataType.Number, "3.14"));

        var result = _sut.GetJson(parentBranch, _testSchema);

        Assert.NotNull(result);
        var mixedObject = result["mixed"]?.AsObject();
        Assert.NotNull(mixedObject);
        Assert.Equal("text", mixedObject["stringProp"]?.GetValue<string>());
        Assert.Equal(42, mixedObject["intProp"]?.GetValue<int>());
        Assert.True(mixedObject["boolProp"]?.GetValue<bool>());
        Assert.Equal(3.14, mixedObject["numberProp"]?.GetValue<double>());
    }

    [Fact]
    public void GetJson_WithMultipleArrays_MergesCorrectly()
    {
        var parentBranch = new SemanticBranchNode("parent", DataType.Object);
        var arrayBranch1 = new SemanticBranchNode("items", DataType.Array);
        arrayBranch1.AddChild(new SemanticLeafNode("value", DataType.String, "item1"));
        var arrayBranch2 = new SemanticBranchNode("items", DataType.Array);
        arrayBranch2.AddChild(new SemanticLeafNode("value", DataType.String, "item2"));
        parentBranch.AddChild(arrayBranch1);
        parentBranch.AddChild(arrayBranch2);

        var result = _sut.GetJson(parentBranch, _testSchema);

        Assert.NotNull(result);
        var parentObject = result["parent"]?.AsObject();
        Assert.NotNull(parentObject);
        var itemsArray = parentObject["items"]?.AsArray();
        Assert.NotNull(itemsArray);
        Assert.Equal(2, itemsArray.Count);
    }
}
