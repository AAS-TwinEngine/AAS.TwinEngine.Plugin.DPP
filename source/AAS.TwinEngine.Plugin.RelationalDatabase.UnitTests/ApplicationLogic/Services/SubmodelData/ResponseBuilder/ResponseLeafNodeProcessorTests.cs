using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseLeafNodeProcessorTests
{
    private readonly IResponseSemanticTreeNodeResolver _responseSemanticTreeNodeResolver;
    private readonly IResponseLeafNodeProcessor _sut;

    public ResponseLeafNodeProcessorTests()
    {
        _responseSemanticTreeNodeResolver = Substitute.For<IResponseSemanticTreeNodeResolver>();
        _sut = new ResponseLeafNodeProcessor(_responseSemanticTreeNodeResolver);
    }

    #region FillLeafNode Tests

    [Fact]
    public void FillLeafNode_WithMatchingValue_SetsValue()
    {
        var requestLeaf = new SemanticLeafNode("Product.Name", DataType.String, string.Empty);
        var responseLeaf = new SemanticLeafNode("ProductName", DataType.String, "Laptop");
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        responseTree.AddChild(responseLeaf);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Name", new ColumnMapping(string.Empty, "ProductName") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Name", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductName"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductName")
            .Returns([responseLeaf]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal("Laptop", requestLeaf.Value);
    }

    [Fact]
    public void FillLeafNode_WithNoColumnMapping_SetsEmptyValue()
    {
        var requestLeaf = new SemanticLeafNode("Product.Name", DataType.String, "OldValue");
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>();
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Name", columnMapping).Returns((ColumnMapping?)null);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal(string.Empty, requestLeaf.Value);
    }

    [Fact]
    public void FillLeafNode_WithEmptyColumnName_SetsEmptyValue()
    {
        var requestLeaf = new SemanticLeafNode("Product.Name", DataType.String, "OldValue");
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Name", new ColumnMapping(string.Empty, string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Name", columnMapping).Returns(new ColumnMapping(string.Empty, string.Empty));

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal(string.Empty, requestLeaf.Value);
    }

    [Fact]
    public void FillLeafNode_WithNoMatchingLeaf_SetsEmptyValue()
    {
        var requestLeaf = new SemanticLeafNode("Product.Name", DataType.String, string.Empty);
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Name", new ColumnMapping(string.Empty, "ProductName") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Name", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductName"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductName")
            .Returns([]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal(string.Empty, requestLeaf.Value);
    }

    [Fact]
    public void FillLeafNode_WithMultipleMatches_UsesFirstMatch()
    {
        var requestLeaf = new SemanticLeafNode("Product.Name", DataType.String, string.Empty);
        var responseLeaf1 = new SemanticLeafNode("ProductName", DataType.String, "Laptop");
        var responseLeaf2 = new SemanticLeafNode("ProductName", DataType.String, "Mouse");
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Name", new ColumnMapping(string.Empty, "ProductName") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Name", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductName"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductName")
            .Returns([responseLeaf1, responseLeaf2]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal("Laptop", requestLeaf.Value);
    }

    [Fact]
    public void FillLeafNode_WithNullResponseValue_SetsEmptyValue()
    {
        var requestLeaf = new SemanticLeafNode("Product.Name", DataType.String, "OldValue");
        var responseLeaf = new SemanticLeafNode("ProductName", DataType.String, null!);
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        responseTree.AddChild(responseLeaf);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Name", new ColumnMapping(string.Empty, "ProductName") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Name", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductName"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductName")
            .Returns([responseLeaf]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal(string.Empty, requestLeaf.Value);
    }

    [Fact]
    public void FillLeafNode_PreservesDataType()
    {
        var requestLeaf = new SemanticLeafNode("Product.Price", DataType.Number, string.Empty);
        var responseLeaf = new SemanticLeafNode("ProductPrice", DataType.Number, "999.99");
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        responseTree.AddChild(responseLeaf);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Price", new ColumnMapping(string.Empty, "ProductPrice") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Price", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductPrice"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductPrice")
            .Returns([responseLeaf]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal("999.99", requestLeaf.Value);
        Assert.Equal(DataType.Number, requestLeaf.DataType);
    }

    [Fact]
    public void FillLeafNode_WithVeryLongValue_SetsValue()
    {
        var longValue = new string('A', 10000);
        var requestLeaf = new SemanticLeafNode("Product.Description", DataType.String, string.Empty);
        var responseLeaf = new SemanticLeafNode("ProductDescription", DataType.String, longValue);
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        responseTree.AddChild(responseLeaf);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Description", new ColumnMapping(string.Empty, "ProductDescription") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Description", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductDescription"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductDescription")
            .Returns([responseLeaf]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal(longValue, requestLeaf.Value);
        Assert.Equal(10000, requestLeaf.Value.Length);
    }

    [Fact]
    public void FillLeafNode_WithSpecialCharacters_PreservesValue()
    {
        const string SpecialValue = "Test@Value#123$%^&*()";
        var requestLeaf = new SemanticLeafNode("Product.Code", DataType.String, string.Empty);
        var responseLeaf = new SemanticLeafNode("ProductCode", DataType.String, SpecialValue);
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        responseTree.AddChild(responseLeaf);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Code", new ColumnMapping(string.Empty, "ProductCode") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Code", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductCode"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductCode")
            .Returns([responseLeaf]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal(SpecialValue, requestLeaf.Value);
    }

    [Fact]
    public void FillLeafNode_WithBooleanDataType_SetsValue()
    {
        var requestLeaf = new SemanticLeafNode("Product.InStock", DataType.Boolean, string.Empty);
        var responseLeaf = new SemanticLeafNode("ProductInStock", DataType.Boolean, "true");
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        responseTree.AddChild(responseLeaf);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.InStock", new ColumnMapping(string.Empty, "ProductInStock") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.InStock", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductInStock"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductInStock")
            .Returns([responseLeaf]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal("true", requestLeaf.Value);
        Assert.Equal(DataType.Boolean, requestLeaf.DataType);
    }

    [Fact]
    public void FillLeafNode_WithIntegerDataType_SetsValue()
    {
        var requestLeaf = new SemanticLeafNode("Product.Quantity", DataType.Integer, string.Empty);
        var responseLeaf = new SemanticLeafNode("ProductQuantity", DataType.Integer, "42");
        var responseTree = new SemanticBranchNode("Product", DataType.Object);
        responseTree.AddChild(responseLeaf);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Product.Quantity", new ColumnMapping(string.Empty, "ProductQuantity") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Product.Quantity", columnMapping).Returns(new ColumnMapping(string.Empty, "ProductQuantity"));
        _responseSemanticTreeNodeResolver.FindMatchingLeafNodes(responseTree, "ProductQuantity")
            .Returns([responseLeaf]);

        _sut.FillLeafNode(requestLeaf, responseTree, columnMapping);

        Assert.Equal("42", requestLeaf.Value);
        Assert.Equal(DataType.Integer, requestLeaf.DataType);
    }

    #endregion
}
