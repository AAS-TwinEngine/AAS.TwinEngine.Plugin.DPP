using System.Text.Json;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;

public record MappingData
{
    public static JsonElement MappingJson { get; set; }
}
