namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

public interface IQueryProvider
{
    string? GetQuery(string serviceName);
}
