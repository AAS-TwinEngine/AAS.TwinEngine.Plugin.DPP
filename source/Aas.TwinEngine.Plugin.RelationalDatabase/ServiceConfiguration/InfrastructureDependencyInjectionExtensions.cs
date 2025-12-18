using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers;

using Microsoft.Extensions.Options;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Sharad.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<SqlServerConfiguration>(configuration.GetSection(SqlServerConfiguration.Section));
        _ = services.AddSingleton(sp => sp.GetRequiredService<IOptions<SqlServerConfiguration>>().Value);
        _ = services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        _ = services.AddScoped<IQueryProvider, QueryProvider>();

        _ = services.Configure<ExtractionRules>(configuration.GetSection(ExtractionRules.Section));
        _ = services.AddScoped<ISubmodelDataProvider, SubmodelDataProvider>();
    }
}
