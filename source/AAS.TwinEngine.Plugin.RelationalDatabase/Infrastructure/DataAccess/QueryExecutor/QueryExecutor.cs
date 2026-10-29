using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Observability;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Validators;

using Npgsql;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;

public class QueryExecutor(ILogger<QueryExecutor> logger, IDbConnectionFactory connectionFactory) : IQueryExecutor
{
    private const int DefaultCommandTimeout = 30;
    private const int MaxQueryLength = 100000; // 100KB

    public Task<string?> ExecuteQueryAsync(string query, CancellationToken cancellationToken)
       => ExecuteInternalAsync(query, null, cancellationToken);

    public Task<string?> ExecuteQueryAsync(string query, IEnumerable<DbParameter> parameters, CancellationToken cancellationToken)
        => ExecuteInternalAsync(query, parameters, cancellationToken);

    private async Task<string?> ExecuteInternalAsync(string query, IEnumerable<DbParameter>? parameters, CancellationToken cancellationToken)
    {
        using var span = PluginTracing.StartSpan(PluginTracing.Spans.QueryExecution);
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

    public async Task<string?> ExecuteInTransactionAsync(string query, IEnumerable<DbParameter> parameters, string advisoryLockName, CancellationToken cancellationToken)
    {
        using var span = PluginTracing.StartSpan(PluginTracing.Spans.QueryExecution);
        logger.LogDebug("Executing query in transaction");

        ValidateQuery(query);

        var parameterList = parameters.ToList();
        DbParameterValidator.ValidateParameters(parameterList, logger);

        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (var lockCommand = connection.CreateCommand())
            {
                lockCommand.Transaction = transaction;
                lockCommand.CommandText = "SELECT pg_advisory_xact_lock(hashtext(@LockName))";
                _ = lockCommand.Parameters.Add(new NpgsqlParameter("@LockName", advisoryLockName));
                _ = await lockCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            string? result = null;
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = query;
                command.CommandTimeout = DefaultCommandTimeout;
                foreach (var parameter in parameterList)
                {
                    _ = command.Parameters.Add(parameter);
                }

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && !await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                {
                    result = reader.GetString(0);
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            logger.LogError(ex, "Unique constraint violation while executing query");
            throw new ConflictException("Resource already exists.", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error executing query in transaction");
            throw new InternalDataProcessingException(ex);
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
