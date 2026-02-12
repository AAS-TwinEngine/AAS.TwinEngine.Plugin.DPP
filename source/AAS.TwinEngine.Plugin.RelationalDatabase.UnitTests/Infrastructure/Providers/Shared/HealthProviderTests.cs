using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.Shared;

public class HealthProviderTests
{
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ILogger<HealthProvider> _logger = Substitute.For<ILogger<HealthProvider>>();
    private readonly HealthProvider _sut;

    public HealthProviderTests() => _sut = new HealthProvider(_connectionFactory, _logger);

    [Fact]
    public async Task IsDatabaseHealthyAsync_ReturnsTrue_WhenConnectionOpensSuccessfully()
    {
        var connection = Substitute.For<DbConnection>();
        _connectionFactory.CreateConnection().Returns(connection);

        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.IsDatabaseHealthyAsync(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsDatabaseHealthyAsync_ReturnsFalse_WhenDbExceptionIsThrown()
    {
        var connection = Substitute.For<DbConnection>();
        _connectionFactory.CreateConnection().Returns(connection);

        connection.OpenAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new TestDbException());

        var result = await _sut.IsDatabaseHealthyAsync(CancellationToken.None);

        Assert.False(result);
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
