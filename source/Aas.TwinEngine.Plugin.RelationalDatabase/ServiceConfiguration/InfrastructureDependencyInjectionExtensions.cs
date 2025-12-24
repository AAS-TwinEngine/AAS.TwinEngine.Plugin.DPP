using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.ManifestProvider;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

using Microsoft.Extensions.Options;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.Configure<SqlServerConfiguration>(configuration.GetSection(SqlServerConfiguration.Section));
        _ = services.AddSingleton(sp => sp.GetRequiredService<IOptions<SqlServerConfiguration>>().Value);
        _ = services.AddOptions<Capabilities>().Bind(configuration.GetSection(Capabilities.Section)).ValidateDataAnnotations().ValidateOnStart();
        _ = services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        _ = services.AddScoped<IQueryProvider, QueryProvider>();

        _ = services.AddScoped<MappingDataInitializer>();

        _ = services.AddScoped<IManifestProvider, ManifestProvider>();
    }
}
