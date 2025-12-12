using System.ComponentModel.DataAnnotations;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

public class Semantics
{
    public const string Section = "Semantics";

    [Required]
    public string IndexContextPrefix { get; set; }
}

