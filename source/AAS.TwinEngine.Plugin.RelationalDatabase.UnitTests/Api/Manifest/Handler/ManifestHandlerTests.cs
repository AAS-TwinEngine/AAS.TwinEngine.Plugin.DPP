using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Responses;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.Manifest.Handler;

public class ManifestHandlerTests
{
    private readonly ILogger<ManifestHandler> _logger = Substitute.For<ILogger<ManifestHandler>>();
    private readonly IManifestService _manifestService = Substitute.For<IManifestService>();
    private readonly ManifestHandler _sut;

    public ManifestHandlerTests() => _sut = new ManifestHandler(_logger, _manifestService);

    [Fact]
    public async Task GetManifestData_ShouldReturnDto_WhenManifestIsAvailable()
    {
        var manifest = new ManifestData { Capabilities = new CapabilitiesData { HasAssetInformation = true, HasShellDescriptor = true }, SupportedSemanticIds = ["test"] };
        var expectedDto = new ManifestDto { Capabilities = new CapabilitiesDto { HasAssetInformation = true, HasShellDescriptor = true }, SupportedSemanticIds = ["test"] };
        _ = _manifestService.GetManifestData()
                        .Returns(await Task.FromResult(manifest));

        var result = await _sut.GetManifestData();

        Assert.Equal(expectedDto.ToString(), result.ToString());
    }

    [Fact]
    public async Task GetManifestData_ShouldThrowException_WhenServiceThrows()
    {
        _manifestService.GetManifestData()
                        .Throws(ex: new Exception("Service failure"));

        await Assert.ThrowsAsync<Exception>(_sut.GetManifestData);
    }

    [Fact]
    public async Task GetManifestData_ShouldThrowNotFound_WhenServiceReturnsNull()
    {
        _manifestService.GetManifestData()
                        .Returns(await Task.FromResult<ManifestData?>(null));

        var ex = await Assert.ThrowsAsync<NotFoundException>(_sut.GetManifestData);
        Assert.NotNull(ex);
    }
}
