namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

public class MappingItem
{
    public string Column { get; set; } = null!;
    public IList<string> SemanticId { get; init; } = null!;
}
