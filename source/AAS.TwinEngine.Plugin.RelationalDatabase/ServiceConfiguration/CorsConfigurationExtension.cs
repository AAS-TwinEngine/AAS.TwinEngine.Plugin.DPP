using System.Diagnostics.CodeAnalysis;

using AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
internal static class CorsConfigurationExtension
{
    public static void ConfigureCorsServices(this WebApplicationBuilder builder)
    {
        var corsOptions = builder.Configuration.GetSection(CorsOptions.Section).Get<CorsOptions>() ?? throw new InvalidOperationException("CORS configuration is missing.");

        if (corsOptions.AllowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("CORS AllowedOrigins must be configured.");
        }

        var allowAnyOrigin = corsOptions.AllowedOrigins.Length == 1 && corsOptions.AllowedOrigins[0] == "*";

        _ = builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsOptions.PolicyName, policy =>
            {
                if (allowAnyOrigin)
                {
                    _ = policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    _ = policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
                }
            });
        });
    }

    public static void UseCorsServices(this WebApplication app)
    {
        var policyName = app.Configuration.GetValue<string>($"{CorsOptions.Section}:PolicyName") ?? "CorsPolicy";

        _ = app.UseCors(policyName);
    }
}
