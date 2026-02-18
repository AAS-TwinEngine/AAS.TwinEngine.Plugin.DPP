using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Monitoring;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Monitoring;

public class DatabaseAvailabilityHealthCheckTests
{
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ILogger<DatabaseAvailabilityHealthCheck> _logger = Substitute.For<ILogger<DatabaseAvailabilityHealthCheck>>();
    private readonly DatabaseAvailabilityHealthCheck _sut;

    public DatabaseAvailabilityHealthCheckTests() => _sut = new DatabaseAvailabilityHealthCheck(_connectionFactory, _logger);

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenConnectionOpensSuccessfully()
    {
        var connection = Substitute.For<DbConnection>();
        _connectionFactory.CreateConnection().Returns(connection);

        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var context = new HealthCheckContext();

        var result = await _sut.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenDbExceptionIsThrown()
    {
        var connection = Substitute.For<DbConnection>();
        _connectionFactory.CreateConnection().Returns(connection);

        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new TestDbException());

        var context = new HealthCheckContext();

        var result = await _sut.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private sealed class TestDbException : DbException
    {
        public TestDbException() : base("Test DB exception")
        {
        }

        public TestDbException(string message) : base(message)
        {
        }

        public TestDbException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
