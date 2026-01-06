using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.MappingProfiles;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Requests;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Handler;

public class MetaDataHandler(
    ILogger<MetaDataHandler> logger,
    IMetaDataService metaDataService) : IMetaDataHandler
{
    public Task<ShellDescriptorsDto> GetShellDescriptors(GetShellDescriptorsRequest request, CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);
        request?.Cursor?.ValidateCursor(logger);

        return GetResourceAsync(
            "shell-descriptors",
            () => metaDataService.GetShellDescriptorsAsync(request?.Limit, request?.Cursor, cancellationToken)!,
            descriptors => descriptors.ToDto()
        );
    }

    public Task<ShellDescriptorDto> GetShellDescriptor(GetShellDescriptorRequest request, CancellationToken cancellationToken)
        => GetResourceByIdAsync(
            request?.aasIdentifier,
            "shell-descriptor",
            id => metaDataService.GetShellDescriptorAsync(id, cancellationToken)!,
            descriptor => descriptor.ToDto()
        );

    public Task<AssetDto> GetAsset(GetAssetRequest request, CancellationToken cancellationToken)
        => GetResourceByIdAsync(
            request?.shellIdentifier,
            "asset",
            id => metaDataService.GetAssetAsync(id, cancellationToken)!,
            asset => asset.ToDto()
        );

    private async Task<TDto> GetResourceAsync<TModel, TDto>(
        string resourceName,
        Func<Task<TModel?>> fetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        logger.LogInformation("Start executing get request for {ResourceName}", resourceName);

        var result = await fetchFunc().ConfigureAwait(false);
        ValidateResourceExists(result, resourceName);

        return mapFunc(result!);
    }

    private async Task<TDto> GetResourceByIdAsync<TModel, TDto>(
        string? encodedId,
        string resourceName,
        Func<string, Task<TModel?>> fetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encodedId, nameof(encodedId));

        var decodedId = encodedId.DecodeBase64(logger);
        logger.LogInformation("Start executing get request for {ResourceName}. Identifier: {DecodedId}", resourceName, decodedId);

        var result = await fetchFunc(decodedId!).ConfigureAwait(false);
        ValidateResourceExists(result, resourceName, decodedId!);

        return mapFunc(result!);
    }

    private void ValidateResourceExists<T>(T? result, string resourceName, string? decodedId = null)
    {
        if (result is null)
        {
            if (decodedId is not null)
            {
                logger.LogWarning("{ResourceName} not found for Identifier: {DecodedId}", resourceName, decodedId);
            }
            else
            {
                logger.LogWarning("{ResourceName} not found.", resourceName);
            }

            throw new NotFoundException();
        }
    }
}

