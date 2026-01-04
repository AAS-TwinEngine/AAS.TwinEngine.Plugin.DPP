using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Requests;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.MetaData.Handler;

public class MetaDataHandlerTests
{
    private readonly ILogger<MetaDataHandler> _logger = Substitute.For<ILogger<MetaDataHandler>>();
    private readonly IMetaDataService _metaDataService = Substitute.For<IMetaDataService>();
    private readonly MetaDataHandler _sut;

    public MetaDataHandlerTests() => _sut = new MetaDataHandler(_logger, _metaDataService);

    [Fact]
    public async Task GetShellDescriptors_ReturnsShellDescriptorsDto_WhenDescriptorsExist()
    {
        var request = new GetShellDescriptorsRequest(10, "cursor1234");
        var shellDescriptorsData = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            Result =
            [
                new() { Id = "desc1" },
                new() { Id = "desc2" }
            ]
        };
        _metaDataService
            .GetShellDescriptorsAsync(Arg.Is<int?>(10), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(shellDescriptorsData);

        var result = await _sut.GetShellDescriptors(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Result!.Count);
        Assert.Equal("desc1", result.Result![0].Id);
        Assert.Equal("desc2", result.Result![1].Id);
        Assert.Equal("nextCursor", result.PagingMetaData!.Cursor);
        await _metaDataService.Received(1)
            .GetShellDescriptorsAsync(10, Arg.Any<string>(), Arg.Any<CancellationToken>());
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Start executing get request for shell-descriptors")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task GetShellDescriptors_ThrowBadRequest_WhenLimitIsZero()
    {
        var request = new GetShellDescriptorsRequest(0, "cursor123");

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetShellDescriptors(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetShellDescriptors_ShouldWork_WhenRequestIsNull()
    {
        var shellDescriptorsData = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            Result = [new() { Id = "desc1" }]
        };
        _metaDataService
            .GetShellDescriptorsAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(shellDescriptorsData);

        var result = await _sut.GetShellDescriptors(null!, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Result!);
        Assert.Equal("desc1", result.Result![0].Id);
        Assert.Equal("nextCursor", result.PagingMetaData!.Cursor);
        await _metaDataService.Received(1)
            .GetShellDescriptorsAsync(null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptors_ThrowsNotFound_WhenServiceReturnsNull()
    {
        var request = new GetShellDescriptorsRequest(10, "cursor1234");
        _metaDataService
            .GetShellDescriptorsAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((ShellDescriptorsData)null!);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetShellDescriptors(request, CancellationToken.None));

        Assert.NotNull(ex);
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("shell-descriptors not found")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task GetShellDescriptor_ReturnsShellDescriptor_WhenExists_AndDecodesBase64()
    {
        var request = new GetShellDescriptorRequest("dGVzdA==");
        var shellMetaData = new ShellDescriptorData { Id = "test" };
        _metaDataService
            .GetShellDescriptorAsync(Arg.Is<string>(s => s == "test"), Arg.Any<CancellationToken>())
            .Returns(shellMetaData);

        var result = await _sut.GetShellDescriptor(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(shellMetaData.Id, result.Id);
        await _metaDataService.Received(1)
            .GetShellDescriptorAsync("test", Arg.Any<CancellationToken>());
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Start executing get request for shell-descriptor") &&
                                o.ToString()!.Contains("Identifier: test")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task GetShellDescriptor_ThrowsNotFoundException_WhenShellDoesNotExist()
    {
        var request = new GetShellDescriptorRequest("dGVzdA==");
        _metaDataService
            .GetShellDescriptorAsync(Arg.Is<string>(s => s == "test"), Arg.Any<CancellationToken>())
            .Returns((ShellDescriptorData)null!);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetShellDescriptor(request, CancellationToken.None));
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("shell-descriptor not found for Identifier: test")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task GetShellDescriptor_ThrowsNotFound_WhenIdentifierIsNull()
    {
        var request = new GetShellDescriptorRequest(null!);
        _metaDataService
            .GetShellDescriptorAsync(null!, Arg.Any<CancellationToken>())
            .Returns((ShellDescriptorData)null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.GetShellDescriptor(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsset_ReturnsAsset_WhenExists_AndDecodesBase64()
    {
        var request = new GetAssetRequest("dGVzdC1zaGVsbA==");
        var assetMetaData = new AssetData { GlobalAssetId = "test-shell" };
        _metaDataService
            .GetAssetAsync(Arg.Is<string>(s => s == "test-shell"), Arg.Any<CancellationToken>())
            .Returns(assetMetaData);

        var result = await _sut.GetAsset(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(assetMetaData.GlobalAssetId, result.GlobalAssetId);
        await _metaDataService.Received(1)
            .GetAssetAsync("test-shell", Arg.Any<CancellationToken>());
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Start executing get request for asset") &&
                                o.ToString()!.Contains("Identifier: test-shell")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task GetAsset_ThrowsNotFoundException_WhenAssetDoesNotExist()
    {
        var request = new GetAssetRequest("dGVzdC1zaGVsbA==");
        _metaDataService
            .GetAssetAsync(Arg.Is<string>(s => s == "test-shell"), Arg.Any<CancellationToken>())
            .Returns((AssetData)null!);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetAsset(request, CancellationToken.None));
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("asset not found for Identifier: test-shell")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }
}
