using System.Data.Common;

using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;

using Npgsql;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

public class PostgresSqlConnectionFactory(SqlServerConfiguration sqlServerConfiguration) : IDbConnectionFactory
{
    private readonly SqlServerConfiguration _sqlServerConfiguration = sqlServerConfiguration ?? throw new ArgumentNullException(nameof(sqlServerConfiguration));

    public DbConnection CreateConnection() => new NpgsqlConnection(_sqlServerConfiguration.ConnectionString);
}
