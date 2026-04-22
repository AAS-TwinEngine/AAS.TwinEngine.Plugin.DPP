using System.Text.RegularExpressions;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public class SubmodelMetadataExtractor(IOptions<ExtractionRules> options, ILogger<SubmodelMetadataExtractor> logger) : ISubmodelMetadataExtractor
{
    private readonly IList<ProductIdExtractionRule> _productIdExtractionRules = options.Value.ProductIdExtractionRules;
    private readonly IList<SubmodelNameExtractionRules> _submodelNameExtractionRules = options.Value.SubmodelNameExtractionRules;
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public SubmodelIdExtractionResult ExtractSubmodelMetadata(string submodelId)
    {
        if (string.IsNullOrWhiteSpace(submodelId))
        {
            logger.LogError("ProductId could not be extracted from the provided submodel Identifier.");
            throw new InvalidUserInputException();
        }

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
        foreach (var rule in _productIdExtractionRules)
        {
            var extracted = rule.Strategy switch
            {
                ExtractionStrategy.Regex => TryExtractWithRegex(submodelId, rule),
                ExtractionStrategy.Split => TryExtractWithSplit(submodelId, rule),
                _ => null
            };

            if (string.IsNullOrEmpty(extracted))
            {
                continue;
            }

            if (rule.ValidationPattern is not null &&
                !Regex.IsMatch(extracted, rule.ValidationPattern, RegexOptions.None, _regexTimeout))
            {
                continue;
            }

            return extracted;
        }

        logger.LogError("ProductId could not be extracted from the provided submodel Identifier.");
        throw new InvalidUserInputException();
    }

    private string? TryExtractWithRegex(string input, ProductIdExtractionRule rule)
    {
        try
        {
            var match = Regex.Match(input, rule.Pattern, RegexOptions.None, _regexTimeout);

            if (match.Success == false)
            {
                return null;
            }

            if (rule.Index >= match.Groups.Count)
            {
                return null;
            }

            var value = match.Groups[rule.Index].Value;

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static string? TryExtractWithSplit(string input, ProductIdExtractionRule rule)
    {
        var parts = input.Split(rule.Pattern);

        var startIndex = rule.Index;
        var endIndex = rule.EndIndex ?? rule.Index;

        if (endIndex >= parts.Length)
        {
            return null;
        }

        var extracted = string.Join(rule.Pattern, parts[startIndex..(endIndex + 1)]);

        return string.IsNullOrWhiteSpace(extracted) ? null : extracted;
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
