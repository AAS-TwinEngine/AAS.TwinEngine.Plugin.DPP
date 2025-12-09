namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Configuration;

public class SqlServerConfiguration
{
    public const string Section = "SqlServer";

    public string ConnectionString { get; set; } = null!;
}
