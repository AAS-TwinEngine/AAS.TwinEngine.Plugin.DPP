using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

public class ExtractionRules
{
    public const string Section = "ExtractionRules";
    public IList<SubmodelNameExtractionRules> SubmodelNameExtractionRules { get; init; } = [];
    public IList<ProductIdExtractionRule> ProductIdExtractionRules { get; init; } = [];
}

public enum ExtractionStrategy
{
    Regex,
    Split
}

public class ProductIdExtractionRule
{
    [Required] public ExtractionStrategy Strategy { get; set; }
    [Required] public string Pattern { get; set; } = string.Empty;
    [Required] public int Index { get; set; }
    public int? EndIndex { get; set; }
    public string? ValidationPattern { get; set; }
}

public class SubmodelNameExtractionRules
{
    public string SubmodelName { get; set; } = string.Empty;
    public IList<string> Pattern { get; init; } = [];
}
