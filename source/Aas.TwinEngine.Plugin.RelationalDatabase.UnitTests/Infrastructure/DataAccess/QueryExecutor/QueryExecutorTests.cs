using System.Data.Common;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Executor = Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor.QueryExecutor;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.DataAccess.QueryExecutor;

public class QueryExecutorTests
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbDataReader _reader;
    private readonly ILogger<Executor> _logger;
    private readonly RelationalDatabase.Infrastructure.DataAccess.QueryExecutor.QueryExecutor _sut;

    public QueryExecutorTests()
    {
        _connectionFactory = Substitute.For<IDbConnectionFactory>();
        _connection = Substitute.For<DbConnection>();
        _command = Substitute.For<DbCommand>();
        _reader = Substitute.For<DbDataReader>();
        _logger = Substitute.For<ILogger<Executor>>();

        _connectionFactory.CreateConnection().Returns(_connection);
        _connection.CreateCommand().Returns(_command);
        _command.ExecuteReaderAsync(Arg.Any<CancellationToken>())
                .Returns(_reader);

        _sut = new Executor(_logger, _connectionFactory);
    }

    #region ExecuteQueryAsync (no parameters)

    [Fact]
    public async Task ExecuteQueryAsync_WhenRowExists_ReturnsStringValue()
    {
        // Arrange
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);

        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);

        _reader.GetString(0).Returns("result-json");

        // Act
        var result = await _sut.ExecuteQueryAsync(
            "SELECT 1",
            CancellationToken.None);

        // Assert
        Assert.Equal("result-json", result);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenFirstColumnIsNull_ReturnsNull()
    {
        // Arrange
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);

        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenNoRows_ThrowsResourceNotFoundException()
    {
        // Arrange
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None));
    }

    #endregion

    #region ExecuteQueryAsync (with parameters)

    [Fact]
    public async Task ExecuteQueryAsync_WithParameters_AddsParametersToCommand()
    {
        // Arrange
        var parameter = Substitute.For<DbParameter>();
        var parameters = new List<DbParameter> { parameter };

        var parameterCollection = Substitute.For<DbParameterCollection>();

        _command.Parameters.Returns(parameterCollection);

        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);

        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);

        _reader.GetString(0).Returns("value");

        // Act
        var result = await _sut.ExecuteQueryAsync("SELECT @p", parameters, CancellationToken.None);

        // Assert
        Assert.Equal("value", result);
        parameterCollection.Received(1).Add(parameter);
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task ExecuteQueryAsync_WhenConnectionThrows_ExceptionIsWrapped()
    {
        // Arrange
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Throws(new InvalidOperationException());

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenReaderThrows_ExceptionIsWrapped()
    {
        // Arrange
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Throws(new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None));
    }

    #endregion
}
