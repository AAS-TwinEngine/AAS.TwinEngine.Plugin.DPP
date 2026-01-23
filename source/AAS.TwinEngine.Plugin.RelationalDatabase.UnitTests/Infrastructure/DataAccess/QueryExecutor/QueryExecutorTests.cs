using System.Data;
using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

using Microsoft.Extensions.Logging;

using Npgsql;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Executor = AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor.QueryExecutor;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.DataAccess.QueryExecutor;

public class QueryExecutorTests
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbDataReader _reader;
    private readonly ILogger<Executor> _logger;
    private readonly Executor _sut;

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
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("result-json");

        var result = await _sut.ExecuteQueryAsync(
            "SELECT 1",
            CancellationToken.None);

        Assert.Equal("result-json", result);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenFirstColumnIsNull_ReturnsNull()
    {
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenNoRows_ThrowsResourceNotFoundException()
    {
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None));
    }

    #endregion

    #region ExecuteQueryAsync (with parameters)

    [Fact]
    public async Task ExecuteQueryAsync_WithValidParameters_AddsParametersToCommand()
    {
        var parameter = new NpgsqlParameter("@Value", "test-value")
        {
            Direction = ParameterDirection.Input
        };
        var parameters = new List<DbParameter> { parameter };
        var parameterCollection = Substitute.For<DbParameterCollection>();
        _command.Parameters.Returns(parameterCollection);
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("value");

        var result = await _sut.ExecuteQueryAsync("SELECT @Value", parameters, CancellationToken.None);

        Assert.Equal("value", result);
        parameterCollection.Received(1).Add(parameter);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithMultipleValidParameters_ExecutesSuccessfully()
    {
        var parameters = new List<DbParameter>
        {
            new NpgsqlParameter("@Param1", "value1") { Direction = ParameterDirection.Input },
            new NpgsqlParameter("@Param2", 123) { Direction = ParameterDirection.Input },
            new NpgsqlParameter("@Param3", DateTime.UtcNow) { Direction = ParameterDirection.Input }
        };
        var parameterCollection = Substitute.For<DbParameterCollection>();
        _command.Parameters.Returns(parameterCollection);
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("result");

        var result = await _sut.ExecuteQueryAsync("SELECT @Param1, @Param2, @Param3", parameters, CancellationToken.None);

        Assert.Equal("result", result);
        parameterCollection.Received(3).Add(Arg.Any<DbParameter>());
        parameterCollection.Received(1).Add(Arg.Is<DbParameter>(p => p.ParameterName == "@Param1"));
        parameterCollection.Received(1).Add(Arg.Is<DbParameter>(p => p.ParameterName == "@Param2"));
        parameterCollection.Received(1).Add(Arg.Is<DbParameter>(p => p.ParameterName == "@Param3"));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithNullParameters_ExecutesSuccessfully()
    {
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("result");

        var result = await _sut.ExecuteQueryAsync("SELECT 1", null!, CancellationToken.None);

        Assert.Equal("result", result);
    }

    #endregion

    #region Query Validation Tests

    [Fact]
    public async Task ExecuteQueryAsync_WithEmptyQuery_ThrowsResourceNotValidException()
    {
        await Assert.ThrowsAsync<ResourceNotValidException>(() =>
            _sut.ExecuteQueryAsync(string.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithWhitespaceQuery_ThrowsResourceNotValidException()
    {
        await Assert.ThrowsAsync<ResourceNotValidException>(() =>
            _sut.ExecuteQueryAsync("   ", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithNullQuery_ThrowsResourceNotValidException()
    {
        await Assert.ThrowsAsync<ResourceNotValidException>(() =>
            _sut.ExecuteQueryAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithTooLongQuery_ThrowsResourceNotValidException()
    {
        var longQuery = new string('A', 100001);

        await Assert.ThrowsAsync<ResourceNotValidException>(() =>
            _sut.ExecuteQueryAsync(longQuery, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithMaximumAllowedQueryLength_ExecutesSuccessfully()
    {
        var maxQuery = new string('A', 100000);
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("result");

        var result = await _sut.ExecuteQueryAsync(maxQuery, CancellationToken.None);

        Assert.Equal("result", result);
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task ExecuteQueryAsync_WhenConnectionThrows_ThrowsResourceNotFoundException()
    {
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Throws(new InvalidOperationException());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenReaderThrows_ThrowsResourceNotFoundException()
    {
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _command.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Throws(new Exception("DB error"));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenParameterValidationFails_ThrowsResourceNotValidException()
    {
        var parameter = new NpgsqlParameter("@Param", "'; DROP TABLE Users; --")
        {
            Direction = ParameterDirection.Input
        };
        var parameters = new List<DbParameter> { parameter };

        await Assert.ThrowsAsync<ResourceNotValidException>(() => _sut.ExecuteQueryAsync("SELECT @Param", parameters, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenQueryValidationFails_LogsWarning()
    {
        await Assert.ThrowsAsync<ResourceNotValidException>(() =>
            _sut.ExecuteQueryAsync(string.Empty, CancellationToken.None));
        AssertLoggedError(_logger);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteQueryAsync_WithCancellationToken_PassesToDatabaseOperations()
    {
        using var cts = new CancellationTokenSource();
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("result");

        await _sut.ExecuteQueryAsync("SELECT 1", cts.Token);

        await _connection.Received(1).OpenAsync(cts.Token);
        await _command.Received(1).ExecuteReaderAsync(cts.Token);
    }

    #endregion

    #region Command Configuration Tests

    [Fact]
    public async Task ExecuteQueryAsync_SetsCommandTimeout()
    {
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("result");

        await _sut.ExecuteQueryAsync("SELECT 1", CancellationToken.None);

        Assert.Equal(30, _command.CommandTimeout);
    }

    [Fact]
    public async Task ExecuteQueryAsync_SetsCommandText()
    {
        const string expectedQuery = "SELECT * FROM Users WHERE Id = @Id";
        _connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(true);
        _reader.IsDBNullAsync(0, Arg.Any<CancellationToken>()).Returns(false);
        _reader.GetString(0).Returns("result");

        await _sut.ExecuteQueryAsync(expectedQuery, CancellationToken.None);

        Assert.Equal(expectedQuery, _command.CommandText);
    }

    #endregion

    #region Helper Methods

    private static void AssertLoggedAtLevel(ILogger logger, LogLevel logLevel, int expectedCallCount = 1)
    {
        logger.Received(expectedCallCount).Log(
                                               logLevel,
                                               Arg.Any<EventId>(),
                                               Arg.Any<object>(),
                                               Arg.Any<Exception>(),
                                               Arg.Any<Func<object, Exception?, string>>());
    }

    private static void AssertLoggedError(ILogger logger, int expectedCallCount = 1) => AssertLoggedAtLevel(logger, LogLevel.Error, expectedCallCount);

    private static void AssertLoggedWarning(ILogger logger, int expectedCallCount = 1) => AssertLoggedAtLevel(logger, LogLevel.Warning, expectedCallCount);

    #endregion

}
