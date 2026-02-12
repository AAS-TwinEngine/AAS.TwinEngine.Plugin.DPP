namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.Providers;

public interface IHealthProvider
{
    Task<bool> IsDatabaseHealthyAsync(CancellationToken cancellationToken);
}
