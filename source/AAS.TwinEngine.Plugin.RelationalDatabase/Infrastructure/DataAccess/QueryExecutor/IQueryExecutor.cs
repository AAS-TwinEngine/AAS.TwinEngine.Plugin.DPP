using System.Data.Common;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;

public interface IQueryExecutor
{
    Task<string?> ExecuteQueryAsync(string query, CancellationToken cancellationToken);

    Task<string?> ExecuteQueryAsync(string query, IEnumerable<DbParameter> parameters, CancellationToken cancellationToken);
}
