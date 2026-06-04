using System.Text.RegularExpressions;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.Shared.InputValidation;

public static partial class InputValidationPatterns
{

    /// <summary>
    /// Validates that a string contains only XML-compatible characters (per AAS IDTA specification):
    /// tab (#x9), newline (#xA), carriage return (#xD), space through U+D7FF,
    /// U+E000 through U+FFFD, and supplementary characters U+10000 through U+10FFFF.
    /// Surrogates (U+D800–U+DFFF) are included to allow .NET surrogate-pair representation
    /// of supplementary code points.
    /// </summary>
    [GeneratedRegex(
        @"^[\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD\uD800-\uDBFF\uDC00-\uDFFF]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    public static partial Regex XmlCharacterPattern();
}
