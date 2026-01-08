using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;

public class QueryProvider(ILogger<QueryProvider> logger, IWebHostEnvironment env) : IQueryProvider
{
    private readonly string _basePath = Path.Combine(env.ContentRootPath, "Infrastructure", "DataAccess", "Queries");

    private const int MaxServiceNameLength = 100;

    public string? GetQuery(string serviceName)
    {
        ValidateServiceName(serviceName);

        var fileName = $"{serviceName}.sql";
        var path = Path.Combine(_basePath, fileName);

        if (!File.Exists(path))
        {
            logger.LogError("Query file not found: {ServiceName}", serviceName);

            return null;
        }

        return File.ReadAllText(path);
    }

    private void ValidateServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName) || serviceName.Length > MaxServiceNameLength || ContainsInvalidServiceNameCharacters(serviceName))
        {
            logger.LogError("Invalid service name provided: {ServiceName}", serviceName);
            throw new InvalidUserInputException();
        }
    }

    private static bool ContainsInvalidServiceNameCharacters(string serviceName)
    {
        // Explicit, cross-platform whitelist so behavior is consistent across Windows and Linux runners.
        // Allowed: letters, digits, underscore, hyphen.
        foreach (var c in serviceName)
        {
            if (char.IsLetterOrDigit(c) || c is '_' or '-')
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
