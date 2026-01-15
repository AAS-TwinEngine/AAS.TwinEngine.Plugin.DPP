using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

public class SemanticTreeResponseBuilderTests
{
    private readonly IResponseLeafNodeProcessor _responseLeafNodeProcessor;
    private readonly IResponseBranchNodeProcessor _responseBranchNodeProcessor;
    private readonly SemanticTreeResponseBuilder _sut;
    private const string IndexPrefix = "[Index]:";

    public SemanticTreeResponseBuilderTests()
    {
        var semanticsOptions = Options.Create(new Semantics { IndexContextPrefix = IndexPrefix });
        _responseLeafNodeProcessor = Substitute.For<IResponseLeafNodeProcessor>();
        _responseBranchNodeProcessor = Substitute.For<IResponseBranchNodeProcessor>();
        _sut = new SemanticTreeResponseBuilder(semanticsOptions, _responseLeafNodeProcessor, _responseBranchNodeProcessor);
    }

    #region BuildResponse Tests

    [Fact]
    public void BuildResponse_WithNullRequestNode_ThrowsArgumentNullException()
    {
        var responseNode = new SemanticLeafNode("test", DataType.String, "value");
        var mapping = new Dictionary<string, string>();

        Assert.Throws<ArgumentNullException>(() => _sut.BuildResponse(null!, responseNode, mapping));
    }

    [Fact]
    public void BuildResponse_WithNullMapping_ThrowsArgumentNullException()
    {
        var requestNode = new SemanticLeafNode("test", DataType.String, string.Empty);
        var responseNode = new SemanticLeafNode("test", DataType.String, "value");

        Assert.Throws<ArgumentNullException>(() =>
            _sut.BuildResponse(requestNode, responseNode, null!));
    }

    [Fact]
    public void BuildResponse_WithNullResponseNode_ReturnsRequestNodeUnmodified()
    {
        var requestNode = new SemanticLeafNode("Product.Name", DataType.String, "OriginalValue");
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, null, mapping);

        Assert.Equal("OriginalValue", ((SemanticLeafNode)result).Value);
        _responseLeafNodeProcessor.DidNotReceive().FillLeafNode(Arg.Any<SemanticLeafNode>(), Arg.Any<SemanticTreeNode>(), Arg.Any<Dictionary<string, string>>());
    }

    [Fact]
    public void BuildResponse_WithLeafNode_CallsLeafNodeFiller()
    {
        var requestNode = new SemanticLeafNode("Product.Name", DataType.String, string.Empty);
        var responseNode = new SemanticLeafNode("ProductName", DataType.String, "Laptop");
        var mapping = new Dictionary<string, string>
        {
            { "Product.Name", "ProductName" }
        };

        _sut.BuildResponse(requestNode, responseNode, mapping);

        _responseLeafNodeProcessor.Received(1).FillLeafNode(requestNode, responseNode, mapping);
    }

    [Fact]
    public void BuildResponse_WithBranchNode_CallsBranchNodeFiller()
    {
        var requestNode = new SemanticBranchNode("Product", DataType.Object);
        var responseNode = new SemanticBranchNode("Product", DataType.Object);
        var mapping = new Dictionary<string, string>
        {
            { "Product", "Product" }
        };

        _sut.BuildResponse(requestNode, responseNode, mapping);

        _responseBranchNodeProcessor.Received(1).FillBranchNode(requestNode, responseNode, mapping);
    }

    #endregion

    #region RemoveIndexPrefix Tests

    [Fact]
    public void BuildResponse_RemovesIndexPrefixFromLeafNode()
    {
        var requestNode = new SemanticLeafNode($"Product{IndexPrefix}0", DataType.String, "Laptop");
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, null, mapping);

        Assert.Equal("Product", result.SemanticId);
    }

    [Fact]
    public void BuildResponse_RemovesIndexPrefixFromBranchNode()
    {
        var requestNode = new SemanticBranchNode($"Items{IndexPrefix}0", DataType.Array);
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, null, mapping);

        Assert.Equal("Items", result.SemanticId);
    }

    [Fact]
    public void BuildResponse_RemovesIndexPrefixFromNestedNodes()
    {
        var requestNode = new SemanticBranchNode($"Order{IndexPrefix}0", DataType.Object);
        var itemsBranch = new SemanticBranchNode($"Items{IndexPrefix}0", DataType.Array);
        var nameLeaf = new SemanticLeafNode($"Name{IndexPrefix}0", DataType.String, "Product");
        itemsBranch.AddChild(nameLeaf);
        requestNode.AddChild(itemsBranch);
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, null, mapping);

        Assert.Equal("Order", result.SemanticId);
        var items = Assert.IsType<SemanticBranchNode>(((SemanticBranchNode)result).Children[0]);
        Assert.Equal("Items", items.SemanticId);
        var name = Assert.IsType<SemanticLeafNode>(items.Children[0]);
        Assert.Equal("Name", name.SemanticId);
    }

    [Fact]
    public void BuildResponse_WithoutIndexPrefix_LeavesSemanticIdUnchanged()
    {
        var requestNode = new SemanticLeafNode("Product", DataType.String, "Laptop");
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, null, mapping);

        Assert.Equal("Product", result.SemanticId);
    }

    [Fact]
    public void BuildResponse_RemovesOnlyFirstIndexPrefix()
    {
        var requestNode = new SemanticLeafNode($"Product{IndexPrefix}0.Items{IndexPrefix}1", DataType.String, "value");
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, null, mapping);

        Assert.Equal("Product", result.SemanticId);
    }

    #endregion

    [Fact]
    public void BuildResponse_CompleteFlow_FillsAndCleansUp()
    {
        var requestNode = new SemanticBranchNode($"Product{IndexPrefix}0", DataType.Object);
        var nameLeaf = new SemanticLeafNode($"Name{IndexPrefix}0", DataType.String, string.Empty);
        requestNode.AddChild(nameLeaf);
        var responseNode = new SemanticBranchNode("Product", DataType.Object);
        var mapping = new Dictionary<string, string>
        {
            { "Product", "Product" }
        };

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        _responseBranchNodeProcessor.Received(1).FillBranchNode(requestNode, responseNode, mapping);
        Assert.Equal("Product", result.SemanticId);
        var name = Assert.IsType<SemanticLeafNode>(((SemanticBranchNode)result).Children[0]);
        Assert.Equal("Name", name.SemanticId);
    }

    [Fact]
    public void BuildResponse_WithMultipleLevels_ProcessesAllLevels()
    {
        var requestNode = new SemanticBranchNode($"Order{IndexPrefix}0", DataType.Object);
        var customerBranch = new SemanticBranchNode($"Customer{IndexPrefix}0", DataType.Object);
        var nameLeaf = new SemanticLeafNode($"Name{IndexPrefix}0", DataType.String, string.Empty);
        customerBranch.AddChild(nameLeaf);
        requestNode.AddChild(customerBranch);
        var responseNode = new SemanticBranchNode("Order", DataType.Object);
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        Assert.Equal("Order", result.SemanticId);
        var customer = Assert.IsType<SemanticBranchNode>(((SemanticBranchNode)result).Children[0]);
        Assert.Equal("Customer", customer.SemanticId);
        var name = Assert.IsType<SemanticLeafNode>(customer.Children[0]);
        Assert.Equal("Name", name.SemanticId);
    }
}
