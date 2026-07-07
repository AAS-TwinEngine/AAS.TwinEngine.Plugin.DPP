using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseBranchNodeProcessorTests
{
    private readonly IResponseSemanticTreeNodeResolver _responseSemanticTreeNodeResolver;
    private readonly IResponseLeafNodeProcessor _responseLeafNodeProcessor;
    private readonly IResponseBranchNodeProcessor _sut;
    private const string IndexPrefix = "[Index]:";

    public ResponseBranchNodeProcessorTests()
    {
        _responseSemanticTreeNodeResolver = Substitute.For<IResponseSemanticTreeNodeResolver>();
        _responseLeafNodeProcessor = Substitute.For<IResponseLeafNodeProcessor>();
        _sut = new ResponseBranchNodeProcessor(_responseSemanticTreeNodeResolver, _responseLeafNodeProcessor);
    }

    #region No Match Strategy Tests

    [Fact]
    public void FillBranchNode_WithNoMatches_SetsAllLeafsToEmpty()
    {
        var requestBranch = new SemanticBranchNode("Customer", DataType.Object);
        var nameLeaf = new SemanticLeafNode("Name", DataType.String, "OldValue");
        requestBranch.AddChild(nameLeaf);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Customer", new ColumnMapping("CustomerData", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Customer", columnMapping).Returns(new ColumnMapping("CustomerData", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "CustomerData")
            .Returns([]);

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        Assert.Equal(string.Empty, nameLeaf.Value);
    }

    [Fact]
    public void FillBranchNode_WithNoMatches_HandlesNestedBranches()
    {
        var requestBranch = new SemanticBranchNode("Order", DataType.Object);
        var customerBranch = new SemanticBranchNode("Customer", DataType.Object);
        var nameLeaf = new SemanticLeafNode("Name", DataType.String, "OldValue");
        customerBranch.AddChild(nameLeaf);
        requestBranch.AddChild(customerBranch);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Order", new ColumnMapping("OrderData", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order", columnMapping).Returns(new ColumnMapping("OrderData", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "OrderData")
            .Returns([]);

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        Assert.Equal(string.Empty, nameLeaf.Value);
    }

    [Fact]
    public void FillBranchNode_WithNoMatches_HandlesEmptyBranch()
    {
        var requestBranch = new SemanticBranchNode("Customer", DataType.Object);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Customer", new ColumnMapping("CustomerData", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Customer", columnMapping).Returns(new ColumnMapping("CustomerData", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "CustomerData")
            .Returns([]);

        var exception = Record.Exception(() => _sut.FillBranchNode(requestBranch, responseTree, columnMapping));

        Assert.Null(exception);
    }

    #endregion

    #region Single Match Strategy Tests

    [Fact]
    public void FillBranchNode_WithSingleMatch_FillsChildren()
    {
        var requestBranch = new SemanticBranchNode("Customer", DataType.Object);
        var nameLeaf = new SemanticLeafNode("Customer.Name", DataType.String, string.Empty);
        requestBranch.AddChild(nameLeaf);
        var responseBranch = new SemanticBranchNode("Customer", DataType.Object);
        var responseLeaf = new SemanticLeafNode("CustomerName", DataType.String, "John Doe");
        responseBranch.AddChild(responseLeaf);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        responseTree.AddChild(responseBranch);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Customer", new ColumnMapping("Customer", string.Empty) },
            { "Customer.Name", new ColumnMapping(string.Empty, "CustomerName") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Customer", columnMapping).Returns(new ColumnMapping("Customer", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Customer")
            .Returns([responseBranch]);

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        _responseLeafNodeProcessor.Received(1).FillLeafNode(nameLeaf, responseBranch, columnMapping);
    }

    [Fact]
    public void FillBranchNode_WithSingleMatch_HandlesMixedChildren()
    {
        var requestBranch = new SemanticBranchNode("Order", DataType.Object);
        var nameLeaf = new SemanticLeafNode("Customer.Name", DataType.String, string.Empty);
        var addressBranch = new SemanticBranchNode("Address", DataType.Object);
        requestBranch.AddChild(nameLeaf);
        requestBranch.AddChild(addressBranch);
        var responseBranch = new SemanticBranchNode("Order", DataType.Object);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        responseTree.AddChild(responseBranch);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Order", new ColumnMapping("Order", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order", columnMapping).Returns(new ColumnMapping("Order", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Order")
            .Returns([responseBranch]);
        _responseSemanticTreeNodeResolver.GetColumnMapping("Address", columnMapping).Returns((ColumnMapping?)null);

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        _responseLeafNodeProcessor.Received(1).FillLeafNode(nameLeaf, responseBranch, columnMapping);
    }

    #endregion

    #region Multiple Matches Strategy Tests

    [Fact]
    public void FillBranchNode_WithMultipleMatches_CreatesIndexedClones()
    {
        var requestBranch = new SemanticBranchNode("Items", DataType.Array);
        var nameLeaf = new SemanticLeafNode("Name", DataType.String, string.Empty);
        requestBranch.AddChild(nameLeaf);
        var responseBranch1 = new SemanticBranchNode($"Items{IndexPrefix}0", DataType.Object);
        var responseBranch2 = new SemanticBranchNode($"Items{IndexPrefix}1", DataType.Object);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        responseTree.AddChild(responseBranch1);
        responseTree.AddChild(responseBranch2);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Items", new ColumnMapping("Items", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Items", columnMapping).Returns(new ColumnMapping("Items", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Items")
            .Returns([responseBranch1, responseBranch2]);
        _responseSemanticTreeNodeResolver.CreateIndexedSemanticId("Items", 0).Returns($"Items{IndexPrefix}0");
        _responseSemanticTreeNodeResolver.CreateIndexedSemanticId("Items", 1).Returns($"Items{IndexPrefix}1");

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        Assert.Equal(2, requestBranch.Children.Count);
    }

    [Fact]
    public void FillBranchNode_WithMultipleMatches_PreservesStructure()
    {
        var requestBranch = new SemanticBranchNode("Items", DataType.Array);
        var nameLeaf = new SemanticLeafNode("Name", DataType.String, string.Empty);
        var priceLeaf = new SemanticLeafNode("Price", DataType.Number, string.Empty);
        requestBranch.AddChild(nameLeaf);
        requestBranch.AddChild(priceLeaf);
        var responseBranch1 = new SemanticBranchNode($"Items{IndexPrefix}0", DataType.Object);
        var responseBranch2 = new SemanticBranchNode($"Items{IndexPrefix}1", DataType.Object);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        responseTree.AddChild(responseBranch1);
        responseTree.AddChild(responseBranch2);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Items", new ColumnMapping("Items", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Items", columnMapping).Returns(new ColumnMapping("Items", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Items")
            .Returns([responseBranch1, responseBranch2]);
        _responseSemanticTreeNodeResolver.CreateIndexedSemanticId("Items", Arg.Any<int>())
            .Returns(x => $"Items{IndexPrefix}{x[1]}");

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        Assert.Equal(2, requestBranch.Children.Count);
        Assert.All(requestBranch.Children, child =>
        {
            var branch = Assert.IsType<SemanticBranchNode>(child);
            Assert.Equal(2, branch.Children.Count);
        });
    }

    [Fact]
    public void FillBranchNode_WithManyMatches_HandlesLargeArrays()
    {
        var requestBranch = new SemanticBranchNode("Items", DataType.Array);
        var nameLeaf = new SemanticLeafNode("Name", DataType.String, string.Empty);
        requestBranch.AddChild(nameLeaf);
        var responseBranches = Enumerable.Range(0, 100)
            .Select(i => new SemanticBranchNode($"Items{IndexPrefix}{i}", DataType.Object))
            .ToList();
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        foreach (var branch in responseBranches)
        {
            responseTree.AddChild(branch);
        }

        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Items", new ColumnMapping("Items", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Items", columnMapping).Returns(new ColumnMapping("Items", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Items")
            .Returns(responseBranches);
        _responseSemanticTreeNodeResolver.CreateIndexedSemanticId("Items", Arg.Any<int>())
            .Returns(x => $"Items{IndexPrefix}{x[1]}");

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        Assert.Equal(100, requestBranch.Children.Count);
    }

    #endregion

    #region No Column Strategy Tests

    [Fact]
    public void FillBranchNode_WithNoColumn_ProcessesChildrenIndividually()
    {
        var requestBranch = new SemanticBranchNode("Order", DataType.Object);
        var nameLeaf = new SemanticLeafNode("Customer.Name", DataType.String, string.Empty);
        var totalLeaf = new SemanticLeafNode("Order.Total", DataType.Number, string.Empty);
        requestBranch.AddChild(nameLeaf);
        requestBranch.AddChild(totalLeaf);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);

        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Order", new ColumnMapping(string.Empty, string.Empty) },
            { "Customer.Name", new ColumnMapping(string.Empty, "CustomerName") },
            { "Order.Total", new ColumnMapping(string.Empty, "OrderTotal") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order", columnMapping).Returns(new ColumnMapping(string.Empty, string.Empty));
        _responseSemanticTreeNodeResolver.GetColumnMapping("Customer.Name", columnMapping).Returns(new ColumnMapping(string.Empty, "CustomerName"));
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order.Total", columnMapping).Returns(new ColumnMapping(string.Empty, "OrderTotal"));

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        _responseLeafNodeProcessor.Received(1).FillLeafNode(nameLeaf, responseTree, columnMapping);
        _responseLeafNodeProcessor.Received(1).FillLeafNode(totalLeaf, responseTree, columnMapping);
    }

    [Fact]
    public void FillBranchNode_WithNoColumn_ExpandsChildrenWithMultipleMatches()
    {
        var requestBranch = new SemanticBranchNode("Order", DataType.Object);
        var itemsBranch = new SemanticBranchNode("Items", DataType.Array);
        requestBranch.AddChild(itemsBranch);
        var responseBranch1 = new SemanticBranchNode($"Items{IndexPrefix}0", DataType.Object);
        var responseBranch2 = new SemanticBranchNode($"Items{IndexPrefix}1", DataType.Object);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        responseTree.AddChild(responseBranch1);
        responseTree.AddChild(responseBranch2);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Order", new ColumnMapping(string.Empty, string.Empty) },
            { "Items", new ColumnMapping("Items", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order", columnMapping).Returns(new ColumnMapping(string.Empty, string.Empty));
        _responseSemanticTreeNodeResolver.GetColumnMapping("Items", columnMapping).Returns(new ColumnMapping("Items", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Items")
            .Returns([responseBranch1, responseBranch2]);
        _responseSemanticTreeNodeResolver.CreateIndexedSemanticId("Items", Arg.Any<int>())
            .Returns(x => $"Items{IndexPrefix}{x[1]}");

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        Assert.Equal(2, requestBranch.Children.Count);
    }

    [Fact]
    public void FillBranchNode_WithNullColumnMapping_ProcessesAsNoColumn()
    {
        var requestBranch = new SemanticBranchNode("Order", DataType.Object);
        var nameLeaf = new SemanticLeafNode("Customer.Name", DataType.String, string.Empty);
        requestBranch.AddChild(nameLeaf);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Order", new ColumnMapping(string.Empty, string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order", columnMapping).Returns((ColumnMapping?)null);
        _responseSemanticTreeNodeResolver.GetColumnMapping("Customer.Name", columnMapping).Returns((ColumnMapping?)null);

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        _responseLeafNodeProcessor.Received(1).FillLeafNode(nameLeaf, responseTree, columnMapping);
    }

    #endregion

    [Fact]
    public void FillBranchNode_WithComplexNestedStructure_FillsCorrectly()
    {
        var requestBranch = new SemanticBranchNode("Order", DataType.Object);
        var itemsBranch = new SemanticBranchNode("Items", DataType.Array);
        var itemNameLeaf = new SemanticLeafNode("Items.Name", DataType.String, string.Empty);
        itemsBranch.AddChild(itemNameLeaf);
        requestBranch.AddChild(itemsBranch);
        var responseBranch = new SemanticBranchNode("Order", DataType.Object);
        var responseItems = new SemanticBranchNode("Items", DataType.Array);
        responseBranch.AddChild(responseItems);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        responseTree.AddChild(responseBranch);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Order", new ColumnMapping("Order", string.Empty) },
            { "Items", new ColumnMapping("Items", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order", columnMapping).Returns(new ColumnMapping("Order", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Order")
            .Returns([responseBranch]);
        _responseSemanticTreeNodeResolver.GetColumnMapping("Items", columnMapping).Returns(new ColumnMapping("Items", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseBranch, "Items")
            .Returns([responseItems]);

        _sut.FillBranchNode(requestBranch, responseTree, columnMapping);

        Assert.NotNull(requestBranch.Children);
    }

    [Fact]
    public void FillBranchNode_WithDeeplyNestedStructure_CompletesSuccessfully()
    {
        var level1 = new SemanticBranchNode("Level1", DataType.Object);
        var level2 = new SemanticBranchNode("Level2", DataType.Object);
        var level3 = new SemanticBranchNode("Level3", DataType.Object);
        var level4 = new SemanticBranchNode("Level4", DataType.Object);
        var level5 = new SemanticBranchNode("Level5", DataType.Object);
        level4.AddChild(level5);
        level3.AddChild(level4);
        level2.AddChild(level3);
        level1.AddChild(level2);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Level1", new ColumnMapping("Level1", string.Empty) }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Level1", columnMapping).Returns(new ColumnMapping("Level1", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Level1")
            .Returns([]);

        var exception = Record.Exception(() =>
            _sut.FillBranchNode(level1, responseTree, columnMapping));

        Assert.Null(exception);
    }

    [Fact]
    public void FillBranchNode_WithPartialMatches_HandlesMixedScenarios()
    {
        var requestBranch = new SemanticBranchNode("Order", DataType.Object);
        var item1 = new SemanticBranchNode("Items", DataType.Array);
        var item2 = new SemanticLeafNode("Total", DataType.Number, string.Empty);
        requestBranch.AddChild(item1);
        requestBranch.AddChild(item2);
        var responseBranch = new SemanticBranchNode("Order", DataType.Object);
        var responseTree = new SemanticBranchNode("Response", DataType.Object);
        responseTree.AddChild(responseBranch);
        var columnMapping = new Dictionary<string, ColumnMapping>
        {
            { "Order", new ColumnMapping("Order", string.Empty) },
            { "Items", new ColumnMapping(string.Empty, string.Empty) },
            { "Total", new ColumnMapping(string.Empty, "OrderTotal") }
        };
        _responseSemanticTreeNodeResolver.GetColumnMapping("Order", columnMapping).Returns(new ColumnMapping("Order", string.Empty));
        _responseSemanticTreeNodeResolver.FindMatchingBranchNodes(responseTree, "Order")
            .Returns([responseBranch]);
        _responseSemanticTreeNodeResolver.GetColumnMapping("Items", columnMapping).Returns(new ColumnMapping(string.Empty, string.Empty));
        _responseSemanticTreeNodeResolver.GetColumnMapping("Total", columnMapping).Returns(new ColumnMapping(string.Empty, "OrderTotal"));

        var exception = Record.Exception(() => _sut.FillBranchNode(requestBranch, responseTree, columnMapping));

        Assert.Null(exception);
        _responseLeafNodeProcessor.Received(1).FillLeafNode(item2, responseBranch, columnMapping);
    }
}
