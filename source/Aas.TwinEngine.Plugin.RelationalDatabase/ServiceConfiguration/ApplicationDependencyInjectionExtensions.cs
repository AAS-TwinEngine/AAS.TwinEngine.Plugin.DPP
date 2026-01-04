using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = services.AddProblemDetails();

        _ = services.AddScoped<IManifestService, ManifestService>();
        _ = services.AddScoped<IManifestHandler, ManifestHandler>();

        _ = services.AddScoped<IMetaDataHandler, MetaDataHandler>();
        _ = services.AddScoped<IMetaDataService, MetaDataService>();
    }
}
