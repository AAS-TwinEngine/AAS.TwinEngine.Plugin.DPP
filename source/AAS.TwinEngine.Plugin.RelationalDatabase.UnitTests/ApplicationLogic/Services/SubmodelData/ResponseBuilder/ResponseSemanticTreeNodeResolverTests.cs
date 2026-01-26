using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

public class ResponseSemanticTreeNodeResolverTests
{
    private readonly IResponseSemanticTreeNodeResolver _sut;
    private const string IndexPrefix = "[Index]:";

    public ResponseSemanticTreeNodeResolverTests()
    {
        var semanticsOptions = Options.Create(new Semantics { IndexContextPrefix = IndexPrefix });
        _sut = new ResponseSemanticTreeNodeResolver(semanticsOptions);
    }

    #region GetColumnName Tests

    [Fact]
    public void GetColumnName_WithExistingMapping_ReturnsColumnName()
    {
        var mapping = new Dictionary<string, string>
        {
            { "Product.Name", "ProductName" }
        };

        var result = _sut.GetColumnName("Product.Name", mapping);

        Assert.Equal("ProductName", result);
    }

    [Fact]
    public void GetColumnName_WithNonExistingMapping_ReturnsNull()
    {
        var mapping = new Dictionary<string, string>
        {
            { "Product.Name", "ProductName" }
        };

        var result = _sut.GetColumnName("Product.Price", mapping);

        Assert.Null(result);
    }

    [Fact]
    public void GetColumnName_WithEmptyMapping_ReturnsNull()
    {
        var mapping = new Dictionary<string, string>();

        var result = _sut.GetColumnName("Product.Name", mapping);

        Assert.Null(result);
    }

    [Fact]
    public void GetColumnName_WithNullSemanticId_ReturnsNull()
    {
        var mapping = new Dictionary<string, string>
        {
            { "Product.Name", "ProductName" }
        };

        var result = _sut.GetColumnName(null!, mapping);

        Assert.Null(result);
    }

    [Fact]
    public void GetColumnName_WithEmptySemanticId_ReturnsNull()
    {
        var mapping = new Dictionary<string, string>
        {
            { "Product.Name", "ProductName" }
        };

        var result = _sut.GetColumnName(string.Empty, mapping);

        Assert.Null(result);
    }

    [Fact]
    public void GetColumnName_WithSpecialCharacters_HandlesCorrectly()
    {
        var mapping = new Dictionary<string, string>
        {
            { "Product@Name#123", "ProductName" }
        };

        var result = _sut.GetColumnName("Product@Name#123", mapping);

        Assert.Equal("ProductName", result);
    }

    #endregion

    #region FindMatchingLeafNodes Tests

    [Fact]
    public void FindMatchingLeafNodes_WithMatchingLeaf_ReturnsLeaf()
    {
        var leafNode = new SemanticLeafNode("ProductName", DataType.String, "Laptop");

        var result = _sut.FindMatchingLeafNodes(leafNode, "ProductName");

        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Value);
    }

    [Fact]
    public void FindMatchingLeafNodes_WithNonMatchingLeaf_ReturnsEmpty()
    {
        var leafNode = new SemanticLeafNode("ProductName", DataType.String, "Laptop");

        var result = _sut.FindMatchingLeafNodes(leafNode, "ProductPrice");

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatchingLeafNodes_CaseInsensitive_ReturnsLeaf()
    {
        var leafNode = new SemanticLeafNode("ProductName", DataType.String, "Laptop");

        var result = _sut.FindMatchingLeafNodes(leafNode, "productname");

        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Value);
    }

    [Fact]
    public void FindMatchingLeafNodes_WithNestedStructure_FindsAllMatches()
    {
        var root = new SemanticBranchNode("Product", DataType.Object);
        var name1 = new SemanticLeafNode("Name", DataType.String, "Laptop");
        var name2 = new SemanticLeafNode("Name", DataType.String, "Mouse");
        var child1 = new SemanticBranchNode("Item1", DataType.Object);
        child1.AddChild(name1);
        var child2 = new SemanticBranchNode("Item2", DataType.Object);
        child2.AddChild(name2);
        root.AddChild(child1);
        root.AddChild(child2);

        var result = _sut.FindMatchingLeafNodes(root, "Name");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, n => n.Value == "Laptop");
        Assert.Contains(result, n => n.Value == "Mouse");
    }

    [Fact]
    public void FindMatchingLeafNodes_WithBranchNode_ReturnsEmpty()
    {
        var branchNode = new SemanticBranchNode("Product", DataType.Object);

        var result = _sut.FindMatchingLeafNodes(branchNode, "Product");

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatchingLeafNodes_WithEmptyBranch_ReturnsEmpty()
    {
        var branchNode = new SemanticBranchNode("Product", DataType.Object);

        var result = _sut.FindMatchingLeafNodes(branchNode, "Name");

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatchingLeafNodes_WithDeeplyNestedStructure_FindsLeaves()
    {
        var root = new SemanticBranchNode("Level1", DataType.Object);
        var level2 = new SemanticBranchNode("Level2", DataType.Object);
        var level3 = new SemanticBranchNode("Level3", DataType.Object);
        var level4 = new SemanticBranchNode("Level4", DataType.Object);
        var leaf = new SemanticLeafNode("Target", DataType.String, "Found");
        level4.AddChild(leaf);
        level3.AddChild(level4);
        level2.AddChild(level3);
        root.AddChild(level2);

        var result = _sut.FindMatchingLeafNodes(root, "Target");

        Assert.Single(result);
        Assert.Equal("Found", result[0].Value);
    }

    [Fact]
    public void FindMatchingLeafNodes_WithNullValue_FindsLeaf()
    {
        var leafNode = new SemanticLeafNode("ProductName", DataType.String, null!);

        var result = _sut.FindMatchingLeafNodes(leafNode, "ProductName");

        Assert.Single(result);
        Assert.Null(result[0].Value);
    }

    [Fact]
    public void FindMatchingLeafNodes_WithEmptyValue_FindsLeaf()
    {
        var leafNode = new SemanticLeafNode("ProductName", DataType.String, string.Empty);

        var result = _sut.FindMatchingLeafNodes(leafNode, "ProductName");

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].Value);
    }

    #endregion

    #region FindMatchingBranchNodes Tests

    [Fact]
    public void FindMatchingBranchNodes_WithMatchingBranch_ReturnsBranch()
    {
        var branchNode = new SemanticBranchNode("Customer", DataType.Object);

        var result = _sut.FindMatchingBranchNodes(branchNode, "Customer");

        Assert.Single(result);
        Assert.Equal("Customer", result[0].SemanticId);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithNonMatchingBranch_ReturnsEmpty()
    {
        var branchNode = new SemanticBranchNode("Customer", DataType.Object);

        var result = _sut.FindMatchingBranchNodes(branchNode, "Product");

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithIndexedBranch_StripsIndexForComparison()
    {
        var branchNode = new SemanticBranchNode($"Items{IndexPrefix}0", DataType.Array);

        var result = _sut.FindMatchingBranchNodes(branchNode, "Items");

        Assert.Single(result);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithMultipleMatches_ReturnsAll()
    {
        var root = new SemanticBranchNode("Order", DataType.Object);
        var item1 = new SemanticBranchNode($"Items{IndexPrefix}0", DataType.Object);
        var item2 = new SemanticBranchNode($"Items{IndexPrefix}1", DataType.Object);
        root.AddChild(item1);
        root.AddChild(item2);

        var result = _sut.FindMatchingBranchNodes(root, "Items");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithLeafNode_ReturnsEmpty()
    {
        var leafNode = new SemanticLeafNode("Name", DataType.String, "Test");

        var result = _sut.FindMatchingBranchNodes(leafNode, "Name");

        Assert.Empty(result);
    }

    [Fact]
    public void FindMatchingBranchNodes_CaseInsensitive_ReturnsBranch()
    {
        var branchNode = new SemanticBranchNode("Customer", DataType.Object);

        var result = _sut.FindMatchingBranchNodes(branchNode, "customer");

        Assert.Single(result);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithNestedBranches_FindsAllMatches()
    {
        var root = new SemanticBranchNode("Order", DataType.Object);
        var items = new SemanticBranchNode("Items", DataType.Array);
        var tags = new SemanticBranchNode("Tags", DataType.Array);
        items.AddChild(tags);
        root.AddChild(items);

        var result = _sut.FindMatchingBranchNodes(root, "Items");

        Assert.Single(result);
        Assert.Equal("Items", result[0].SemanticId);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithEmptyBranch_FindsBranch()
    {
        var branchNode = new SemanticBranchNode("EmptyBranch", DataType.Object);

        var result = _sut.FindMatchingBranchNodes(branchNode, "EmptyBranch");

        Assert.Single(result);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithMixedChildren_FindsOnlyBranches()
    {
        var root = new SemanticBranchNode("Order", DataType.Object);
        var itemsBranch = new SemanticBranchNode("Items", DataType.Array);
        var nameLeaf = new SemanticLeafNode("Name", DataType.String, "Test");
        root.AddChild(itemsBranch);
        root.AddChild(nameLeaf);

        var result = _sut.FindMatchingBranchNodes(root, "Items");

        Assert.Single(result);
        Assert.Equal("Items", result[0].SemanticId);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithDuplicateSemanticIds_ReturnsAllMatches()
    {
        var root = new SemanticBranchNode("Order", DataType.Object);
        var items1 = new SemanticBranchNode("Items", DataType.Array);
        var items2 = new SemanticBranchNode("Items", DataType.Array);
        root.AddChild(items1);
        root.AddChild(items2);

        var result = _sut.FindMatchingBranchNodes(root, "Items");

        Assert.Equal(2, result.Count);
    }

    #endregion

    #region CreateIndexedSemanticId Tests

    [Fact]
    public void CreateIndexedSemanticId_CreatesCorrectFormat()
    {
        const string BaseId = "Product";
        const int Index = 0;

        var result = _sut.CreateIndexedSemanticId(BaseId, Index);

        Assert.Equal($"Product{IndexPrefix}0", result);
    }

    [Fact]
    public void CreateIndexedSemanticId_WithLargeIndex_CreatesCorrectFormat()
    {
        const string BaseId = "Product";
        const int Index = 999;

        var result = _sut.CreateIndexedSemanticId(BaseId, Index);

        Assert.Equal($"Product{IndexPrefix}999", result);
    }

    [Fact]
    public void CreateIndexedSemanticId_WithZeroIndex_CreatesCorrectFormat()
    {
        const string BaseId = "Product";
        const int Index = 0;

        var result = _sut.CreateIndexedSemanticId(BaseId, Index);

        Assert.Equal($"Product{IndexPrefix}0", result);
    }

    [Fact]
    public void CreateIndexedSemanticId_WithSpecialCharactersInBaseId_PreservesCharacters()
    {
        const string BaseId = "Product@Item#123";
        const int Index = 5;

        var result = _sut.CreateIndexedSemanticId(BaseId, Index);

        Assert.Equal($"Product@Item#123{IndexPrefix}5", result);
    }

    [Fact]
    public void CreateIndexedSemanticId_WithEmptyBaseId_CreatesIndexOnly()
    {
        var baseId = string.Empty;
        const int Index = 1;

        var result = _sut.CreateIndexedSemanticId(baseId, Index);

        Assert.Equal($"{IndexPrefix}1", result);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void FindMatchingLeafNodes_WithVeryLargeTree_CompletesSuccessfully()
    {
        var root = new SemanticBranchNode("Root", DataType.Object);
        for (var i = 0; i < 100; i++)
        {
            var leaf = new SemanticLeafNode("Target", DataType.String, $"Value{i}");
            root.AddChild(leaf);
        }

        var result = _sut.FindMatchingLeafNodes(root, "Target");

        Assert.Equal(100, result.Count);
    }

    [Fact]
    public void FindMatchingBranchNodes_WithVeryLargeTree_CompletesSuccessfully()
    {
        var root = new SemanticBranchNode("Root", DataType.Object);
        for (var i = 0; i < 100; i++)
        {
            var branch = new SemanticBranchNode("Target", DataType.Object);
            root.AddChild(branch);
        }

        var result = _sut.FindMatchingBranchNodes(root, "Target");

        Assert.Equal(100, result.Count);
    }

    #endregion
}
