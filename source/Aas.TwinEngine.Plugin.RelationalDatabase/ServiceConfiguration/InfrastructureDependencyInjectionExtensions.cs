using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.SqlCommandExecutor;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData;

using Microsoft.Extensions.Options;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<SqlServerConfiguration>(configuration.GetSection(SqlServerConfiguration.Section));
        _ = services.AddSingleton(sp => sp.GetRequiredService<IOptions<SqlServerConfiguration>>().Value);
        _ = services.AddSingleton<IDbConnectionFactory, PostgresSqlConnectionFactory>();
        _ = services.AddScoped<IQueryProvider, QueryProvider>();
        _ = services.AddScoped<ISqlCommandExecutor, SqlCommandExecutor>();
        _ = services.AddScoped<IMetaDataProvider, MetaDataProvider>();
    }
}
