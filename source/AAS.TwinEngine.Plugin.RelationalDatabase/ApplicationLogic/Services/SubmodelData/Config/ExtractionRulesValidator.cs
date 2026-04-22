using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

public class ExtractionRulesValidator(ILogger<ExtractionRulesValidator> logger) : IValidateOptions<ExtractionRules>
{
    public ValidateOptionsResult Validate(string? name, ExtractionRules options)
    {
        var errors = new List<string>();

        ValidateProductIdRules(options.ProductIdExtractionRules, errors);
        ValidateSubmodelNameRules(options.SubmodelNameExtractionRules, errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                logger.LogError("ExtractionRules validation failed: {Error}", error);
            }

            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }

    private static void ValidateProductIdRules(IList<ProductIdExtractionRule> rules, List<string> errors)
    {
        if (rules.Count == 0)
        {
            errors.Add("At least one ProductIdExtractionRule is required.");
            return;
        }

        var hasMultipleRules = rules.Count > 1;

        for (var i = 0; i < rules.Count; i++)
        {
            ValidateSingleRule(rules[i], i, hasMultipleRules, errors);
        }
    }

    private static void ValidateSingleRule(ProductIdExtractionRule rule, int index, bool hasMultipleRules, List<string> errors)
    {
        var label = $"ProductIdExtractionRule[{index}]";

        ValidatePattern(rule, label, errors);
        ValidateIndexes(rule, label, errors);
        ValidateRegexPattern(rule, label, errors);

        if (rule.Strategy == ExtractionStrategy.Regex)
        {
            ValidateValidationPattern(rule, label, errors);

            if (hasMultipleRules && string.IsNullOrWhiteSpace(rule.ValidationPattern))
            {
                errors.Add($"{label}: ValidationPattern is required when multiple Regex rules are configured.");
            }
        }
    }

    private static void ValidatePattern(ProductIdExtractionRule rule, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            errors.Add($"{label}: Pattern must not be empty.");
        }
    }

    private static void ValidateIndexes(ProductIdExtractionRule rule, string label, List<string> errors)
    {
        if (rule.Strategy == ExtractionStrategy.Regex)
        {
            if (rule.Index < 1)
            {
                errors.Add($"{label}: Index must be >= 1 for Regex strategy.");
            }
        }
        else if (rule.Strategy == ExtractionStrategy.Split)
        {
            if (rule.Index < 0)
            {
                errors.Add($"{label}: Index must be >= 0 for Split strategy.");
            }
        }

        if (rule.EndIndex is not null && rule.EndIndex < rule.Index)
        {
            errors.Add($"{label}: EndIndex must be >= Index.");
        }
    }

    private static void ValidateRegexPattern(ProductIdExtractionRule rule, string label, List<string> errors)
    {
        if (rule.Strategy == ExtractionStrategy.Regex &&
            !string.IsNullOrWhiteSpace(rule.Pattern) &&
            !IsValidRegex(rule.Pattern))
        {
            errors.Add($"{label}: Pattern is not a valid regex.");
        }
    }

    private static void ValidateValidationPattern(ProductIdExtractionRule rule, string label, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(rule.ValidationPattern) &&
            !IsValidRegex(rule.ValidationPattern))
        {
            errors.Add($"{label}: ValidationPattern is not a valid regex.");
        }
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

    private static void ValidateSubmodelNameRules(IList<SubmodelNameExtractionRules> rules, List<string> errors)
    {
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var label = rule.SubmodelName ?? $"SubmodelNameExtractionRule[{i}]";

            if (string.IsNullOrWhiteSpace(rule.SubmodelName))
            {
                errors.Add($"SubmodelNameExtractionRule[{i}]: SubmodelName must not be empty.");
            }

            if (rule.Pattern.Count == 0)
            {
                errors.Add($"{label}: At least one pattern is required.");
            }

            foreach (var pattern in rule.Pattern.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                if (!IsValidRegex(pattern))
                {
                    errors.Add($"{label}: Pattern '{pattern}' is not a valid regex.");
                }
            }
        }
    }
}
