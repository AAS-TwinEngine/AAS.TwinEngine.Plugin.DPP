namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Configuration;

public class MetaDataEndpoints
{
    public const string Section = "MetaDataEndpoints";
    public required string Shells { get; set; }
    public required string Shell { get; set; }
    public required string Asset { get; set; }
}
