using System.Diagnostics.CodeAnalysis;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;

[ExcludeFromCodeCoverage]
public class QueryProvider(ILogger<QueryProvider> logger, IWebHostEnvironment env) : IQueryProvider
{
    private readonly string _basePath = Path.Combine(env.ContentRootPath, "Infrastructure", "DataAccess", "Queries");

    public string? GetQuery(string serviceName)
    {
        ValidateServiceName(serviceName);

        var fileName = $"{serviceName}.sql";
        var path = Path.Combine(_basePath, fileName);

        if (!File.Exists(path))
        {
            logger.LogWarning("Query file not found: {ServiceName}", serviceName);

            return null;
        }

        return File.ReadAllText(path);
    }

    private void ValidateServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName) ||
            serviceName.Contains("..", StringComparison.Ordinal) ||
            serviceName.Contains('/', StringComparison.Ordinal) ||
            serviceName.Contains('\\', StringComparison.Ordinal) ||
            serviceName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            logger.LogWarning("Invalid service name provided: {ServiceName}", serviceName);

            throw new ArgumentException($"Invalid service name: {serviceName}", nameof(serviceName));
        }
    }
}
