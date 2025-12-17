using System.Text.Json;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

public record MappingData
{
    public static JsonDocument MappingJson { get; set; } = null!;
}
