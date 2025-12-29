using System.Data;
using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;

using Microsoft.Data.SqlClient;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

[ExcludeFromCodeCoverage]
public class SqlConnectionFactory(SqlServerConfiguration sqlServerConfiguration) : IDbConnectionFactory
{
    private readonly SqlServerConfiguration _sqlServerConfiguration = sqlServerConfiguration ?? throw new ArgumentNullException(nameof(sqlServerConfiguration));

    public IDbConnection CreateConnection() => new SqlConnection(_sqlServerConfiguration.ConnectionString);
}
