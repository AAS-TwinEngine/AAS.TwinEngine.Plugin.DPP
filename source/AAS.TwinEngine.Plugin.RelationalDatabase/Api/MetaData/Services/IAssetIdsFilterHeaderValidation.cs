using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.AssetIdFilter;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Services;

public interface IAssetIdsFilterHeaderValidation
{
    AssetIdFilterHeader? ParseToDomainModel(string? headerValue);
}
