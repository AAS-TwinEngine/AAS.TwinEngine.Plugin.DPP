using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Config;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Configuration;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Manifest;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData.Helper;

using Microsoft.Extensions.Options;

using IQueryProvider = AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.Configure<RelationalDatabaseConfiguration>(configuration.GetSection(RelationalDatabaseConfiguration.Section));
        _ = services.AddSingleton(sp => sp.GetRequiredService<IOptions<RelationalDatabaseConfiguration>>().Value);
        _ = services.AddSingleton<IDbConnectionFactory, PostgreSqlConnectionFactory>();
        _ = services.AddOptions<Capabilities>().Bind(configuration.GetSection(Capabilities.Section)).ValidateDataAnnotations().ValidateOnStart();
        _ = services.AddOptions<ExtractionRules>().Bind(configuration.GetSection(ExtractionRules.Section)).ValidateDataAnnotations().ValidateOnStart();
        _ = services.AddOptions<Semantics>().Bind(configuration.GetSection(Semantics.Section)).ValidateDataAnnotations().ValidateOnStart();
        _ = services.AddOptions<MetaDataEndpoints>().Bind(configuration.GetSection(MetaDataEndpoints.Section)).ValidateDataAnnotations().ValidateOnStart();
        _ = services.AddScoped<IQueryProvider, QueryProvider>();
        _ = services.AddScoped<IQueryExecutor, QueryExecutor>();

        _ = services.AddScoped<MappingDataInitializer>();

        _ = services.AddScoped<IManifestProvider, ManifestProvider>();

        _ = services.AddScoped<IMetaDataProvider, MetaDataProvider>();

        _ = services.AddScoped<ISubmodelDataProvider, SubmodelDataProvider>();
        _ = services.AddScoped<IJsonResponseParser, JsonResponseParser>();
    }
}
