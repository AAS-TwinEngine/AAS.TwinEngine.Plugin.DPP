namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

public class MappingItem
{
    public string Column { get; set; } = null!;
    public IList<string> SemanticId { get; init; } = null!;
}
