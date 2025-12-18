using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

using IQueryProvider = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Sharad.IQueryProvider;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SubmodelDataService(IOptions<ExtractionRules> options,
    IQueryProvider queryProvider,
    ISubmodelDataProvider submodelDataProvider,
    ILogger<SubmodelDataService> _logger) : ISubmodelDataService
{
    private readonly IList<ProductIdExtractionRules> _productIdExtractionRules = options.Value.ProductIdExtractionRules;
    private readonly IList<SubmodelNameExtractionRules> _submodelNameExtractionRules = options.Value.SubmodelNameExtractionRules;
    public async Task<SemanticTreeNode> GetValuesBySemanticIds(SemanticTreeNode semanticIds, string submodelId, CancellationToken cancellationToken)
    {
        var productId = GetProductIdFromRule(submodelId);

        var submodel = GetSubmodelNameFromRule(submodelId);

        if (!Enum.TryParse<SubmodelName>(submodel, ignoreCase: true, result: out var submodelName))
        {
            _logger.LogError("Submodel name '{SubmodelName}' is not recognized.", submodel);
            throw new NotFoundException($"Submodel name '{submodel}' is not recognized.");
        }

        var sqlQuery = queryProvider.GetQuery(submodelName.ToString());
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new InvalidOperationException($"SQL query not found for: shells");
        }

        var responseSemanticTreeNode = await submodelDataProvider.GetSubmodelValuesAsync(sqlQuery, productId, cancellationToken).ConfigureAwait(false);

        return responseSemanticTreeNode;
    }

    public string GetProductIdFromRule(string submodelId)
    {
        var productId = _productIdExtractionRules
            .Select(rule => new
            {
                Rule = rule,
                Parts = submodelId?.Split(rule.Separator)
            })
            .Where(x => x.Parts is { Length: >= 1 } && x.Rule.Index > 0 && x.Parts.Length >= x.Rule.Index)
            .Select(x => x.Parts![x.Rule.Index - 1])
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(productId))
        {
            return productId;
        }

        _logger.LogError("ProductId could not be extracted from the provided submodel Identifier.");
        throw new NotFoundException();
    }

    private string GetSubmodelNameFromRule(string submodelId)
    {
        var SubmodelName = _submodelNameExtractionRules
           .Select(rule => new
           {
               Rule = rule,
               Parts = submodelId?.Split(rule.Separator)
           })
           .Where(x => x.Parts is { Length: >= 1 } && x.Rule.Index > 0 && x.Parts.Length >= x.Rule.Index)
           .Select(x => x.Parts![x.Rule.Index - 1])
           .FirstOrDefault();

        if (!string.IsNullOrEmpty(SubmodelName))
        {
            return SubmodelName;
        }

        _logger.LogError("Submodel Name could not be extracted from the provided submodel Identifier.");
        throw new NotFoundException();
    }
}
