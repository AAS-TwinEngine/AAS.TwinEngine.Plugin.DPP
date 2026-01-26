using System.Data.Common;
using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.MetaData;

public class MetaDataProviderTests
{
    private readonly IQueryExecutor _queryExecutor;
    private readonly ILogger<MetaDataProvider> _logger;
    private readonly MetaDataProvider _sut;

    public MetaDataProviderTests()
    {
        _queryExecutor = Substitute.For<IQueryExecutor>();
        _logger = Substitute.For<ILogger<MetaDataProvider>>();

        _sut = new MetaDataProvider(_logger, _queryExecutor);
    }

    #region GetShellDescriptorsAsync

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenQueryReturnsEmpty_ReturnsEmptyResult()
    {
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<CancellationToken>()).Returns(string.Empty);

        var result = await _sut.GetShellDescriptorsAsync("query", null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Result!);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenValidJson_ReturnsProcessedItems()
    {
        var items = new List<ShellDescriptorData>
        {
            new()
            {
                GlobalAssetId = "asset-1",
                Id = null!,
                IdShort = "Shell1",
                SpecificAssetIds =
                [
                    new SpecificAssetIdsData { Name = null, Value = "VAL1" }
                ]
            }
        };
        var json = JsonSerializer.Serialize(items);
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<CancellationToken>()).Returns(json);

        var result = await _sut.GetShellDescriptorsAsync("query", null, null, CancellationToken.None);

        var item = Assert.Single(result!.Result!);
        Assert.Equal("asset-1", item.Id); // fallback
        Assert.Equal("VAL1", item.SpecificAssetIds![0].Name);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenJsonDeserializesToEmptyList_ReturnsEmptyResult()
    {
        var emptyJsonArray = "[]";
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<CancellationToken>()).Returns(emptyJsonArray);

        var result = await _sut.GetShellDescriptorsAsync(
            query: "query",
            limit: null,
            cursor: null,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.PagingMetaData);
        Assert.Null(result.PagingMetaData.Cursor);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result);
    }

    #endregion

    #region GetShellDescriptorAsync

    [Fact]
    public async Task GetShellDescriptorAsync_WhenEmptyResult_ReturnsEmptyObject()
    {
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(string.Empty);

        var result = await _sut.GetShellDescriptorAsync("query", "aas-1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Id);
        Assert.Null(result.GlobalAssetId);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenValidJson_ReturnsProcessedObject()
    {
        var item = new ShellDescriptorData
        {
            GlobalAssetId = "asset-1",
            Id = null!,
            IdShort = "Shell1",
            SpecificAssetIds =
            [
                new SpecificAssetIdsData { Name = null, Value = "VAL1" }
            ]
        };
        var json = JsonSerializer.Serialize(item);
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        var result = await _sut.GetShellDescriptorAsync("query", "aas-1", CancellationToken.None);

        Assert.Equal("asset-1", result!.Id);
        Assert.Equal("VAL1", result.SpecificAssetIds![0].Name);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenJsonIsNullLiteral_ReturnsEmptyShellDescriptor()
    {
        var jsonNullLiteral = "null";
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(jsonNullLiteral);

        var result = await _sut.GetShellDescriptorAsync(
            query: "query",
            aasIdentifier: "aas-1",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Id);
        Assert.Null(result.GlobalAssetId);
        Assert.Null(result.IdShort);
    }

    #endregion

    #region GetAssetAsync

    [Fact]
    public async Task GetAssetAsync_WhenEmptyResult_ReturnsEmptyAsset()
    {
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(string.Empty);

        var result = await _sut.GetAssetAsync("query", "asset-1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.GlobalAssetId);
    }

    [Fact]
    public async Task GetAssetAsync_WhenValidJson_ReturnsAsset()
    {
        var asset = new AssetData
        {
            GlobalAssetId = "asset-123",
            DefaultThumbnail = new DefaultThumbnailData
            {
                Path = "/img.png",
                ContentType = "image/png"
            }
        };
        var json = JsonSerializer.Serialize(asset);
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        var result = await _sut.GetAssetAsync("query", "asset-123", CancellationToken.None);

        Assert.Equal("asset-123", result!.GlobalAssetId);
        Assert.Equal("image/png", result.DefaultThumbnail?.ContentType);
    }

    #endregion
}
