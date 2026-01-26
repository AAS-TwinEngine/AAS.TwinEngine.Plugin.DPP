namespace AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

internal sealed class CorsOptions
{
    public const string Section = "Cors";
    public string PolicyName { get; init; } = "CorsPolicy";
    public string[] AllowedOrigins { get; init; } = [];
}
