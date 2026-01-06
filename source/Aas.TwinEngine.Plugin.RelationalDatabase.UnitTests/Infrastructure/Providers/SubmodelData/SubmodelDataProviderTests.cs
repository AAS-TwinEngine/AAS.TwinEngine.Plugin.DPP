using System.Data.Common;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.SubmodelData;

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
        // Arrange
        var sql = "SELECT * FROM table";
        var productId = "PROD-001";
        var json = "{ \"key\": \"value\" }";

        var expectedNode = new SemanticLeafNode("semanticId", DataType.String, "value");

        _queryExecutor.ExecuteQueryAsync(sql, Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        _jsonResponseParser.ParseJson(json).Returns(expectedNode);

        // Act
        var result = await _sut.GetSubmodelValuesAsync(sql, productId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedNode, result);

        await _queryExecutor.Received(1).ExecuteQueryAsync(sql, Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>());

        _jsonResponseParser.Received(1).ParseJson(json);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSubmodelValuesAsync_WhenJsonIsEmpty_ShouldThrowResponseNotFoundException(string json)
    {
        // Arrange
        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        // Act & Assert
        await Assert.ThrowsAsync<ResponseNotFoundException>(() => _sut.GetSubmodelValuesAsync("sql", "productId", CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelValuesAsync_ShouldPassProductIdAsSqlParameter()
    {
        // Arrange
        var productId = "PROD-XYZ";
        var json = "{ }";

        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        _jsonResponseParser.ParseJson(json).Returns(new SemanticLeafNode("id", DataType.String, "value"));

        // Act
        await _sut.GetSubmodelValuesAsync("sql", productId, CancellationToken.None);

        // Assert
        await _queryExecutor.Received(1).ExecuteQueryAsync(Arg.Any<string>(), Arg.Is<IEnumerable<DbParameter>>(parameters => parameters.Any(p =>
                        p.ParameterName == "@ProductId" && (string?)p.Value == productId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SemanticBranchNode_ShouldAddAndReplaceChildrenCorrectly()
    {
        // Arrange
        var branch = new SemanticBranchNode("branch", DataType.Object);

        var child1 = new SemanticLeafNode("c1", DataType.String, "v1");
        var child2 = new SemanticLeafNode("c2", DataType.String, "v2");

        // Act
        branch.AddChild(child1);
        branch.ReplaceChildren([child2]);

        // Assert
        Assert.Single(branch.Children);
        Assert.Equal(child2, branch.Children.First());
    }

    [Fact]
    public void SemanticLeafNode_ShouldStoreValueCorrectly()
    {
        // Arrange
        var leaf = new SemanticLeafNode("leaf", DataType.String, "test-value");

        // Assert
        Assert.Equal("leaf", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
        Assert.Equal("test-value", leaf.Value);
    }
}
