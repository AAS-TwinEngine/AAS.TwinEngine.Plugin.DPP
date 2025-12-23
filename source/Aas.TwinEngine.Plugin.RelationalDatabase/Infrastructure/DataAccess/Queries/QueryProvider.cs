using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;
public class QueryProvider(ILogger<QueryProvider> logger, IWebHostEnvironment env) : IQueryProvider
{
    private readonly string _basePath = Path.Combine(env.ContentRootPath, "Infrastructure", "DataAccess", "Queries");

    public string? GetQuery(string serviceName)
    {
        var fileName = serviceName + ".sql";
        var path = Path.Combine(_basePath, fileName);

        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }

        logger.LogWarning("Query file not found for {ServiceName}: {Path}", serviceName, path);
        return null;
    }
}
