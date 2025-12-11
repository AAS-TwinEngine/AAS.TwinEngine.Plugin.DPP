using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Responses;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;

public interface IManifestHandler
{
    Task<ManifestDto> GetManifestData(CancellationToken cancellationToken);
}
