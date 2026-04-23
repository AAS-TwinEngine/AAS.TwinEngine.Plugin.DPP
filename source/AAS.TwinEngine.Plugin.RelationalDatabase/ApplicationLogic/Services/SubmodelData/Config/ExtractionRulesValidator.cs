using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

public class ExtractionRulesValidator(ILogger<ExtractionRulesValidator> logger)
    : IValidateOptions<ExtractionRules>
{
    private const string GenericErrorMessage = "Invalid extraction rules configuration.";

    public ValidateOptionsResult Validate(string? name, ExtractionRules options)
    {
        var result = ValidateProductIdRules(options.ProductIdExtractionRules);
        if (result != ValidateOptionsResult.Success)
        {
            return result;
        }

        result = ValidateSubmodelNameRules(options.SubmodelNameExtractionRules);
        return result != ValidateOptionsResult.Success ? result : ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateProductIdRules(IList<ProductIdExtractionRule> rules)
    {
        if (rules.Count == 0)
        {
            return Fail("At least one ProductIdExtractionRule is required.");
        }

        var hasMultipleRules = rules.Count > 1;

        for (var i = 0; i < rules.Count; i++)
        {
            var result = ValidateSingleProductRule(rules[i], i, hasMultipleRules);
            if (result != ValidateOptionsResult.Success)
            {
                return result;
            }
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateSingleProductRule(ProductIdExtractionRule rule, int index, bool hasMultipleRules)
    {
        var label = $"ProductIdExtractionRule[{index}]";

        return ValidatePattern(rule.Pattern, label)
            ?? ValidateIndex(rule, label)
            ?? ValidateEndIndex(rule, label)
            ?? ValidateRegexPattern(rule, label)
            ?? ValidateValidationPattern(rule, label, hasMultipleRules)
            ?? ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult? ValidatePattern(string? pattern, string label) => string.IsNullOrWhiteSpace(pattern) ? Fail($"{label}: Pattern must not be empty.") : null;

    private ValidateOptionsResult? ValidateIndex(ProductIdExtractionRule rule, string label) => rule.Index < 0 ? Fail($"{label}: Index must be >= 0") : null;

    private ValidateOptionsResult? ValidateEndIndex(ProductIdExtractionRule rule, string label) => rule.EndIndex is not null && rule.EndIndex < rule.Index ? Fail($"{label}: EndIndex must be >= Index.") : null;

    private ValidateOptionsResult? ValidateRegexPattern(ProductIdExtractionRule rule, string label)
    {
        if (rule.Strategy == ExtractionStrategy.Regex &&
            !IsValidRegex(rule.Pattern))
        {
            return Fail($"{label}: Pattern is not a valid regex.");
        }

        return null;
    }

    private ValidateOptionsResult? ValidateValidationPattern(ProductIdExtractionRule rule, string label, bool hasMultipleRules)
    {
        if (rule.Strategy != ExtractionStrategy.Regex)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(rule.ValidationPattern) &&
            !IsValidRegex(rule.ValidationPattern))
        {
            return Fail($"{label}: ValidationPattern is not a valid regex.");
        }

        if (hasMultipleRules && string.IsNullOrWhiteSpace(rule.ValidationPattern))
        {
            return Fail($"{label}: ValidationPattern is required when multiple Regex rules are configured.");
        }

        return null;
    }

    private ValidateOptionsResult ValidateSubmodelNameRules(IList<SubmodelNameExtractionRules> rules)
    {
        for (var i = 0; i < rules.Count; i++)
        {
            var result = ValidateSingleSubmodelRule(rules[i], i);
            if (result != ValidateOptionsResult.Success)
            {
                return result;
            }
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateSingleSubmodelRule(SubmodelNameExtractionRules rule, int index)
    {
        var label = rule.SubmodelName ?? $"SubmodelNameExtractionRule[{index}]";

        return ValidateSubmodelName(rule.SubmodelName, index)
            ?? ValidateSubmodelPatterns(rule.Pattern, label)
            ?? ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult? ValidateSubmodelName(string? name, int index) => string.IsNullOrWhiteSpace(name) ? Fail($"SubmodelNameExtractionRule[{index}]: SubmodelName must not be empty.") : null;

    private ValidateOptionsResult? ValidateSubmodelPatterns(IList<string> patterns, string label)
    {
        if (patterns.Count == 0)
        {
            return Fail($"{label}: At least one pattern is required.");
        }

        foreach (var pattern in patterns.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (!IsValidRegex(pattern))
            {
                return Fail($"{label}: Pattern '{pattern}' is not a valid regex.");
            }
        }

        return null;
    }

    private ValidateOptionsResult Fail(string detailedMessage)
    {
        logger.LogError("ExtractionRules validation failed: {Error}", detailedMessage);
        return ValidateOptionsResult.Fail(GenericErrorMessage);
    }

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = Regex.Match(string.Empty, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
