using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services)
    {
        _ = services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = services.AddProblemDetails();

        _ = services.AddScoped<IManifestService, ManifestService>();
        _ = services.AddScoped<IManifestHandler, ManifestHandler>();
    }
}
