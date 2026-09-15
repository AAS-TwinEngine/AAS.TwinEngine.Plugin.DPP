using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;

public interface ISubmodelDataProvider
{
    Task<SemanticTreeNode> GetSubmodelValuesAsync(string sqlQuery, string productId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, SemanticTreeNode>> GetSubmodelValuesAsync(
        string sqlQuery,
        IReadOnlyCollection<string> productIds,
        CancellationToken cancellationToken);
}
