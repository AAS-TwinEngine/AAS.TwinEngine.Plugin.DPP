using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

public class ExtractionRulesValidator : IValidateOptions<ExtractionRules>
{
    public ValidateOptionsResult Validate(string? name, ExtractionRules options)
    {
        var errors = new List<string>();

        ValidateProductIdRules(options.ProductIdExtractionRules, errors);
        ValidateSubmodelNameRules(options.SubmodelNameExtractionRules, errors);

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
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
            var rule = rules[i];
            var label = rule.Description ?? $"ProductIdExtractionRule[{i}]";

            if (string.IsNullOrWhiteSpace(rule.Pattern))
            {
                errors.Add($"{label}: Pattern must not be empty.");
            }

            if (rule.Index < 1)
            {
                errors.Add($"{label}: Index must be >= 1.");
            }

            if (rule.EndIndex is not null && rule.EndIndex < rule.Index)
            {
                errors.Add($"{label}: EndIndex must be >= Index.");
            }

            if (rule.Strategy == ExtractionStrategy.Regex && !string.IsNullOrWhiteSpace(rule.Pattern))
            {
                try
                {
                    _ = Regex.Match(string.Empty, rule.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                }
                catch (ArgumentException)
                {
                    errors.Add($"{label}: Pattern is not a valid regex.");
                }
            }

            if (!string.IsNullOrWhiteSpace(rule.ValidationPattern))
            {
                try
                {
                    _ = Regex.Match(string.Empty, rule.ValidationPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                }
                catch (ArgumentException)
                {
                    errors.Add($"{label}: ValidationPattern is not a valid regex.");
                }
            }

            if (hasMultipleRules && string.IsNullOrWhiteSpace(rule.ValidationPattern))
            {
                errors.Add($"{label}: ValidationPattern is required when multiple rules are configured.");
            }
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
                try
                {
                    _ = Regex.Match(string.Empty, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                }
                catch (ArgumentException)
                {
                    errors.Add($"{label}: Pattern '{pattern}' is not a valid regex.");
                }
            }
        }
    }
}
