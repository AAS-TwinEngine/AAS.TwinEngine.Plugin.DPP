using System.Text.RegularExpressions;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SubmodelMetadataExtractor(IOptions<ExtractionRules> options, ILogger<SubmodelMetadataExtractor> logger) : ISubmodelMetadataExtractor
{
    private readonly IList<ProductIdExtractionRules> _productIdExtractionRules = options.Value.ProductIdExtractionRules;
    private readonly IList<SubmodelNameExtractionRules> _submodelNameExtractionRules = options.Value.SubmodelNameExtractionRules;
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public SubmodelIdExtractionResult ExtractSubmodelMetadata(string submodelId)
    {
        var productId = ExtractProductId(submodelId);
        var submodelName = ExtractSubmodelName(submodelId);

        if (Enum.TryParse<SubmodelName>(submodelName, ignoreCase: true, result: out var parsedSubmodelName))
        {
            return new SubmodelIdExtractionResult(productId, parsedSubmodelName);
        }

        logger.LogError("Submodel name '{SubmodelName}' is not recognized.", submodelName);
        throw new InvalidUserInputException();
    }

    private string ExtractProductId(string submodelId)
    {
        var productId = _productIdExtractionRules
            .Select(rule => new
            {
                Rule = rule,
                Parts = submodelId?.Split(rule.Separator),
            })
            .Where(x => x.Parts is { Length: >= 1 } && x.Rule.Index > 0 && x.Parts.Length >= x.Rule.Index)
            .Select(x => x.Parts![x.Rule.Index - 1])
            .FirstOrDefault(extractedId => !string.Equals(extractedId, submodelId, StringComparison.Ordinal));

        if (!string.IsNullOrEmpty(productId))
        {
            return productId;
        }

        logger.LogError("ProductId could not be extracted from the provided submodel Identifier.");
        throw new InvalidUserInputException();
    }

    private string ExtractSubmodelName(string submodelId)
    {
        var submodelName = _submodelNameExtractionRules
            .Where(pattern => pattern.Pattern
                .Any(p => Regex.IsMatch(submodelId, p, RegexOptions.IgnoreCase | RegexOptions.Compiled, _regexTimeout)))
            .Select(templatePattern => templatePattern.SubmodelName)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(submodelName))
        {
            return submodelName;
        }

        logger.LogError("Submodel Name could not be extracted from the provided submodel Identifier.");
        throw new InvalidUserInputException();
    }
}
