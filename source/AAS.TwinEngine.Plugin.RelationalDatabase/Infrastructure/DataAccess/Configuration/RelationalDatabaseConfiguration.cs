namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;

public class RelationalDatabaseConfiguration
{
    public const string Section = "RelationalDatabaseConfiguration";

    public string ConnectionString { get; set; } = null!;
}
