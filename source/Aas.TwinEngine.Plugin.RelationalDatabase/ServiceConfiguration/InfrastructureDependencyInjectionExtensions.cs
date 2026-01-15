using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Configuration;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Manifest;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData.Helper;

using Microsoft.Extensions.Options;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

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
