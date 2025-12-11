using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.Manifest;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;

public class ManifestService(IManifestProvider manifestProvider, IOptions<Capabilities> capabilities) : IManifestService
{
    private readonly bool _hasShellDescriptor = capabilities.Value.HasShellDescriptor;
    private readonly bool _hasAssetInformation = capabilities.Value.HasAssetInformation;

    public ManifestData GetManifestData(CancellationToken cancellationToken)
    {
        try
        {
            var supportedSemanticIds = manifestProvider.GetSupportedSemanticIds();

            var manifestData = new ManifestData
            {
                SupportedSemanticIds = supportedSemanticIds,
                Capabilities = new CapabilitiesData { HasAssetInformation = _hasAssetInformation, HasShellDescriptor = _hasShellDescriptor }
            };
            return manifestData;
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
    }
}
