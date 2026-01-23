using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;

public interface IManifestService
{
    ManifestData GetManifestData();
}
