using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

public class HealthProvider(IDbConnectionFactory connectionFactory, ILogger<HealthProvider> logger) : IHealthProvider
{
    public async Task<bool> IsDatabaseHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database health check failed");
            return false;
        }
    }
}
