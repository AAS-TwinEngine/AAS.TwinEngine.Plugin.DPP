using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;

public interface IManifestProvider
{
    IList<string> GetSupportedSemanticIds();
}
