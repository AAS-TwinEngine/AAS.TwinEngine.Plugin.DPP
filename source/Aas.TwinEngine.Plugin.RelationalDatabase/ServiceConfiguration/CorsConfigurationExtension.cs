using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
internal static class CorsConfigurationExtension
{
    public static void ConfigureCorsServices(this WebApplicationBuilder builder)
    {
        var corsOptions = builder.Configuration.GetSection(CorsOptions.Section).Get<CorsOptions>()
            ?? throw new InvalidOperationException("CORS configuration is missing.");

        _ = builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsOptions.PolicyName, policy =>
            {
                if (corsOptions.AllowedOrigins.Length > 0)
                {
                    _ = policy.WithOrigins(corsOptions.AllowedOrigins);
                }

                if (corsOptions.AllowAnyHeader)
                {
                    _ = policy.AllowAnyHeader();
                }

                if (corsOptions.AllowAnyMethod)
                {
                    _ = policy.AllowAnyMethod();
                }

                if (corsOptions.AllowCredentials)
                {
                    _ = policy.AllowCredentials();
                }

                if (corsOptions.AllowAnyOriginFallback)
                {
                    _ = policy.SetIsOriginAllowed(_ => true);
                }
            });
        });
    }

    public static void UseCorsServices(this WebApplication app)
    {
        var policyName = app.Configuration["Cors:PolicyName"] ?? "CorsPolicy";
        _ = app.UseCors(policyName);
    }
}
