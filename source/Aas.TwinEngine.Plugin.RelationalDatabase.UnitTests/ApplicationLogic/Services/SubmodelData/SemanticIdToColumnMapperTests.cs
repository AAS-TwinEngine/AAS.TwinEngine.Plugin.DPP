using System.Text;
using System.Text.Json;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

public class SemanticIdToColumnMapperTests
{
    private readonly ILogger<SemanticIdToColumnMapper> _logger;
    private readonly IOptions<Semantics> _semanticsOptions;
    private readonly SemanticIdToColumnMapper _sut;

    public SemanticIdToColumnMapperTests()
    {
        _logger = Substitute.For<ILogger<SemanticIdToColumnMapper>>();
        _semanticsOptions = Substitute.For<IOptions<Semantics>>();
        MappingData.MappingJson = CreateJsonDocument("[]");
        _semanticsOptions.Value.Returns(new Semantics
        {
            IndexContextPrefix = "_aastwinengine_"
        });
        _sut = new SemanticIdToColumnMapper(_semanticsOptions, _logger);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_NullRequestNode_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _sut.GetSemanticIdToColumnMapping(null!));

        Assert.NotNull(exception);
        Assert.Contains("Value cannot be null", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_LeafNodeWithMatchingSemanticId_ReturnsCorrectMapping()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var leafNode = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName", DataType.String, string.Empty);

        var result = _sut.GetSemanticIdToColumnMapping(leafNode);

        Assert.Single(result);
        Assert.True(result.ContainsKey("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"));
        Assert.NotEmpty(result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
        Assert.Equal("ManufacturerName", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_LeafNodeWithNonMatchingSemanticId_ThrowsInvalidUserInputException()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var leafNode = new SemanticLeafNode("non-existent-semantic-id", DataType.String, string.Empty);

        Assert.Throws<InvalidUserInputException>(() => _sut.GetSemanticIdToColumnMapping(leafNode));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("not found in mapping")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_BranchNodeWithNoMapping_ReturnsEmptyColumnName()
    {
        MappingData.MappingJson = CreateJsonDocument("""
                                                             [
                                                                 { "Column": "dbo.Products.Name", "SemanticId": [ "  sid:1  " , "sid:1.0"]}
                                                               
                                                             ]
                                                     """);
        var branchNode = new SemanticBranchNode("unmapped-branch-id", DataType.Object);

        var result = _sut.GetSemanticIdToColumnMapping(branchNode);

        Assert.Single(result);
        Assert.Equal(string.Empty, result["unmapped-branch-id"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_BranchNodeWithChildren_MapsAllNodes()
    {
        MappingData.MappingJson = CreateJsonDocument("""
                                                             [
                                                                 { "Column": "dbo.Nameplate", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate"]},
                                                                 { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
                                                               
                                                             ]
                                                     """);
        var branchNode = new SemanticBranchNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate", DataType.Object);
        var childLeaf = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName", DataType.String, string.Empty);
        branchNode.AddChild(childLeaf);

        var result = _sut.GetSemanticIdToColumnMapping(branchNode);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("https://admin-shell.io/zvei/nameplate/2/0/Nameplate"));
        Assert.True(result.ContainsKey("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"));
        Assert.NotEmpty(result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate"]);
        Assert.Equal("Nameplate", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate"]);
        Assert.NotEmpty(result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
        Assert.Equal("ManufacturerName", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_NestedBranchNodes_MapsAllNodesRecursively()
    {
        MappingData.MappingJson = CreateJsonDocument("""
                                                             [
                                                                 { "Column": "dbo.root", "SemanticId": [ "root-branch"]},
                                                                 { "Column": "dbo.root.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
                                                               
                                                             ]
                                                     """);
        var rootBranch = new SemanticBranchNode("root-branch", DataType.Object);
        var childBranch = new SemanticBranchNode("child-branch", DataType.Object);
        var grandchildLeaf = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName", DataType.String, string.Empty);

        childBranch.AddChild(grandchildLeaf);
        rootBranch.AddChild(childBranch);

        var result = _sut.GetSemanticIdToColumnMapping(rootBranch);

        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("root-branch"));
        Assert.True(result.ContainsKey("child-branch"));
        Assert.True(result.ContainsKey("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"));
        Assert.NotEmpty(result["root-branch"]);
        Assert.Equal("root", result["root-branch"]);
        Assert.Empty(result["child-branch"]);
        Assert.NotEmpty(result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
        Assert.Equal("ManufacturerName", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_SemanticIdWithIndexPrefix_SplitsAndAppendsPrefix()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var leafNode = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName_aastwinengine_00", DataType.String, string.Empty);

        var result = _sut.GetSemanticIdToColumnMapping(leafNode);

        Assert.Single(result);
        var key = "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName_aastwinengine_00";
        Assert.True(result.ContainsKey(key));
        Assert.Contains("_aastwinengine_00", result[key], StringComparison.Ordinal);
        Assert.Equal("ManufacturerName_aastwinengine_00", result[key]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_SemanticIdWithoutIndexPrefix_ReturnsBaseColumnName()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var leafNode = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName", DataType.String, string.Empty);

        var result = _sut.GetSemanticIdToColumnMapping(leafNode);

        Assert.Single(result);
        Assert.DoesNotContain("_aastwinengine_", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"], StringComparison.CurrentCulture);
        Assert.Equal("ManufacturerName", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_ColumnWithDotNotation_ExtractsLastSegment()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var leafNode = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName", DataType.String, string.Empty);

        var result = _sut.GetSemanticIdToColumnMapping(leafNode);

        Assert.Single(result);
        var columnName = result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"];
        Assert.DoesNotContain(".", columnName, StringComparison.Ordinal);
        Assert.Equal("ManufacturerName", columnName);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_BranchWithMultipleChildren_MapsAllChildren()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Parent", "SemanticId": [ "parent-branch"]},
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var branchNode = new SemanticBranchNode("parent-branch", DataType.Object);
        var child1 = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName", DataType.String, string.Empty);
        var child2 = new SemanticBranchNode("child-branch", DataType.Object);

        branchNode.AddChild(child1);
        branchNode.AddChild(child2);

        var result = _sut.GetSemanticIdToColumnMapping(branchNode);

        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("parent-branch"));
        Assert.True(result.ContainsKey("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"));
        Assert.True(result.ContainsKey("child-branch"));
        Assert.Equal("Parent", result["parent-branch"]);
        Assert.Equal("ManufacturerName", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
        Assert.Equal(string.Empty, result["child-branch"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_SemanticIdCaseInsensitiveMatch_ReturnsMapping()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var leafNode = new SemanticLeafNode("HTTPS://ADMIN-SHELL.IO/ZVEI/NAMEPLATE/2/0/NAMEPLATE/MANUFACTURERNAME", DataType.String, string.Empty);

        var result = _sut.GetSemanticIdToColumnMapping(leafNode);

        Assert.Single(result);
        Assert.NotEmpty(result["HTTPS://ADMIN-SHELL.IO/ZVEI/NAMEPLATE/2/0/NAMEPLATE/MANUFACTURERNAME"]);
        Assert.Equal("ManufacturerName", result["HTTPS://ADMIN-SHELL.IO/ZVEI/NAMEPLATE/2/0/NAMEPLATE/MANUFACTURERNAME"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_ComplexTreeStructure_MapsAllNodes()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Root", "SemanticId": [ "root"]},
            { "Column": "dbo.Branch1", "SemanticId": [ "branch1"]},
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]},
            { "Column": "dbo.Branch2", "SemanticId": [ "branch2"]}
        ]
        """);
        var root = new SemanticBranchNode("root", DataType.Object);
        var branch1 = new SemanticBranchNode("branch1", DataType.Object);
        var leaf1 = new SemanticLeafNode("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName", DataType.String, string.Empty);
        branch1.AddChild(leaf1);
        var branch2 = new SemanticBranchNode("branch2", DataType.Array);
        var nestedBranch = new SemanticBranchNode("nested", DataType.Object);
        branch2.AddChild(nestedBranch);
        root.AddChild(branch1);
        root.AddChild(branch2);

        var result = _sut.GetSemanticIdToColumnMapping(root);

        Assert.Equal(5, result.Count);
        Assert.True(result.ContainsKey("root"));
        Assert.True(result.ContainsKey("branch1"));
        Assert.True(result.ContainsKey("https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"));
        Assert.True(result.ContainsKey("branch2"));
        Assert.True(result.ContainsKey("nested"));
        Assert.Equal("Root", result["root"]);
        Assert.Equal("Branch1", result["branch1"]);
        Assert.Equal("ManufacturerName", result["https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]);
        Assert.Equal("Branch2", result["branch2"]);
        Assert.Equal(string.Empty, result["nested"]);
    }

    [Fact]
    public void GetSemanticIdToColumnMapping_LeafWithUnmappedSemanticId_LogsErrorAndThrows()
    {
        MappingData.MappingJson = CreateJsonDocument("""
        [
            { "Column": "dbo.Nameplate.ManufacturerName", "SemanticId": [ "https://admin-shell.io/zvei/nameplate/2/0/Nameplate/ManufacturerName"]}
        ]
        """);
        var leafNode = new SemanticLeafNode("unknown-semantic-id", DataType.String, string.Empty);

        Assert.Throws<InvalidUserInputException>(() => _sut.GetSemanticIdToColumnMapping(leafNode));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static JsonElement CreateJsonDocument(string json) => JsonDocument.Parse(Encoding.UTF8.GetBytes(json)).RootElement;
}
