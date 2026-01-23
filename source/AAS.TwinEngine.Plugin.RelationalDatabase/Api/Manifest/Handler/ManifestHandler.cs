using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.MappingProfiles;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Responses;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;

public class ManifestHandler(ILogger<ManifestHandler> logger,
                             IManifestService manifestService) : IManifestHandler
{
    public Task<ManifestDto> GetManifestData()
        => GetResource(
                            "manifest",
                            () => manifestService.GetManifestData()!,
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
