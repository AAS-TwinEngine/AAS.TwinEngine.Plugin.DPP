using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Services;

public interface IAssetIdsFilterHeaderValidation
{
    AssetIdFilterHeader? ParseToDomainModel(string? headerValue);
}
