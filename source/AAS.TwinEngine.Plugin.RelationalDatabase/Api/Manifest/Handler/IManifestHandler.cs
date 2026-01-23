using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Responses;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;

public interface IManifestHandler
{
    Task<ManifestDto> GetManifestData();
}
