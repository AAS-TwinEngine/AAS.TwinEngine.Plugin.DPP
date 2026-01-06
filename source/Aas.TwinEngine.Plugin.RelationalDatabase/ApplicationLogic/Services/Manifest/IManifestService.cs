using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;

public interface IManifestService
{
    ManifestData GetManifestData();
}
