using System.Text.Json;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

public record MappingData
{
    public static JsonElement MappingJson { get; set; }
}
