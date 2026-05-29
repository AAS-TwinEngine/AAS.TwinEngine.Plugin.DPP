using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.AssetIdFilter;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

public static class AssetIdMatcher
{
    public static bool MatchesAllIdentifiers(ShellDescriptorData shellDescriptor, AssetIdFilterHeader filter)
    {
        if (filter == null || filter.Identifiers.Count == 0)
        {
            return true;
        }

        return filter.Identifiers.All(identifier => MatchesSingleIdentifier(shellDescriptor, identifier));
    }

    private static bool MatchesSingleIdentifier(ShellDescriptorData shellDescriptor, SpecificAssetIdData identifier)
    {
        if (string.Equals(identifier.Name, "globalAssetId", StringComparison.Ordinal))
        {
            return MatchesGlobalAssetId(shellDescriptor.GlobalAssetId, identifier.Value);
        }

        return MatchesSpecificAssetId(shellDescriptor.SpecificAssetIds, identifier);
    }

    private static bool MatchesGlobalAssetId(string? shellGlobalAssetId, string identifierValue)
    {
        if (string.IsNullOrEmpty(shellGlobalAssetId))
        {
            return false;
        }

        return string.Equals(shellGlobalAssetId, identifierValue, StringComparison.Ordinal);
    }

    private static bool MatchesSpecificAssetId(IList<SpecificAssetIdsData>? shellAssets, SpecificAssetIdData identifier)
    {
        if (shellAssets == null || shellAssets.Count == 0)
        {
            return false;
        }

        return shellAssets.Any(shellAsset => MatchesNameAndValue(shellAsset, identifier));
    }

    private static bool MatchesNameAndValue(SpecificAssetIdsData shellAsset, SpecificAssetIdData identifier)
    {
        if (string.IsNullOrEmpty(shellAsset.Name) || string.IsNullOrEmpty(shellAsset.Value))
        {
            return false;
        }

        return string.Equals(shellAsset.Name, identifier.Name, StringComparison.Ordinal)
               && string.Equals(shellAsset.Value, identifier.Value, StringComparison.Ordinal);
    }
}
