using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Responses;

using Microsoft.AspNetCore.Mvc;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.Manifest;

public class ManifestControllerTests
{
    private readonly IManifestHandler _manifestHandler = Substitute.For<IManifestHandler>();
    private readonly ManifestController _sut;
    private readonly ManifestDto ManifestDtoValue = new() { Capabilities = new CapabilitiesDto(), SupportedSemanticIds = ["abc"] };
    public ManifestControllerTests() => _sut = new ManifestController(_manifestHandler);

    [Fact]
    public async Task RetrieveManifestDataAsync_ShouldReturnOk_WhenDataIsAvailable()
    {
        _ = _manifestHandler.GetManifestData(Arg.Any<CancellationToken>()).Returns(Task.FromResult(ManifestDtoValue));

        var result = await _sut.RetrieveManifestDataAsync(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(ManifestDtoValue, okResult.Value);
    }

    [Fact]
    public async Task RetrieveManifestDataAsync_ShouldReturn500_WhenHandlerThrows()
    {
        _manifestHandler.GetManifestData(Arg.Any<CancellationToken>())
                        .Returns<Task<ManifestDto>>(_ => throw new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RetrieveManifestDataAsync(CancellationToken.None));
    }
}
