using System.Data.Common;
using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.Core;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.MetaData;

public class MetaDataProviderTests
{
    private const string QueryWithMarkers = "query\n{{__ASSET_FILTER__}}\n{{__PAGINATION__}};";

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
        _queryExecutor
            .ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, null, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Result!);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenValidJson_ExcludesInvalidIdItems_AndLogsError()
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
            },
            new()
            {
                GlobalAssetId = "asset-2",
                Id = "shell-2",
                IdShort = "Shell2",
                SpecificAssetIds =
                [
                    new SpecificAssetIdsData { Name = null, Value = "VAL2" }
                ]
            }
        };
        var json = JsonSerializer.Serialize(items);
        _queryExecutor
            .ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(json);

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, null, null, null, null, CancellationToken.None);

        var item = Assert.Single(result!.Result!);
        Assert.Equal("shell-2", item.Id);
        Assert.Equal("VAL2", item.SpecificAssetIds![0].Name);
        Assert.True(HasLogged(_logger.ReceivedCalls(), LogLevel.Error, "Metadata-Shell has null or empty Id."));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenJsonDeserializesToEmptyList_ReturnsEmptyResult()
    {
        var emptyJsonArray = "[]";
        _queryExecutor
            .ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(emptyJsonArray);

        var result = await _sut.GetShellDescriptorsAsync(
            query: QueryWithMarkers,
            limit: null,
            cursor: null,
            filter: null,
            idShort: null,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.PagingMetaData);
        Assert.Null(result.PagingMetaData.Cursor);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenFilterMatchesSpecificAssetId_ReturnsOnlyMatchingItems()
    {
        var items = new List<ShellDescriptorData>
        {
            new()
            {
                GlobalAssetId = "asset-1",
                Id = "shell-1",
                IdShort = "Shell1",
                SpecificAssetIds =
                [
                    new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
                ]
            }
        };
        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(items));

        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, null, null, filter, null, CancellationToken.None);

        var matched = Assert.Single(result!.Result!);
        Assert.Equal("shell-1", matched.Id);

        await _queryExecutor.Received(1).ExecuteQueryAsync(
            Arg.Is<string>(query => query.Contains("WHERE EXISTS", StringComparison.Ordinal)
                                    && query.Contains("FROM \"SpecificAssetIds\" sai", StringComparison.Ordinal)),
            Arg.Is<IEnumerable<DbParameter>>(parameters => parameters.Count() == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenIdShortProvided_FiltersByIdShort()
    {
        const string idShort = "M&M03";
        var items = new List<ShellDescriptorData>
        {
            new() { GlobalAssetId = "asset-1", Id = "shell-1", IdShort = "M&M03", SpecificAssetIds = [] },
            new() { GlobalAssetId = "asset-2", Id = "shell-2", IdShort = "OtherShell", SpecificAssetIds = [] }
        };
        _queryExecutor
            .ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(items));

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, null, null, null, idShort, CancellationToken.None);

        Assert.NotNull(result);
        await _queryExecutor.Received(1).ExecuteQueryAsync(
            Arg.Is<string>(query => query.Contains("A.\"IdShort\" = @idShort", StringComparison.Ordinal)),
            Arg.Is<IEnumerable<DbParameter>>(parameters =>
                parameters.Count() == 2 &&
                parameters.First().ParameterName == "@idShort" &&
                parameters.First().Value!.ToString() == idShort),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenFilterHasNoMatches_ReturnsEmptyResult()
    {
        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns("[]");

        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-NOMATCH" }
            ]
        };

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, null, null, filter, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Result!);

        await _queryExecutor.Received(1).ExecuteQueryAsync(
            Arg.Is<string>(query => query.Contains("WHERE EXISTS", StringComparison.Ordinal)),
            Arg.Any<IEnumerable<DbParameter>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenFilterHasGlobalAssetId_ReturnsMatchingGlobalAssetItem()
    {
        var items = new List<ShellDescriptorData>
        {
            new() { GlobalAssetId = "https://mm-software.com/ids/assets/000-002", Id = "shell-2", IdShort = "Shell2", SpecificAssetIds = [] }
        };
        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(items));

        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "https://mm-software.com/ids/assets/000-002" }
            ]
        };

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, null, null, filter, null, CancellationToken.None);

        var matched = Assert.Single(result!.Result!);
        Assert.Equal("shell-2", matched.Id);

        await _queryExecutor.Received(1).ExecuteQueryAsync(
            Arg.Is<string>(query => query.Contains("A.\"GlobalAssetId\" = @f_value_0", StringComparison.Ordinal)
                                    && !query.Contains("EXISTS", StringComparison.Ordinal)),
            Arg.Is<IEnumerable<DbParameter>>(parameters => parameters.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenResultContainsMoreThanPageSize_ReturnsLimitedItems_AndNextCursor()
    {
        var items = new List<ShellDescriptorData>
        {
            new() { GlobalAssetId = "asset-1", Id = "shell-1", IdShort = "Shell1", SpecificAssetIds = [] },
            new() { GlobalAssetId = "asset-2", Id = "shell-2", IdShort = "Shell2", SpecificAssetIds = [] },
            new() { GlobalAssetId = "asset-3", Id = "shell-3", IdShort = "Shell3", SpecificAssetIds = [] }
        };

        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(items));

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, 2, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);
        Assert.Equal("shell-1", result.Result[0].Id);
        Assert.Equal("shell-2", result.Result[1].Id);
        Assert.Equal("shell-2".EncodeBase64(), result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenResultFitsWithinPageSize_ReturnsAllItems_AndNullCursor()
    {
        var items = new List<ShellDescriptorData>
        {
            new() { GlobalAssetId = "asset-1", Id = "shell-1", IdShort = "Shell1", SpecificAssetIds = [] },
            new() { GlobalAssetId = "asset-2", Id = "shell-2", IdShort = "Shell2", SpecificAssetIds = [] }
        };

        _queryExecutor.ExecuteQueryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<DbParameter>>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(items));

        var result = await _sut.GetShellDescriptorsAsync(QueryWithMarkers, 2, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    #endregion

    #region GetShellDescriptorAsync

    [Fact]
    public async Task GetShellDescriptorAsync_WhenEmptyResult_ReturnsNull()
    {
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(string.Empty);

        var result = await _sut.GetShellDescriptorAsync("query", "aas-1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenValidJson_ReturnsProcessedObject()
    {
        var item = new ShellDescriptorData
        {
            GlobalAssetId = "asset-1",
            Id = "shell-1",
            IdShort = "Shell1",
            SpecificAssetIds =
            [
                new SpecificAssetIdsData { Name = null, Value = "VAL1" }
            ]
        };
        var json = JsonSerializer.Serialize(item);
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        var result = await _sut.GetShellDescriptorAsync("query", "aas-1", CancellationToken.None);

        Assert.Equal("shell-1", result!.Id);
        Assert.Equal("VAL1", result.SpecificAssetIds![0].Name);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenJsonIsNullLiteral_ReturnsNull()
    {
        var jsonNullLiteral = "null";
        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(jsonNullLiteral);

        var result = await _sut.GetShellDescriptorAsync(
            query: "query",
            aasIdentifier: "aas-1",
            cancellationToken: CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenIdIsInvalid_ThrowsValidationFailedException_AndLogsError()
    {
        var json = """
            {
              "globalAssetId": "asset-1",
              "id": "",
              "idShort": "Shell1"
            }
            """;

        _queryExecutor.ExecuteQueryAsync("query", Arg.Any<List<DbParameter>>(), Arg.Any<CancellationToken>()).Returns(json);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetShellDescriptorAsync("query", "aas-1", CancellationToken.None));

        Assert.True(HasLogged(_logger.ReceivedCalls(), LogLevel.Error, "Metadata-Shell has null or empty Id."));
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

    private static bool HasLogged(IEnumerable<ICall> calls, LogLevel level, string messageFragment)
        => calls.Any(call =>
        {
            if (call.GetMethodInfo().Name != "Log")
            {
                return false;
            }

            var args = call.GetArguments();
            if (args.Length < 3 || args[0] is not LogLevel actualLevel || actualLevel != level)
            {
                return false;
            }

            return args[2]?.ToString()?.Contains(messageFragment, StringComparison.Ordinal) == true;
        });
}
