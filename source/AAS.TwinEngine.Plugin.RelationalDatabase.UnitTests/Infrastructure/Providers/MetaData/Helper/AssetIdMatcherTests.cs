using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.MetaData.Helper;

public class AssetIdMatcherTests
{
    [Fact]
    public void MatchesAllIdentifiers_WhenFilterIsEmpty_ReturnsTrue()
    {
        var shell = CreateShell("shell-1", "https://mm-software.com/ids/assets/000-001", [new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }]);
        var filter = new AssetIdFilterHeader { Identifiers = [] };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_WhenGlobalAssetIdMatches_ReturnsTrue()
    {
        var shell = CreateShell("shell-1", "https://mm-software.com/ids/assets/000-002", []);
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "https://mm-software.com/ids/assets/000-002" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_WhenSpecificAssetIdMatches_ReturnsTrue()
    {
        var shell = CreateShell("shell-1", "asset-1",
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]);
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_WhenMultipleIdentifiersAndAllMatch_ReturnsTrue()
    {
        var shell = CreateShell("shell-1", "asset-1",
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" },
                new SpecificAssetIdsData { Name = "batchId", Value = "B-2026-03" }
            ]);
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" },
                new SpecificAssetIdsData { Name = "batchId", Value = "B-2026-03" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_WhenOneOfMultipleIdentifiersDoesNotMatch_ReturnsFalse()
    {
        var shell = CreateShell("shell-1", "asset-1",
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]);
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" },
                new SpecificAssetIdsData { Name = "batchId", Value = "B-2026-03" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.False(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_WhenSpecificAssetIdsAreNull_ReturnsFalse()
    {
        var shell = CreateShell("shell-1", "asset-1", null);
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.False(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_WhenGlobalAssetIdMissingAndFilterRequestsGlobalAssetId_ReturnsFalse()
    {
        var shell = CreateShell("shell-1", null!, [new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }]);
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "https://mm-software.com/ids/assets/000-002" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.False(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_WhenNameOrValueMissingInShellSpecificAssetId_ReturnsFalse()
    {
        var shell = CreateShell("shell-1", "asset-1",
            [
                new SpecificAssetIdsData { Name = null, Value = "SN-4711" },
                new SpecificAssetIdsData { Name = "serialNumber", Value = null }
            ]);
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.False(result);
    }

    private static ShellDescriptorData CreateShell(string id, string globalAssetId, IList<SpecificAssetIdsData>? specificAssetIds)
    {
        return new ShellDescriptorData
        {
            Id = id,
            IdShort = "Shell",
            GlobalAssetId = globalAssetId,
            SpecificAssetIds = specificAssetIds
        };
    }
}
