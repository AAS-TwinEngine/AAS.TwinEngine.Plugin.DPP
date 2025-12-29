using System.ComponentModel.DataAnnotations;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Config;

public class Capabilities
{
    public const string Section = "Capabilities";

    [Required]
    public bool HasShellDescriptor { get; set; }

    [Required]
    public bool HasAssetInformation { get; set; }
}
