namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;

public interface IManifestProvider
{
    IList<string> GetSupportedSemanticIds();
}
