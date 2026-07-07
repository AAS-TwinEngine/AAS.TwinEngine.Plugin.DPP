using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using IQueryProvider = AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.MetaData;

public class MetaDataServiceTests
{
    private readonly IQueryProvider _queryProvider;
    private readonly IMetaDataProvider _metaDataProvider;
    private readonly ILogger<MetaDataService> _logger;
    private readonly MetaDataService _sut;

    public MetaDataServiceTests()
    {
        _queryProvider = Substitute.For<IQueryProvider>();
        _metaDataProvider = Substitute.For<IMetaDataProvider>();
        _logger = Substitute.For<ILogger<MetaDataService>>();
        var metaDataEndpoints = Substitute.For<IOptions<MetaDataEndpoints>>();
        _ = metaDataEndpoints.Value.Returns(new MetaDataEndpoints { Shells = "Shells", Shell = "Shell", Asset = "Asset" });
        _sut = new MetaDataService(_queryProvider, _metaDataProvider, _logger, metaDataEndpoints);
    }

    #region GetShellDescriptorsAsync

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenQueryExists_ReturnsData()
    {
        var query = "SELECT * FROM shells";
        var expected = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "next" },
            Result = []
        };
        _queryProvider.GetQuery("Shells").Returns(query);
        _metaDataProvider
            .GetShellDescriptorsAsync(query, 10, null, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetShellDescriptorsAsync(10, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("next", result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenQueryMissing_ThrowsQueryNotFoundException()
    {
        _queryProvider.GetQuery("Shells").Returns((string?)null);

        await Assert.ThrowsAsync<QueryNotAvailableException>(() =>
            _sut.GetShellDescriptorsAsync(null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_ShellDescriptorsIsNull_ThrowsShellMetaDataNotFoundException()
    {
        var query = "SELECT * FROM shells";
        _queryProvider.GetQuery("Shells").Returns(query);
        _metaDataProvider
            .GetShellDescriptorsAsync(query, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShellDescriptorsData?>(null));

        await Assert.ThrowsAsync<ShellMetaDataNotFoundException>(() =>
            _sut.GetShellDescriptorsAsync(null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenFilterProvided_PassesFilterToProvider()
    {
        var query = "SELECT * FROM shells";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };
        var expected = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };
        _queryProvider.GetQuery("Shells").Returns(query);
        _metaDataProvider
            .GetShellDescriptorsAsync(query, 10, null, filter, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetShellDescriptorsAsync(10, null, filter, CancellationToken.None);

        Assert.NotNull(result);
        await _metaDataProvider.Received(1)
            .GetShellDescriptorsAsync(query, 10, null, filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenResultCollectionIsNull_ThrowsShellMetaDataNotFoundException()
    {
        var query = "SELECT * FROM shells";

        var response = new ShellDescriptorsData
        {
            Result = null
        };

        _queryProvider.GetQuery("Shells").Returns(query);

        _metaDataProvider
            .GetShellDescriptorsAsync(query, null, null, null, Arg.Any<CancellationToken>())
            .Returns(response);

        await Assert.ThrowsAsync<ShellMetaDataNotFoundException>(() =>
            _sut.GetShellDescriptorsAsync(null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenResourceNotFoundExceptionThrown_ThrowsShellMetaDataNotFoundException()
    {
        var query = "SELECT * FROM shells";

        _queryProvider.GetQuery("Shells").Returns(query);

        _metaDataProvider
            .GetShellDescriptorsAsync(query, null, null, null, Arg.Any<CancellationToken>())
            .Returns<Task<ShellDescriptorsData?>>(_ =>
                throw new ResourceNotFoundException("not found"));

        await Assert.ThrowsAsync<ShellMetaDataNotFoundException>(() =>
            _sut.GetShellDescriptorsAsync(null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenInternalDataProcessingExceptionThrown_RethrowsOriginalException()
    {
        var query = "SELECT * FROM shell";

        _queryProvider.GetQuery("Shell").Returns(query);

        _metaDataProvider
            .GetShellDescriptorAsync(query, "aas-1", Arg.Any<CancellationToken>())
            .Returns<Task<ShellDescriptorData?>>(_ =>
                throw new InternalDataProcessingException("unexpected"));

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None));
    }

    #endregion

    #region GetShellDescriptorAsync

    [Fact]
    public async Task GetShellDescriptorAsync_WhenQueryExists_ReturnsShellDescriptor()
    {
        var query = "SELECT * FROM shell";
        var expected = new ShellDescriptorData
        {
            GlobalAssetId = "asset-1",
            Id = "aas-1",
            IdShort = "Shell1"
        };
        _queryProvider.GetQuery("Shell").Returns(query);
        _metaDataProvider
            .GetShellDescriptorAsync(query, "aas-1", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("aas-1", result.Id);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenQueryMissing_ThrowsQueryNotFoundException()
    {
        _queryProvider.GetQuery("shell").Returns((string?)null);

        await Assert.ThrowsAsync<QueryNotAvailableException>(() =>
            _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenShellDescriptorDataIsNull_ThrowsShellMetaDataNotFoundException()
    {
        var query = "SELECT * FROM shell";
        _queryProvider.GetQuery("Shell").Returns(query);
        _metaDataProvider
            .GetShellDescriptorAsync(query, "aas-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShellDescriptorData?>(null));

        await Assert.ThrowsAsync<ShellMetaDataNotFoundException>(() =>
            _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenProviderReturnsInvalidData_ThrowsInternalDataProcessingException()
    {
        var query = "SELECT * FROM shell";
        _queryProvider.GetQuery("Shell").Returns(query);
        _metaDataProvider
            .GetShellDescriptorAsync(query, "aas-1", Arg.Any<CancellationToken>())
            .Returns<Task<ShellDescriptorData?>>(_ => throw new ValidationFailedException("ShellDescriptor Id is null or empty."));

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None));
    }

    #endregion

    #region GetAssetAsync

    [Fact]
    public async Task GetAssetAsync_WhenQueryExists_ReturnsAsset()
    {
        var query = "SELECT * FROM asset";
        var expected = new AssetData
        {
            GlobalAssetId = "asset-123"
        };
        _queryProvider.GetQuery("Asset").Returns(query);
        _metaDataProvider
            .GetAssetAsync(query, "asset-123", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetAssetAsync("asset-123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("asset-123", result.GlobalAssetId);
    }

    [Fact]
    public async Task GetAssetAsync_WhenQueryMissing_ThrowsQueryNotFoundException()
    {
        _queryProvider.GetQuery("Asset").Returns((string?)null);

        await Assert.ThrowsAsync<QueryNotAvailableException>(() =>
            _sut.GetAssetAsync("asset-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_WhenAssetDataIsNull_ThrowsMetaDataNotFoundException()
    {
        var query = "SELECT * FROM asset";
        _queryProvider.GetQuery("Asset").Returns(query);
        _metaDataProvider
            .GetAssetAsync(query, "asset-1", Arg.Any<CancellationToken>())
            .Returns((AssetData?)null);

        await Assert.ThrowsAsync<AssetMetaDataNotFoundException>(() =>
            _sut.GetAssetAsync("asset-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_WhenResourceNotValidExceptionThrown_ThrowsAssetMetaDataNotFoundException()
    {
        var query = "SELECT * FROM asset";

        _queryProvider.GetQuery("Asset").Returns(query);

        _metaDataProvider
            .GetAssetAsync(query, "asset-1", Arg.Any<CancellationToken>())
            .Returns<Task<AssetData?>>(_ =>
                throw new ResourceNotValidException("invalid"));

        await Assert.ThrowsAsync<AssetMetaDataNotFoundException>(() =>
            _sut.GetAssetAsync("asset-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_WhenResponseParsingExceptionThrown_ThrowsInternalDataProcessingException()
    {
        var query = "SELECT * FROM asset";

        _queryProvider.GetQuery("Asset").Returns(query);

        _metaDataProvider
            .GetAssetAsync(query, "asset-1", Arg.Any<CancellationToken>())
            .Returns<Task<AssetData?>>(_ =>
                throw new ResponseParsingException("parse error"));

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetAssetAsync("asset-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_WhenQueryIsWhitespace_ThrowsQueryNotAvailableException()
    {
        _queryProvider.GetQuery("Asset").Returns(" ");

        await Assert.ThrowsAsync<QueryNotAvailableException>(() =>
            _sut.GetAssetAsync("asset-1", CancellationToken.None));
    }

    #endregion
}
