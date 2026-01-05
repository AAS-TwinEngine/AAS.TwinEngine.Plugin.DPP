using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.MetaData;

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

        _sut = new MetaDataService(_queryProvider, _metaDataProvider, _logger);
    }

    #region GetShellDescriptorsAsync

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenQueryExists_ReturnsData()
    {
        // Arrange
        var sql = "SELECT * FROM shells";
        var expected = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "next" },
            Result = []
        };

        _queryProvider.GetQuery("shells").Returns(sql);
        _metaDataProvider
            .GetShellDescriptorsAsync(sql, 10, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _sut.GetShellDescriptorsAsync(10, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("next", result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenQueryMissing_ThrowsSqlQueryNotFoundException()
    {
        // Arrange
        _queryProvider.GetQuery("shells").Returns((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SqlQueryNotAvailableException>(() =>
            _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_WhenResourceNotFound_ThrowsShellMetaDataNotFoundException()
    {
        // Arrange
        var sql = "SELECT * FROM shells";

        _queryProvider.GetQuery("shells").Returns(sql);
        _metaDataProvider
            .GetShellDescriptorsAsync(sql, null, null, Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<ShellMetaDataNotFoundException>(() =>
            _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None));
    }

    #endregion

    #region GetShellDescriptorAsync

    [Fact]
    public async Task GetShellDescriptorAsync_WhenQueryExists_ReturnsShellDescriptor()
    {
        // Arrange
        var sql = "SELECT * FROM shell";
        var expected = new ShellDescriptorData
        {
            GlobalAssetId = "asset-1",
            Id = "aas-1",
            IdShort = "Shell1"
        };

        _queryProvider.GetQuery("shell").Returns(sql);
        _metaDataProvider
            .GetShellDescriptorAsync(sql, "aas-1", Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aas-1", result.Id);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenQueryMissing_ThrowsSqlQueryNotFoundException()
    {
        // Arrange
        _queryProvider.GetQuery("shell").Returns((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SqlQueryNotAvailableException>(() =>
            _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorAsync_WhenResourceNotFound_ThrowsShellMetaDataNotFoundException()
    {
        // Arrange
        var sql = "SELECT * FROM shell";

        _queryProvider.GetQuery("shell").Returns(sql);
        _metaDataProvider
            .GetShellDescriptorAsync(sql, "aas-1", Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<ShellMetaDataNotFoundException>(() =>
            _sut.GetShellDescriptorAsync("aas-1", CancellationToken.None));
    }

    #endregion

    #region GetAssetAsync

    [Fact]
    public async Task GetAssetAsync_WhenQueryExists_ReturnsAsset()
    {
        // Arrange
        var sql = "SELECT * FROM asset";
        var expected = new AssetData
        {
            GlobalAssetId = "asset-123"
        };

        _queryProvider.GetQuery("asset").Returns(sql);
        _metaDataProvider
            .GetAssetAsync(sql, "asset-123", Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _sut.GetAssetAsync("asset-123", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("asset-123", result.GlobalAssetId);
    }

    [Fact]
    public async Task GetAssetAsync_WhenQueryMissing_ThrowsSqlQueryNotFoundException()
    {
        // Arrange
        _queryProvider.GetQuery("asset").Returns((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SqlQueryNotAvailableException>(() =>
            _sut.GetAssetAsync("asset-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_WhenResourceNotFound_ThrowsMetaDataNotFoundException()
    {
        // Arrange
        var sql = "SELECT * FROM asset";

        _queryProvider.GetQuery("asset").Returns(sql);
        _metaDataProvider
            .GetAssetAsync(sql, "asset-1", Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<AssetMetaDataNotFoundException>(() =>
            _sut.GetAssetAsync("asset-1", CancellationToken.None));
    }

    #endregion
}
