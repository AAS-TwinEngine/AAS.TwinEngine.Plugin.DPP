using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration.Config;

public class Capabilities
{
    public const string Section = "Capabilities";

    [Required]
    public bool HasShellDescriptor { get; set; }

    [Required]
    public bool HasAssetInformation { get; set; }

    [Required]
    public bool HasAssetIdSearch { get; set; }

    [Required]
    public bool HasAssetKindTypeFilter { get; set; }
}
