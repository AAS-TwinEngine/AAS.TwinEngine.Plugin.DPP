using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Validators;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;

public class QueryExecutor(ILogger<QueryExecutor> logger, IDbConnectionFactory connectionFactory) : IQueryExecutor
{
    private const int DefaultCommandTimeout = 30;
    private const int MaxQueryLength = 100000; // 100KB

    public Task<string?> ExecuteQueryAsync(
       string query,
       CancellationToken cancellationToken)
       => ExecuteInternalAsync(query, null, cancellationToken);

    public Task<string?> ExecuteQueryAsync(
        string query,
        IEnumerable<DbParameter> parameters,
        CancellationToken cancellationToken)
        => ExecuteInternalAsync(query, parameters, cancellationToken);

    private async Task<string?> ExecuteInternalAsync(
        string query,
        IEnumerable<DbParameter>? parameters,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Executing query");

        ValidateQuery(query);

        DbParameterValidator.ValidateParameters(parameters, logger);
        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = DefaultCommandTimeout;

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    _ = command.Parameters.Add(parameter);
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogDebug("Query returned no rows");
                throw new ResourceNotFoundException();
            }

            return await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                       ? null
                       : reader.GetString(0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing query");
            throw new ResourceNotFoundException();
        }
    }

    private void ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogError("Empty query provided");
            throw new ResourceNotValidException("Query cannot be empty.");
        }

        if (query.Length > MaxQueryLength)
        {
            logger.LogError("Query exceeds maximum length: {Length}", query.Length);
            throw new ResourceNotValidException("Query is too long.");
        }
    }
}
