using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Observability;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;
using AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;

public class ManifestService(IManifestProvider manifestProvider, IOptions<Capabilities> capabilities) : IManifestService
{
    private readonly bool _hasShellDescriptor = capabilities.Value.HasShellDescriptor;
    private readonly bool _hasAssetInformation = capabilities.Value.HasAssetInformation;
    private readonly bool _hasAssetIdSearch = capabilities.Value.HasAssetIdSearch;

    public ManifestData GetManifestData()
    {
        using var span = PluginTracing.StartSpan(PluginTracing.Spans.CollectingSupportedSemanticIds);

        try
        {
            var supportedSemanticIds = manifestProvider.GetSupportedSemanticIds();

            var manifestData = new ManifestData
            {
                SupportedSemanticIds = supportedSemanticIds,
                Capabilities = new CapabilitiesData { HasAssetInformation = _hasAssetInformation, HasShellDescriptor = _hasShellDescriptor, HasAssetIdSearch = _hasAssetIdSearch }
            };
            return manifestData;
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
    }
}
