namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

public interface IHealthService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}
