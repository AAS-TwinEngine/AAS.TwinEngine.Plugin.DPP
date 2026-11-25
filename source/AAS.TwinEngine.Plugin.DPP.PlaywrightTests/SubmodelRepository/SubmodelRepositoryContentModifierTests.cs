using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.ContentModifiers;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Content-modifier ($metadata, $value, $reference, $path) tests for the Submodel Repository routes
/// </summary>
public class SubmodelRepositoryContentModifierTests : ContentModifierTestsBase
{
    private const string ListLimitQuery = "limit=1";

    protected override string SubmodelRoute => $"/submodels/{SubmodelIdentifierNameplate}";

    protected override string UnknownSubmodelRoute => $"/submodels/{Base64EncodeUrl("https://mm-software.com/submodel/000-001/DoesNotExist")}";

    protected override string DecodedSubmodelId => "https://mm-software.com/submodel/000-001/Nameplate";

    protected override string CollectionIdShortPath => "AddressInformation";

    protected override string LeafIdShortPath => "CountryOfOrigin";

    protected override string MlpIdShortPath => "ManufacturerName";

    protected override string ListIdShortPath => "Markings";

    protected override string ListItemCollectionIdShortPath => "Markings%255B0%255D";

    protected override string ListItemIdShortPath => "Markings%255B0%255D.MarkingName";

    [Fact]
    public async Task GetSubmodels_Metadata_WithLimit_ShouldReturnSubmodelsWithoutElements()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync($"/submodels?{ListLimitQuery}"));

        // Act
        var actual = await GetJsonAsync($"/submodels/$metadata?{ListLimitQuery}");

        // Assert
        Assert.Single(normal);
        ContentModifierExpectations.AssertJsonEquals(
            ContentModifierExpectations.SubmodelsMetadata(normal),
            ContentModifierExpectations.UnwrapPagedArray(actual),
            "Submodels $metadata");
    }

    [Fact]
    public async Task GetSubmodels_Value_WithLimit_ShouldReturnUnnamedValueOnlyObjects()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync($"/submodels?{ListLimitQuery}"));

        // Act
        var actual = await GetJsonAsync($"/submodels/$value?{ListLimitQuery}");

        // Assert
        var result = ContentModifierExpectations.UnwrapPagedArray(actual);
        Assert.Equal(normal.Count, result.Count);
        for (var index = 0; index < normal.Count; index++)
        {
            ContentModifierExpectations.AssertSubmodelValueOnly(result[index], normal[index]!);
        }
    }

    [Fact]
    public async Task GetSubmodels_Reference_WithLimit_ShouldReturnModelReferences()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync($"/submodels?{ListLimitQuery}"));

        // Act
        var actual = await GetJsonAsync($"/submodels/$reference?{ListLimitQuery}");

        // Assert
        ContentModifierExpectations.AssertJsonEquals(
            ContentModifierExpectations.SubmodelsReference(normal),
            ContentModifierExpectations.UnwrapPagedArray(actual),
            "Submodels $reference");
    }

    [Fact]
    public async Task GetSubmodels_Path_WithLimit_ShouldReturnPreOrderIdShortPaths()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync($"/submodels?{ListLimitQuery}"));
        var expected = ContentModifierExpectations.ToJsonArray(
            normal.SelectMany(submodel => ContentModifierExpectations.SubmodelPaths(submodel!)));

        // Act
        var actual = await GetJsonAsync($"/submodels/$path?{ListLimitQuery}");

        // Assert
        ContentModifierExpectations.AssertJsonEquals(expected, ContentModifierExpectations.UnwrapPagedArray(actual), "Submodels $path");
    }

    [Fact]
    public async Task GetSubmodels_Metadata_WithLevel_ShouldReturnBadRequest()
        => await AssertStatusAsync($"/submodels/$metadata?{ListLimitQuery}&level=core", 400);

    [Fact]
    public async Task GetSubmodels_Reference_WithLevelDeep_ShouldReturnBadRequest()
        => await AssertStatusAsync($"/submodels/$reference?{ListLimitQuery}&level=deep", 400);
}
