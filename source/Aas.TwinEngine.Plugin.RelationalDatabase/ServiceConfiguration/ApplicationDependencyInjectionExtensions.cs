using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = services.AddProblemDetails();

        _ = services.AddScoped<IMetaDataHandler, MetaDataHandler>();
        _ = services.AddScoped<IMetaDataService, MetaDataService>();
    }
}
