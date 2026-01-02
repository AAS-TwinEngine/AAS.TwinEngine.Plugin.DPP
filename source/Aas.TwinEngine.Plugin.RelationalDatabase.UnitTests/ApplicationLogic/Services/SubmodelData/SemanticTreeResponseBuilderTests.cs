using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

using NSubstitute;

using OpenTelemetry;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

public class SemanticTreeResponseBuilderTests
{
    private readonly IOptions<Semantics> _semanticsOptions;
    private readonly SemanticTreeResponseBuilder _sut;
    private const string IndexPrefix = "_aastwinengine_";

    public SemanticTreeResponseBuilderTests()
    {
        _semanticsOptions = Substitute.For<IOptions<Semantics>>();
        _semanticsOptions.Value.Returns(new Semantics
        {
            IndexContextPrefix = IndexPrefix
        });
        _sut = new SemanticTreeResponseBuilder(_semanticsOptions);
    }

    [Fact]
    public void BuildResponse_NullRequestNode_ThrowsArgumentNullException()
    {
        var responseNode = new SemanticLeafNode("response", DataType.String, "value");
        var mapping = new Dictionary<string, string>();

        Assert.Throws<ArgumentNullException>(() => _sut.BuildResponse(null!, responseNode, mapping));
    }

    [Fact]
    public void BuildResponse_NullResponseNode_ReturnsRequestNode()
    {
        var requestNode = new SemanticLeafNode("request", DataType.String, string.Empty);
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, null!, mapping);

        Assert.Equal("request", result.SemanticId);
        Assert.Equal(DataType.String, result.DataType);
    }

    [Fact]
    public void BuildResponse_EmptyMapping_ReturnsNodeWithEmptyValue()
    {
        var requestNode = new SemanticLeafNode("request", DataType.String, string.Empty);
        var responseNode = new SemanticLeafNode("response", DataType.String, "responseValue");
        var mapping = new Dictionary<string, string>();

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        var leafResult = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(string.Empty, leafResult.Value);
    }

    [Fact]
    public void BuildResponse_LeafNodeWithMatchingColumn_ReturnsValueFromResponse()
    {
        var requestNode = new SemanticLeafNode("semanticId", DataType.String, string.Empty);
        var responseNode = new SemanticLeafNode("columnName", DataType.String, "expectedValue");
        var mapping = new Dictionary<string, string> { ["semanticId"] = "columnName" };

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        var leafResult = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("semanticId", leafResult.SemanticId);
        Assert.Equal("expectedValue", leafResult.Value);
    }

    [Fact]
    public void BuildResponse_LeafNodeWithNoMatchingColumn_ReturnsEmptyValue()
    {
        var requestNode = new SemanticLeafNode("semanticId", DataType.String, string.Empty);
        var responseNode = new SemanticLeafNode("differentColumn", DataType.String, "value");
        var mapping = new Dictionary<string, string> { ["semanticId"] = "nonExistentColumn" };

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        var leafResult = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(string.Empty, leafResult.Value);
    }

    [Fact]
    public void BuildResponse_LeafNodeNestedInResponse_FindsAndReturnsValue()
    {
        var requestNode = new SemanticLeafNode("semanticId", DataType.String, string.Empty);

        var responseBranch = new SemanticBranchNode("parent", DataType.Object);
        var responseLeaf = new SemanticLeafNode("columnName", DataType.String, "nestedValue");
        responseBranch.AddChild(responseLeaf);

        var mapping = new Dictionary<string, string> { ["semanticId"] = "columnName" };

        var result = _sut.BuildResponse(requestNode, responseBranch, mapping);

        var leafResult = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("nestedValue", leafResult.Value);
    }

    [Fact]
    public void BuildResponse_BranchNodeWithoutColumnMapping_MapsChildrenRecursively()
    {
        var requestBranch = new SemanticBranchNode("parentSemanticId", DataType.Object);
        var requestLeaf = new SemanticLeafNode("childSemanticId", DataType.String, string.Empty);
        requestBranch.AddChild(requestLeaf);

        var responseBranch = new SemanticBranchNode("parent", DataType.Object);
        var responseLeaf = new SemanticLeafNode("childColumn", DataType.String, "childValue");
        responseBranch.AddChild(responseLeaf);

        var mapping = new Dictionary<string, string>
        {
            ["parentSemanticId"] = string.Empty,
            ["childSemanticId"] = "childColumn"
        };

        var result = _sut.BuildResponse(requestBranch, responseBranch, mapping);

        var branchResult = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("parentSemanticId", branchResult.SemanticId);
        Assert.Single(branchResult.Children);

        var childLeaf = Assert.IsType<SemanticLeafNode>(branchResult.Children.First());
        Assert.Equal("childValue", childLeaf.Value);
    }

    [Fact]
    public void BuildResponse_BranchNodeWithSingleMatch_MapsBranchCorrectly()
    {
        var requestBranch = new SemanticBranchNode("requestBranch", DataType.Object);
        var requestLeaf = new SemanticLeafNode("leafId", DataType.String, string.Empty);
        requestBranch.AddChild(requestLeaf);

        var responseBranch = new SemanticBranchNode("responseBranch", DataType.Object);
        var nestedBranch = new SemanticBranchNode("branchColumn", DataType.Object);
        var responseLeaf = new SemanticLeafNode("leafColumn", DataType.String, "leafValue");
        nestedBranch.AddChild(responseLeaf);
        responseBranch.AddChild(nestedBranch);

        var mapping = new Dictionary<string, string>
        {
            ["requestBranch"] = "branchColumn",
            ["leafId"] = "leafColumn"
        };

        var result = _sut.BuildResponse(requestBranch, responseBranch, mapping);

        var branchResult = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("requestBranch", branchResult.SemanticId);
        Assert.Single(branchResult.Children);

        var childLeaf = Assert.IsType<SemanticLeafNode>(branchResult.Children.First());
        Assert.Equal("leafValue", childLeaf.Value);
    }

    [Fact]
    public void BuildResponse_BranchNodeWithMultipleMatches_CreatesIndexedBranches()
    {
        var requestBranch = new SemanticBranchNode("requestBranch", DataType.Array);
        var requestLeaf = new SemanticLeafNode("leafId", DataType.String, string.Empty);
        requestBranch.AddChild(requestLeaf);

        var responseBranch = new SemanticBranchNode("root", DataType.Object);

        var matchBranch1 = new SemanticBranchNode("branchColumn", DataType.Object);
        var leaf1 = new SemanticLeafNode("leafColumn", DataType.String, "value1");
        matchBranch1.AddChild(leaf1);

        var matchBranch2 = new SemanticBranchNode("branchColumn", DataType.Object);
        var leaf2 = new SemanticLeafNode("leafColumn", DataType.String, "value2");
        matchBranch2.AddChild(leaf2);

        responseBranch.AddChild(matchBranch1);
        responseBranch.AddChild(matchBranch2);

        var mapping = new Dictionary<string, string>
        {
            ["requestBranch"] = "branchColumn",
            ["leafId"] = "leafColumn"
        };

        var result = _sut.BuildResponse(requestBranch, responseBranch, mapping);

        var branchResult = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(2, branchResult.Children.Count);

        var firstIndexedBranch = Assert.IsType<SemanticBranchNode>(branchResult.Children[0]);
        Assert.Equal("requestBranch", firstIndexedBranch.SemanticId);

        var secondIndexedBranch = Assert.IsType<SemanticBranchNode>(branchResult.Children[1]);
        Assert.Equal("requestBranch", secondIndexedBranch.SemanticId);
    }

    [Fact]
    public void BuildResponse_BranchNodeWithNoMatches_ReturnsEmptyBranch()
    {
        var requestBranch = new SemanticBranchNode("requestBranch", DataType.Object);
        var requestLeaf = new SemanticLeafNode("leafId", DataType.String, string.Empty);
        requestBranch.AddChild(requestLeaf);

        var responseBranch = new SemanticBranchNode("differentBranch", DataType.Object);

        var mapping = new Dictionary<string, string>
        {
            ["requestBranch"] = "nonExistentBranch",
            ["leafId"] = "leafColumn"
        };

        var result = _sut.BuildResponse(requestBranch, responseBranch, mapping);

        var branchResult = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("requestBranch", branchResult.SemanticId);
        Assert.Single(branchResult.Children);

        var childLeaf = Assert.IsType<SemanticLeafNode>(branchResult.Children.First());
        Assert.Equal(string.Empty, childLeaf.Value);
    }

    [Fact]
    public void BuildResponse_NodeWithIndexPrefix_RemovesPrefixFromResult()
    {
        var requestNode = new SemanticLeafNode($"semanticId{IndexPrefix}00", DataType.String, string.Empty);
        var responseNode = new SemanticLeafNode("columnName", DataType.String, "value");
        var mapping = new Dictionary<string, string> { ["semanticId"] = "columnName" };

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        Assert.Equal("semanticId", result.SemanticId);
    }

    [Fact]
    public void BuildResponse_NestedNodesWithIndexPrefix_RemovesPrefixFromAllNodes()
    {
        var requestBranch = new SemanticBranchNode($"parent{IndexPrefix}00", DataType.Object);
        var requestLeaf = new SemanticLeafNode($"child{IndexPrefix}01", DataType.String, string.Empty);
        requestBranch.AddChild(requestLeaf);

        var responseBranch = new SemanticBranchNode("parent", DataType.Object);
        var responseLeaf = new SemanticLeafNode("childColumn", DataType.String, "value");
        responseBranch.AddChild(responseLeaf);

        var mapping = new Dictionary<string, string>
        {
            ["parent"] = string.Empty,
            ["child"] = "childColumn"
        };

        var result = _sut.BuildResponse(requestBranch, responseBranch, mapping);

        var branchResult = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("parent", branchResult.SemanticId);

        var childResult = Assert.IsType<SemanticLeafNode>(branchResult.Children.First());
        Assert.Equal("child", childResult.SemanticId);
    }

    [Fact]
    public void BuildResponse_DeeplyNestedStructure_MapsCorrectly()
    {
        var level1 = new SemanticBranchNode("level1", DataType.Object);
        var level2 = new SemanticBranchNode("level2", DataType.Object);
        var level3 = new SemanticLeafNode("level3", DataType.String, string.Empty);
        level2.AddChild(level3);
        level1.AddChild(level2);

        var responseLevel1 = new SemanticBranchNode("col1", DataType.Object);
        var responseLevel2 = new SemanticBranchNode("col2", DataType.Object);
        var responseLevel3 = new SemanticLeafNode("col3", DataType.String, "deepValue");
        responseLevel2.AddChild(responseLevel3);
        responseLevel1.AddChild(responseLevel2);

        var mapping = new Dictionary<string, string>
        {
            ["level1"] = "col1",
            ["level2"] = "col2",
            ["level3"] = "col3"
        };

        var result = _sut.BuildResponse(level1, responseLevel1, mapping);

        var resultLevel1 = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("level1", resultLevel1.SemanticId);

        var resultLevel2 = Assert.IsType<SemanticBranchNode>(resultLevel1.Children.First());
        Assert.Equal("level2", resultLevel2.SemanticId);

        var resultLevel3 = Assert.IsType<SemanticLeafNode>(resultLevel2.Children.First());
        Assert.Equal("level3", resultLevel3.SemanticId);
        Assert.Equal("deepValue", resultLevel3.Value);
    }

    [Fact]
    public void BuildResponse_MixedNodeTypes_MapsCorrectly()
    {
        var root = new SemanticBranchNode("root", DataType.Object);
        var leaf1 = new SemanticLeafNode("leaf1", DataType.String, string.Empty);
        var branch = new SemanticBranchNode("branch", DataType.Object);
        var leaf2 = new SemanticLeafNode("leaf2", DataType.Integer, string.Empty);
        branch.AddChild(leaf2);
        root.AddChild(leaf1);
        root.AddChild(branch);

        var responseRoot = new SemanticBranchNode("rootCol", DataType.Object);
        var responseLeaf1 = new SemanticLeafNode("leaf1Col", DataType.String, "stringValue");
        var responseBranch = new SemanticBranchNode("branchCol", DataType.Object);
        var responseLeaf2 = new SemanticLeafNode("leaf2Col", DataType.Integer, "42");
        responseBranch.AddChild(responseLeaf2);
        responseRoot.AddChild(responseLeaf1);
        responseRoot.AddChild(responseBranch);

        var mapping = new Dictionary<string, string>
        {
            ["root"] = string.Empty,
            ["leaf1"] = "leaf1Col",
            ["branch"] = "branchCol",
            ["leaf2"] = "leaf2Col"
        };

        var result = _sut.BuildResponse(root, responseRoot, mapping);

        var resultRoot = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(2, resultRoot.Children.Count);

        var resultLeaf1 = resultRoot.Children.First(c => c.SemanticId == "leaf1");
        Assert.IsType<SemanticLeafNode>(resultLeaf1);
        Assert.Equal("stringValue", ((SemanticLeafNode)resultLeaf1).Value);

        var resultBranch = resultRoot.Children.First(c => c.SemanticId == "branch");
        var resultBranchNode = Assert.IsType<SemanticBranchNode>(resultBranch);

        var resultLeaf2 = Assert.IsType<SemanticLeafNode>(resultBranchNode.Children.First());
        Assert.Equal("42", resultLeaf2.Value);
    }

    [Fact]
    public void BuildResponse_CaseInsensitiveColumnMatch_FindsMatchCorrectly()
    {
        var requestNode = new SemanticLeafNode("semanticId", DataType.String, string.Empty);
        var responseNode = new SemanticLeafNode("COLUMNNAME", DataType.String, "caseInsensitiveValue");
        var mapping = new Dictionary<string, string> { ["semanticId"] = "columnname" };

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        var leafResult = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("caseInsensitiveValue", leafResult.Value);
    }

    [Fact]
    public void BuildResponse_PreservesDataType_ForLeafNodes()
    {
        var requestNode = new SemanticLeafNode("semanticId", DataType.Integer, string.Empty);
        var responseNode = new SemanticLeafNode("columnName", DataType.Integer, "123");
        var mapping = new Dictionary<string, string> { ["semanticId"] = "columnName" };

        var result = _sut.BuildResponse(requestNode, responseNode, mapping);

        var leafResult = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.Integer, leafResult.DataType);
    }

    [Fact]
    public void BuildResponse_PreservesDataType_ForBranchNodes()
    {
        var requestBranch = new SemanticBranchNode("branch", DataType.Array);

        var responseBranch = new SemanticBranchNode("branchCol", DataType.Array);

        var mapping = new Dictionary<string, string> { ["branch"] = "branchCol" };

        var result = _sut.BuildResponse(requestBranch, responseBranch, mapping);

        var branchResult = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(DataType.Array, branchResult.DataType);
    }

    [Fact]
    public void BuildResponse_WithNestedBRanchNode_Having_MultipleValues()
    {
        var rootBranch = new SemanticBranchNode("root", DataType.Object);
        var requestBranch = new SemanticBranchNode("branch", DataType.Array);
        var leaf1 = new SemanticLeafNode("leaf1", DataType.String, "value1");
        var leaf2 = new SemanticLeafNode("leaf2", DataType.String, "value2");
        var nestedBranch = new SemanticBranchNode("nestedBranch", DataType.Array);
        nestedBranch.AddChild(leaf1);
        requestBranch.AddChild(nestedBranch);
        requestBranch.AddChild(leaf2);
        rootBranch.AddChild(requestBranch);

        var responseRootBranch = new SemanticBranchNode("root", DataType.Object);
        var responseBranch1 = new SemanticBranchNode("branchCol_aastwinengine_0", DataType.Array);
        var responseBranch2 = new SemanticBranchNode("branchCol_aastwinengine_1", DataType.Array);
        var responseNestedBranch1 = new SemanticBranchNode("nestedBranchCol_aastwinengine_0", DataType.Array);
        var responseNestedBranch2 = new SemanticBranchNode("nestedBranchCol_aastwinengine_1", DataType.Array);
        var responseNestedBranch3 = new SemanticBranchNode("nestedBranchCol", DataType.Array);
        var responseLeaf1 = new SemanticLeafNode("leaf1Col", DataType.String, "responseValue1");
        var responseLeaf2 = new SemanticLeafNode("leaf2Col", DataType.String, "responseValue2");
        
        responseNestedBranch1.AddChild(responseLeaf1);
        responseNestedBranch2.AddChild(responseLeaf1);
        responseNestedBranch3.AddChild(responseLeaf1);
        responseBranch1.AddChild(responseNestedBranch1);
        responseBranch1.AddChild(responseNestedBranch2);
        responseBranch2.AddChild(responseNestedBranch3);
        responseBranch1.AddChild(responseLeaf2);
        responseBranch2.AddChild(responseLeaf2);
        responseRootBranch.AddChild(responseBranch1);
        responseRootBranch.AddChild(responseBranch2);

        var mapping = new Dictionary<string, string>
        {
            ["branch"] = "branchCol",
            ["nestedBranch"] = "nestedBranchCol",
            ["leaf1"] = "leaf1Col",
            ["leaf2"] = "leaf2Col"
        };

        var result = _sut.BuildResponse(rootBranch, responseRootBranch, mapping);

        var resultRoot = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("root", resultRoot.SemanticId);
        Assert.Equal(2, resultRoot.Children.Count);
        var branches = resultRoot.Children.Cast<SemanticBranchNode>().ToList();
        Assert.Equal(2, branches.Count);
        Assert.All(branches, b => Assert.Equal("branch", b.SemanticId));
        var firstBranch = branches[0];
        Assert.Equal(DataType.Array, firstBranch.DataType);
        Assert.Equal(3, firstBranch.Children.Count); // leaf2 + 2 nestedBranches
        var firstBranchLeaf2 = firstBranch.Children.OfType<SemanticLeafNode>().FirstOrDefault(l => l.SemanticId == "leaf2");
        Assert.NotNull(firstBranchLeaf2);
        Assert.Equal("responseValue2", firstBranchLeaf2.Value);
        var firstBranchNestedBranches = firstBranch.Children.OfType<SemanticBranchNode>()
            .Where(b => b.SemanticId == "nestedBranch").ToList();
        Assert.Equal(2, firstBranchNestedBranches.Count);
        var firstNestedBranch1 = firstBranchNestedBranches[0];
        Assert.Equal(DataType.Array, firstNestedBranch1.DataType);
        Assert.Single(firstNestedBranch1.Children);
        var firstNestedBranch1Leaf = Assert.IsType<SemanticLeafNode>(firstNestedBranch1.Children.First());
        Assert.Equal("leaf1", firstNestedBranch1Leaf.SemanticId);
        Assert.Equal("responseValue1", firstNestedBranch1Leaf.Value);
        var firstNestedBranch2 = firstBranchNestedBranches[1];
        Assert.Equal(DataType.Array, firstNestedBranch2.DataType);
        Assert.Single(firstNestedBranch2.Children);
        var firstNestedBranch2Leaf = Assert.IsType<SemanticLeafNode>(firstNestedBranch2.Children.First());
        Assert.Equal("leaf1", firstNestedBranch2Leaf.SemanticId);
        Assert.Equal("responseValue1", firstNestedBranch2Leaf.Value);
        var secondBranch = branches[1];
        Assert.Equal(DataType.Array, secondBranch.DataType);
        Assert.Equal(2, secondBranch.Children.Count); // leaf2 + 1 nestedBranch
        var secondBranchLeaf2 = secondBranch.Children.OfType<SemanticLeafNode>().FirstOrDefault(l => l.SemanticId == "leaf2");
        Assert.NotNull(secondBranchLeaf2);
        Assert.Equal("responseValue2", secondBranchLeaf2.Value);

        var secondBranchNestedBranches = secondBranch.Children.OfType<SemanticBranchNode>()
            .Where(b => b.SemanticId == "nestedBranch").ToList();
        Assert.Single(secondBranchNestedBranches);
        var secondNestedBranch = secondBranchNestedBranches[0];
        Assert.Equal(DataType.Array, secondNestedBranch.DataType);
        Assert.Single(secondNestedBranch.Children);
        var secondNestedBranchLeaf = Assert.IsType<SemanticLeafNode>(secondNestedBranch.Children.First());
        Assert.Equal("leaf1", secondNestedBranchLeaf.SemanticId);
        Assert.Equal("responseValue1", secondNestedBranchLeaf.Value);
    }

    [Fact]
    public void BuildResponse_WithThreeLevelsOfNestedBranches_HandlesCorrectly()
    {
        // Request Structure:
        // Root
        //   └─ Products (Array)
        //        └─ Categories (Array)
        //             └─ Tags (Array)
        //                  └─ Name (Leaf)
        
        var root = new SemanticBranchNode("root", DataType.Object);
        var products = new SemanticBranchNode("products", DataType.Array);
        var categories = new SemanticBranchNode("categories", DataType.Array);
        var tags = new SemanticBranchNode("tags", DataType.Array);
        var tagName = new SemanticLeafNode("tagName", DataType.String, "");
        
        tags.AddChild(tagName);
        categories.AddChild(tags);
        products.AddChild(categories);
        root.AddChild(products);

        // Response Structure:
        // Root
        //   ├─ Products[0]
        //   │    ├─ Categories[0]
        //   │    │    ├─ Tags[0] { Name: "Tag1-1-1" }
        //   │    │    └─ Tags[1] { Name: "Tag1-1-2" }
        //   │    └─ Categories[1]
        //   │         └─ Tags[0] { Name: "Tag1-2-1" }
        //   └─ Products[1]
        //        └─ Categories[0]
        //             └─ Tags[0] { Name: "Tag2-1-1" }
        
        var responseRoot = new SemanticBranchNode("root", DataType.Object);
        
        // Product 0
        var responseProd0 = new SemanticBranchNode("productsCol_aastwinengine_0", DataType.Array);
        var responseCat0_0 = new SemanticBranchNode("categoriesCol_aastwinengine_0", DataType.Array);
        var responseTag0_0_0 = new SemanticBranchNode("tagsCol_aastwinengine_0", DataType.Array);
        var responseTag0_0_1 = new SemanticBranchNode("tagsCol_aastwinengine_1", DataType.Array);
        responseTag0_0_0.AddChild(new SemanticLeafNode("tagNameCol", DataType.String, "Tag1-1-1"));
        responseTag0_0_1.AddChild(new SemanticLeafNode("tagNameCol", DataType.String, "Tag1-1-2"));
        responseCat0_0.AddChild(responseTag0_0_0);
        responseCat0_0.AddChild(responseTag0_0_1);
        
        var responseCat0_1 = new SemanticBranchNode("categoriesCol_aastwinengine_1", DataType.Array);
        var responseTag0_1_0 = new SemanticBranchNode("tagsCol_aastwinengine_0", DataType.Array);
        responseTag0_1_0.AddChild(new SemanticLeafNode("tagNameCol", DataType.String, "Tag1-2-1"));
        responseCat0_1.AddChild(responseTag0_1_0);
        
        responseProd0.AddChild(responseCat0_0);
        responseProd0.AddChild(responseCat0_1);
        
        // Product 1
        var responseProd1 = new SemanticBranchNode("productsCol_aastwinengine_1", DataType.Array);
        var responseCat1_0 = new SemanticBranchNode("categoriesCol_aastwinengine_0", DataType.Array);
        var responseTag1_0_0 = new SemanticBranchNode("tagsCol_aastwinengine_0", DataType.Array);
        responseTag1_0_0.AddChild(new SemanticLeafNode("tagNameCol", DataType.String, "Tag2-1-1"));
        responseCat1_0.AddChild(responseTag1_0_0);
        responseProd1.AddChild(responseCat1_0);
        
        responseRoot.AddChild(responseProd0);
        responseRoot.AddChild(responseProd1);

        var mapping = new Dictionary<string, string>
        {
            ["products"] = "productsCol",
            ["categories"] = "categoriesCol",
            ["tags"] = "tagsCol",
            ["tagName"] = "tagNameCol"
        };

        var result = _sut.BuildResponse(root, responseRoot, mapping);

        var resultRoot = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(2, resultRoot.Children.Count); // 2 Products
        var product0 = resultRoot.Children.Cast<SemanticBranchNode>().ElementAt(0);
        Assert.Equal(2, product0.Children.Count); // 2 Categories in Product 0
        var category0_0 = product0.Children.Cast<SemanticBranchNode>().ElementAt(0);
        Assert.Equal(2, category0_0.Children.Count); // 2 Tags in Category 0 of Product 0
        var tag0_0_0 = category0_0.Children.Cast<SemanticBranchNode>().ElementAt(0);
        var leafValue0_0_0 = Assert.IsType<SemanticLeafNode>(tag0_0_0.Children.First());
        Assert.Equal("Tag1-1-1", leafValue0_0_0.Value);
        var tag0_0_1 = category0_0.Children.Cast<SemanticBranchNode>().ElementAt(1);
        var leafValue0_0_1 = Assert.IsType<SemanticLeafNode>(tag0_0_1.Children.First());
        Assert.Equal("Tag1-1-2", leafValue0_0_1.Value);
    }
}
