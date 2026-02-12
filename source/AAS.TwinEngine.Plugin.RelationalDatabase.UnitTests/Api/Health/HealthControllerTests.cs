using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Health;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.Health;

public class HealthControllerTests
{
    private readonly IHealthService _healthService = Substitute.For<IHealthService>();
    private readonly ILogger<HealthController> _logger = Substitute.For<ILogger<HealthController>>();
    private readonly HealthController _sut;

    public HealthControllerTests() => _sut = new HealthController(_logger, _healthService);

    [Fact]
    public async Task GetAsync_ReturnsOk_WhenHealthServiceReportsHealthy()
    {
        _healthService.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.GetAsync(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetAsync_ReturnsServiceUnavailable_WhenHealthServiceReportsUnhealthy()
    {
        _healthService.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.GetAsync(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
    }
}
