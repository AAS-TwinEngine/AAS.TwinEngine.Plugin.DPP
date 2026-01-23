namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

public class ExtractionRules
{
    public const string Section = "ExtractionRules";
    public IList<SubmodelNameExtractionRules> SubmodelNameExtractionRules { get; init; } = [];
    public IList<ProductIdExtractionRules> ProductIdExtractionRules { get; init; } = [];
}
public class ProductIdExtractionRules
{
    public string Pattern { get; set; } = string.Empty;
    public int Index { get; set; }
    public string Separator { get; set; } = string.Empty;
}

public class SubmodelNameExtractionRules
{
    public string SubmodelName { get; set; } = string.Empty;
    public IList<string> Pattern { get; init; } = [];
}

