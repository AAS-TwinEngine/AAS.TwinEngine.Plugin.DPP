using Npgsql;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.DataAccess.ConnectionFactory;

public class PostgreSqlConnectionFactoryTests
{
    #region Constructor validation

    [Fact]
    public void Constructor_WhenConfigurationIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgreSqlConnectionFactory(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenConnectionStringIsNullOrWhitespace_ThrowsValidationFailedException(string? connectionString)
    {
        // Arrange
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = connectionString ?? string.Empty
        };

        // Act & Assert
        Assert.Throws<ValidationFailedException>(() => new PostgreSqlConnectionFactory(config));
    }

    [Fact]
    public void Constructor_WhenConnectionStringIsInvalid_ThrowsValidationFailedException()
    {
        // Arrange
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = "this-is-not-a-valid-connection-string"
        };

        // Act & Assert
        Assert.Throws<ValidationFailedException>(() => new PostgreSqlConnectionFactory(config));
    }

    [Fact]
    public void Constructor_WhenConnectionStringIsValid_DoesNotThrow()
    {
        // Arrange
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = "Host=localhost;Username=test;Password=test;Database=testdb"
        };

        // Act
        var exception = Record.Exception(() => new PostgreSqlConnectionFactory(config));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region CreateConnection

    [Fact]
    public void CreateConnection_ReturnsNpgsqlConnection()
    {
        // Arrange
        var connectionString = "Host=localhost;Username=test;Password=test;Database=testdb";
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = connectionString
        };

        var factory = new PostgreSqlConnectionFactory(config);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        Assert.NotNull(connection);
        Assert.IsType<NpgsqlConnection>(connection);
        Assert.Equal(connectionString, connection.ConnectionString);
    }

    #endregion
}
