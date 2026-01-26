using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

using Npgsql;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.DataAccess.ConnectionFactory;

public class PostgreSqlConnectionFactoryTests
{
    #region Constructor validation

    [Fact]
    public void Constructor_WhenConfigurationIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PostgreSqlConnectionFactory(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenConnectionStringIsNullOrWhitespace_ThrowsValidationFailedException(string? connectionString)
    {
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = connectionString ?? string.Empty
        };

        Assert.Throws<ValidationFailedException>(() => new PostgreSqlConnectionFactory(config));
    }

    [Fact]
    public void Constructor_WhenConnectionStringIsInvalid_ThrowsValidationFailedException()
    {
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = "this-is-not-a-valid-connection-string"
        };

        Assert.Throws<ValidationFailedException>(() => new PostgreSqlConnectionFactory(config));
    }

    [Fact]
    public void Constructor_WhenConnectionStringIsValid_DoesNotThrow()
    {
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = "Host=localhost;Username=test;Password=test;Database=testdb"
        };

        var exception = Record.Exception(() => new PostgreSqlConnectionFactory(config));

        Assert.Null(exception);
    }

    #endregion

    #region CreateConnection

    [Fact]
    public void CreateConnection_ReturnsNpgsqlConnection()
    {
        var connectionString = "Host=localhost;Username=test;Password=test;Database=testdb";
        var config = new RelationalDatabaseConfiguration
        {
            ConnectionString = connectionString
        };

        var factory = new PostgreSqlConnectionFactory(config);

        var connection = factory.CreateConnection();

        Assert.NotNull(connection);
        Assert.IsType<NpgsqlConnection>(connection);
        Assert.Equal(connectionString, connection.ConnectionString);
    }

    #endregion
}
