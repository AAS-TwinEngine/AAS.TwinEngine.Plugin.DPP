using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.MappingProfiles;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Responses;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;

public class ManifestHandler(ILogger<ManifestHandler> logger,
                             IManifestService manifestService) : IManifestHandler
{
    public Task<ManifestDto> GetManifestData(CancellationToken cancellationToken)
        => GetResource(
                            "manifest",
                            () => manifestService.GetManifestData(cancellationToken)!,
                            manifest => manifest.ToDto()
                           );

    private Task<TDto> GetResource<TModel, TDto>(
        string resourceName,
        Func<TModel?> fetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        logger.LogInformation("Start executing get request for {ResourceName}", resourceName);

        var result = fetchFunc();
        ValidateResourceExists(result, resourceName);

        return Task.FromResult(mapFunc(result!));
    }

    private void ValidateResourceExists<T>(T? result, string resourceName)
    {
        if (result is null)
        {
            logger.LogWarning("{ResourceName} not found.", resourceName);
            throw new NotFoundException();
        }
    }
}
