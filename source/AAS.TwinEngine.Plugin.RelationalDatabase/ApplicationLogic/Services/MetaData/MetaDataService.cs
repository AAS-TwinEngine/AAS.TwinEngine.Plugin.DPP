using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Configuration;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

using Microsoft.Extensions.Options;

using IQueryProvider = AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;
namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;

public class MetaDataService(IQueryProvider queryProvider, IMetaDataProvider metaDataProvider, ILogger<MetaDataService> logger, IOptions<MetaDataEndpoints> metaDataEndpoints) : IMetaDataService
{
    public async Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var query = GetValidatedQuery(metaDataEndpoints.Value.Shells);
            var result = await metaDataProvider.GetShellDescriptorsAsync(query, limit, cursor, cancellationToken).ConfigureAwait(false);
            if (result?.Result != null)
            {
                return result;
            }

            logger.LogError("Shell descriptors not found for limit: {Limit}, cursor: {Cursor}", limit, cursor);
            throw new ShellMetaDataNotFoundException();
        }
        catch (Exception ex)
        {
            throw HandleMetaDataException(ex, inner => new ShellMetaDataNotFoundException(inner));
        }
    }

    public async Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var query = GetValidatedQuery(metaDataEndpoints.Value.Shell);
            var result = await metaDataProvider.GetShellDescriptorAsync(query, aasIdentifier, cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                return result;
            }

            logger.LogError("Shell descriptor not found for AAS Identifier: {AasIdentifier}", aasIdentifier);
            throw new ShellMetaDataNotFoundException();
        }
        catch (Exception ex)
        {
            throw HandleMetaDataException(ex, inner => new ShellMetaDataNotFoundException(inner));
        }
    }

    public async Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var query = GetValidatedQuery(metaDataEndpoints.Value.Asset);
            var result = await metaDataProvider.GetAssetAsync(query, assetIdentifier, cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                return result;
            }

            logger.LogError("Asset not found for Asset Identifier: {AssetIdentifier}", assetIdentifier);
            throw new AssetMetaDataNotFoundException();
        }
        catch (Exception ex)
        {
            throw HandleMetaDataException(ex, inner => new AssetMetaDataNotFoundException(inner));
        }
    }

    private string GetValidatedQuery(string endpointName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName, nameof(endpointName));

        var query = queryProvider.GetQuery(endpointName);
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogError("Query not found for endpoint: {EndpointName}", endpointName);
            throw new QueryNotAvailableException();
        }

        return query;
    }

    private static Exception HandleMetaDataException(Exception exception, Func<Exception, Exception> notFoundExceptionFactory)
    {
        return exception switch
        {
            ResourceNotFoundException ex => notFoundExceptionFactory(ex),

            ResourceNotValidException ex => notFoundExceptionFactory(ex),

            ResponseParsingException ex => new InternalDataProcessingException(ex),

            ValidationFailedException ex => new InternalDataProcessingException(ex),

            _ => exception
        };
    }
}
