using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Monitoring;

public class DatabaseAvailabilityHealthCheck(IDbConnectionFactory connectionFactory, ILogger<DatabaseAvailabilityHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy();
        }
        catch (TimeoutException ex)
        {
            logger.LogError(ex, "Database health check timed out");
            return HealthCheckResult.Unhealthy();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database health check encountered an unexpected error");
            return HealthCheckResult.Unhealthy();
        }
    }
}
