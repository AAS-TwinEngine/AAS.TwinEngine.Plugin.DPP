using System.Data.Common;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;

using Npgsql;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

public sealed class PostgreSqlConnectionFactory : IDbConnectionFactory
{
    private readonly RelationalDatabaseConfiguration _configuration;

    public PostgreSqlConnectionFactory(RelationalDatabaseConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        ValidateConnectionString(_configuration.ConnectionString);
    }

    public DbConnection CreateConnection() => new NpgsqlConnection(_configuration.ConnectionString);

    private static void ValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ValidationFailedException();
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception)
        {
            throw new ValidationFailedException();
        }
    }
}
