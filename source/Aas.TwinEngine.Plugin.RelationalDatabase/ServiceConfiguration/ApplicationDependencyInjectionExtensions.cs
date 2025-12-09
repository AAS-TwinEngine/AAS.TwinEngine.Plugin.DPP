using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = services.AddProblemDetails();
    }
}
