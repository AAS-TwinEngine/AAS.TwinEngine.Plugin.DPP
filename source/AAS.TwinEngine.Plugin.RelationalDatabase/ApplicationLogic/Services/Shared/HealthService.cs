using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.Providers;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

public class HealthService(IHealthProvider healthProvider) : IHealthService
{
    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
        => healthProvider.IsDatabaseHealthyAsync(cancellationToken);
}
