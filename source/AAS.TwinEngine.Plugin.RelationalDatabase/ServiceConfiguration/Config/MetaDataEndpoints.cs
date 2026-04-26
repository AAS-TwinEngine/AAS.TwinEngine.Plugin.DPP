namespace AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

public class MetaDataEndpoints
{
    public const string Section = "MetaDataEndpoints";
    public required string Shells { get; set; }
    public required string Shell { get; set; }
    public required string Asset { get; set; }
}
