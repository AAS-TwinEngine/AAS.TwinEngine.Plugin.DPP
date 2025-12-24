using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

using NSubstitute;

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
}
