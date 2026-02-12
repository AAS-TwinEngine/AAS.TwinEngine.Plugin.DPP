using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.Providers;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.Shared;

public class HealthServiceTests
{
    private readonly IHealthProvider _healthProvider = Substitute.For<IHealthProvider>();
    private readonly IHealthService _sut;

    public HealthServiceTests() => _sut = new HealthService(_healthProvider);

    [Fact]
    public async Task IsHealthyAsync_ReturnsTrue_WhenProviderReportsHealthy()
    {
        _healthProvider.IsDatabaseHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.IsHealthyAsync(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthyAsync_ReturnsFalse_WhenProviderReportsUnhealthy()
    {
        _healthProvider.IsDatabaseHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.IsHealthyAsync(CancellationToken.None);

        Assert.False(result);
    }
}
