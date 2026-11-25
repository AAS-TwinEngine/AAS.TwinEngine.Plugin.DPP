using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.ContentModifiers;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRepository;

/// <summary>
/// Content-modifier ($metadata, $value, $reference, $path) tests for the AAS Repository routes
/// </summary>
public class AasRepositoryContentModifierTests : ContentModifierTestsBase
{
    private const string DecodedAasId = "https://mm-software.com/ids/aas/000-001";

    protected override string SubmodelRoute => $"/shells/{AasIdentifier1}/submodels/{SubmodelIdentifierNameplate}";

    protected override string UnknownSubmodelRoute => $"/shells/{AasIdentifier1}/submodels/{Base64EncodeUrl("https://mm-software.com/submodel/000-001/DoesNotExist")}";

    protected override string DecodedSubmodelId => "https://mm-software.com/submodel/000-001/Nameplate";

    protected override string CollectionIdShortPath => "AddressInformation";

    protected override string LeafIdShortPath => "CountryOfOrigin";

    protected override string MlpIdShortPath => "ManufacturerName";

    protected override string ListIdShortPath => "Markings";

    protected override string ListItemCollectionIdShortPath => "Markings%255B0%255D";

    protected override string ListItemIdShortPath => "Markings%255B0%255D.MarkingName";

    [Fact]
    public async Task GetShells_Reference_WithLimit_ShouldReturnModelReferences()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync("/shells?limit=1"));

        // Act
        var actual = await GetJsonAsync("/shells/$reference?limit=1");

        // Assert
        Assert.Single(normal);
        ContentModifierExpectations.AssertJsonEquals(
            ContentModifierExpectations.ShellsReference(normal),
            ContentModifierExpectations.UnwrapPagedArray(actual),
            "Shells $reference");
    }

    [Fact]
    public async Task GetShell_Reference_ShouldReturnModelReference()
    {
        // Arrange
        var normal = await GetJsonAsync($"/shells/{AasIdentifier1}");
        Assert.Equal(DecodedAasId, normal!["id"]!.GetValue<string>());

        // Act
        var actual = await GetJsonAsync($"/shells/{AasIdentifier1}/$reference");

        // Assert
        ContentModifierExpectations.AssertJsonEquals(ContentModifierExpectations.ShellReference(DecodedAasId), actual, "Shell $reference");
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodel_Modifier_SubmodelNotBelongingToShell_ShouldReturnClientError(string modifier)
    {
        // Arrange
        var url = $"/shells/{AasIdentifier2}/submodels/{SubmodelIdentifierNameplate}/{modifier}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.InRange(response.Status, 400, 499);
    }
}
