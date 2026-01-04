namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

public class MappingItem
{
    public string Column { get; init; } = null!;
    public IList<string> SemanticId { get; init; } = null!;
}
