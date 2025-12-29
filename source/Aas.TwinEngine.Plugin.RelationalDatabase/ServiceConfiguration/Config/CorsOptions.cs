namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

internal sealed class CorsOptions
{
    public const string Section = "Cors";
    public string PolicyName { get; init; } = "CorsPolicy";
    public string[] AllowedOrigins { get; init; } = [];
    public bool AllowAnyHeader { get; init; }
    public bool AllowAnyMethod { get; init; }
    public bool AllowCredentials { get; init; }
    public bool AllowAnyOriginFallback { get; init; }
}
