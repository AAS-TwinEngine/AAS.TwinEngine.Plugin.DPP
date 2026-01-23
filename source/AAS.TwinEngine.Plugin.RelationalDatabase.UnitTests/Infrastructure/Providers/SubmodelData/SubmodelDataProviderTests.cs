using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.SubmodelData;

public class SubmodelDataProviderTests
{
    private readonly ILogger<SubmodelDataProvider> _logger;
    private readonly IJsonResponseParser _jsonResponseParser;
    private readonly IQueryExecutor _queryExecutor;

    private readonly SubmodelDataProvider _sut;

    public SubmodelDataProviderTests()
    {
        _logger = Substitute.For<ILogger<SubmodelDataProvider>>();
        _jsonResponseParser = Substitute.For<IJsonResponseParser>();
        _queryExecutor = Substitute.For<IQueryExecutor>();

        _sut = new SubmodelDataProvider(
            _logger,
            _jsonResponseParser,
            _queryExecutor);
    }

    [Fact]
    public async Task GetSubmodelValuesAsync_WhenValidJsonReturned_ShouldReturnSemanticTreeNode()
    {
        const string Sql = "SELECT * FROM table";
        const string ProductId = "PROD-001";
        const string Json = "{ \"key\": \"value\" }";
        var expectedNode = new SemanticLeafNode("semanticId", DataType.String, "value");
        _queryExecutor.ExecuteQueryAsync(Sql, Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(Json);
        _jsonResponseParser.ParseJson(Json).Returns(expectedNode);

        var result = await _sut.GetSubmodelValuesAsync(Sql, ProductId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedNode, result);
        await _queryExecutor.Received(1).ExecuteQueryAsync(Sql, Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>());
        _jsonResponseParser.Received(1).ParseJson(Json);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSubmodelValuesAsync_WhenJsonIsEmpty_ShouldThrowResourceNotValidException(string json)
    {
        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        await Assert.ThrowsAsync<ResourceNotValidException>(() => _sut.GetSubmodelValuesAsync("sql", "productId", CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelValuesAsync_ShouldPassProductIdAsSqlParameter()
    {
        const string ProductId = "PROD-XYZ";
        const string Json = "{ }";
        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(Json);
        _jsonResponseParser.ParseJson(Json).Returns(new SemanticLeafNode("id", DataType.String, "value"));

        await _sut.GetSubmodelValuesAsync("sql", ProductId, CancellationToken.None);

        await _queryExecutor.Received(1).ExecuteQueryAsync(Arg.Any<string>(), Arg.Is<IEnumerable<DbParameter>>(parameters => parameters.Any(p =>
                        p.ParameterName == "@ProductId" && (string?)p.Value == ProductId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SemanticBranchNode_ShouldAddAndReplaceChildrenCorrectly()
    {
        var branch = new SemanticBranchNode("branch", DataType.Object);
        var child1 = new SemanticLeafNode("c1", DataType.String, "v1");
        var child2 = new SemanticLeafNode("c2", DataType.String, "v2");

        branch.AddChild(child1);
        branch.ReplaceChildren([child2]);

        Assert.Single(branch.Children);
        Assert.Equal(child2, branch.Children.First());
    }

    [Fact]
    public void SemanticLeafNode_ShouldStoreValueCorrectly()
    {
        var leaf = new SemanticLeafNode("leaf", DataType.String, "test-value");

        Assert.Equal("leaf", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
        Assert.Equal("test-value", leaf.Value);
    }
}
