using System.Text.Json.Nodes;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.ContentModifiers;

/// <summary>
/// Shared content-modifier tests ($metadata, $value, $reference, $path) for the submodel routes of the
/// Submodel Repository and the AAS Repository (superpath). Each expectation is derived from the normal
/// representation that the same live API returns for the same resource.
/// </summary>
public abstract class ContentModifierTestsBase : ApiTestBase
{
    private const int ElementsLimit = 100;

    /// <summary>Route of the submodel under test, e.g. <c>/submodels/{id}</c> or <c>/shells/{aas}/submodels/{id}</c>.</summary>
    protected abstract string SubmodelRoute { get; }

    /// <summary>Same route layout as <see cref="SubmodelRoute"/> but for a submodel that does not exist.</summary>
    protected abstract string UnknownSubmodelRoute { get; }

    protected abstract string DecodedSubmodelId { get; }

    /// <summary>SubmodelElementCollection (URL-encoded idShortPath).</summary>
    protected abstract string CollectionIdShortPath { get; }

    /// <summary>Property (URL-encoded idShortPath).</summary>
    protected abstract string LeafIdShortPath { get; }

    /// <summary>MultiLanguageProperty (URL-encoded idShortPath).</summary>
    protected abstract string MlpIdShortPath { get; }

    /// <summary>SubmodelElementList (URL-encoded idShortPath).</summary>
    protected abstract string ListIdShortPath { get; }

    /// <summary>A SubmodelElementCollection that is an item of a list, e.g. <c>Markings%255B0%255D</c>.</summary>
    protected abstract string ListItemCollectionIdShortPath { get; }

    /// <summary>A Property inside a list item, e.g. <c>Markings%255B0%255D.MarkingName</c>.</summary>
    protected abstract string ListItemIdShortPath { get; }

    // ---------- Submodel: /{sm}/$metadata|$value|$reference|$path ----------

    [Fact]
    public async Task GetSubmodel_Metadata_ShouldReturnSubmodelWithoutElements()
    {
        // Arrange
        var normal = await GetJsonAsync(SubmodelRoute);

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$metadata");

        // Assert
        Assert.False(Assert.IsAssignableFrom<JsonObject>(actual).ContainsKey("submodelElements"));
        ContentModifierExpectations.AssertJsonEquals(ContentModifierExpectations.SubmodelMetadata(normal!), actual, "Submodel $metadata");
    }

    [Fact]
    public async Task GetSubmodel_Value_ShouldReturnUnnamedValueOnlyObject()
    {
        // Arrange
        var normal = await GetJsonAsync(SubmodelRoute);

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$value");

        // Assert
        ContentModifierExpectations.AssertSubmodelValueOnly(actual, normal!);
    }

    [Fact]
    public async Task GetSubmodel_Reference_ShouldReturnModelReference()
    {
        // Arrange
        var expected = ContentModifierExpectations.SubmodelReference(DecodedSubmodelId);

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$reference");

        // Assert
        ContentModifierExpectations.AssertJsonEquals(expected, actual, "Submodel $reference");
    }

    [Fact]
    public async Task GetSubmodel_Path_ShouldReturnPreOrderIdShortPaths()
    {
        // Arrange
        var normal = await GetJsonAsync(SubmodelRoute);
        var expected = ContentModifierExpectations.ToJsonArray(ContentModifierExpectations.SubmodelPaths(normal!));

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$path");

        // Assert
        Assert.NotEmpty(Assert.IsAssignableFrom<JsonArray>(actual));
        ContentModifierExpectations.AssertJsonEquals(expected, actual, "Submodel $path");
    }

    [Fact]
    public async Task GetSubmodel_Reference_WithLevelCore_ShouldReturnSuccess()
    {
        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$reference?level=core");

        // Assert
        ContentModifierExpectations.AssertJsonEquals(ContentModifierExpectations.SubmodelReference(DecodedSubmodelId), actual, "Submodel $reference?level=core");
    }

    [Fact]
    public async Task GetSubmodel_Metadata_WithLevel_ShouldReturnBadRequest()
        => await AssertStatusAsync($"{SubmodelRoute}/$metadata?level=core", 400);

    [Fact]
    public async Task GetSubmodel_Metadata_WithExtentWithBlobValue_ShouldReturnBadRequest()
        => await AssertStatusAsync($"{SubmodelRoute}/$metadata?extent=withBlobValue", 400);

    [Fact]
    public async Task GetSubmodel_Reference_WithLevelDeep_ShouldReturnBadRequest()
        => await AssertStatusAsync($"{SubmodelRoute}/$reference?level=deep", 400);

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodel_Modifier_UnknownSubmodel_ShouldReturnNotFound(string modifier)
        => await AssertStatusAsync($"{UnknownSubmodelRoute}/{modifier}", 404);

    // ---------- Submodel elements: /{sm}/submodel-elements/$metadata|$value|$reference|$path ----------

    [Fact]
    public async Task GetSubmodelElements_Metadata_ShouldReturnElementsWithoutValues()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync(ElementsUrl()));

        // Act
        var actual = await GetJsonAsync(ElementsUrl("$metadata"));

        // Assert
        Assert.NotEmpty(normal);
        ContentModifierExpectations.AssertJsonEquals(
            ContentModifierExpectations.ElementsMetadata(normal),
            ContentModifierExpectations.UnwrapPagedArray(actual),
            "Submodel elements $metadata");
    }

    [Fact]
    public async Task GetSubmodelElements_Value_ShouldReturnValueOnlyObject()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync(ElementsUrl()));

        // Act
        var actual = await GetJsonAsync(ElementsUrl("$value"));

        // Assert
        var result = ContentModifierExpectations.UnwrapPagedResult(actual);
        Assert.IsAssignableFrom<JsonObject>(result);
        ContentModifierExpectations.AssertElementsValueOnly(result, normal, "submodel-elements");
    }

    [Fact]
    public async Task GetSubmodelElements_Reference_ShouldReturnModelReferences()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync(ElementsUrl()));

        // Act
        var actual = await GetJsonAsync(ElementsUrl("$reference"));

        // Assert
        ContentModifierExpectations.AssertJsonEquals(
            ContentModifierExpectations.ElementsReference(DecodedSubmodelId, normal),
            ContentModifierExpectations.UnwrapPagedArray(actual),
            "Submodel elements $reference");
    }

    [Fact]
    public async Task GetSubmodelElements_Path_ShouldReturnPreOrderIdShortPaths()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync(ElementsUrl()));
        var expected = ContentModifierExpectations.ToJsonArray(ContentModifierExpectations.ElementsPaths(normal));

        // Act
        var actual = await GetJsonAsync(ElementsUrl("$path"));

        // Assert
        Assert.NotEmpty(expected);
        ContentModifierExpectations.AssertJsonEquals(expected, ContentModifierExpectations.UnwrapPagedArray(actual), "Submodel elements $path");
    }

    [Fact]
    public async Task GetSubmodelElements_Metadata_WithLevel_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementsUrl("$metadata", "level=core"), 400);

    [Fact]
    public async Task GetSubmodelElements_Metadata_WithExtentWithBlobValue_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementsUrl("$metadata", "extent=withBlobValue"), 400);

    [Fact]
    public async Task GetSubmodelElements_Reference_WithLevelDeep_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementsUrl("$reference", "level=deep"), 400);

    [Fact]
    public async Task GetSubmodelElements_Reference_WithLevelCore_ShouldReturnSuccess()
    {
        // Arrange
        var normal = ContentModifierExpectations.UnwrapPagedArray(await GetJsonAsync(ElementsUrl()));

        // Act
        var actual = await GetJsonAsync(ElementsUrl("$reference", "level=core"));

        // Assert
        ContentModifierExpectations.AssertJsonEquals(
            ContentModifierExpectations.ElementsReference(DecodedSubmodelId, normal),
            ContentModifierExpectations.UnwrapPagedArray(actual),
            "Submodel elements $reference?level=core");
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodelElements_Modifier_UnknownSubmodel_ShouldReturnNotFound(string modifier)
        => await AssertStatusAsync($"{UnknownSubmodelRoute}/submodel-elements/{modifier}", 404);

    // ---------- Submodel element: /{sm}/submodel-elements/{idShortPath}/$metadata ----------

    [Fact]
    public async Task GetSubmodelElement_Metadata_Collection_ShouldReturnElementWithoutValue()
        => await AssertElementMetadataAsync(CollectionIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Metadata_Property_ShouldReturnElementWithoutValue()
        => await AssertElementMetadataAsync(LeafIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Metadata_MultiLanguageProperty_ShouldReturnElementWithoutValue()
        => await AssertElementMetadataAsync(MlpIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Metadata_List_ShouldReturnElementWithoutValue()
        => await AssertElementMetadataAsync(ListIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Metadata_ListItemProperty_ShouldReturnElementWithoutValue()
        => await AssertElementMetadataAsync(ListItemIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Metadata_WithLevel_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementUrl(CollectionIdShortPath, "$metadata", "level=core"), 400);

    [Fact]
    public async Task GetSubmodelElement_Metadata_WithExtentWithBlobValue_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementUrl(CollectionIdShortPath, "$metadata", "extent=withBlobValue"), 400);

    // ---------- Submodel element: /{sm}/submodel-elements/{idShortPath}/$value ----------

    [Fact]
    public async Task GetSubmodelElement_Value_Collection_ShouldReturnValueOnlyObject()
        => await AssertElementValueAsync(CollectionIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Value_Property_ShouldReturnScalar()
        => await AssertElementValueAsync(LeafIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Value_MultiLanguageProperty_ShouldReturnLanguageObjectArray()
        => await AssertElementValueAsync(MlpIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Value_List_ShouldReturnArrayWithOneEntryPerItem()
        => await AssertElementValueAsync(ListIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Value_ListItemCollection_ShouldReturnValueOnlyObject()
        => await AssertElementValueAsync(ListItemCollectionIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Value_ListItemProperty_ShouldReturnScalar()
        => await AssertElementValueAsync(ListItemIdShortPath);

    // ---------- Submodel element: /{sm}/submodel-elements/{idShortPath}/$reference ----------

    [Fact]
    public async Task GetSubmodelElement_Reference_Collection_ShouldReturnModelReference()
        => await AssertElementReferenceAsync(CollectionIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Reference_Property_ShouldReturnModelReference()
        => await AssertElementReferenceAsync(LeafIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Reference_List_ShouldReturnModelReference()
        => await AssertElementReferenceAsync(ListIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Reference_ListItemProperty_ShouldReturnKeyPerSegmentAndIndex()
        => await AssertElementReferenceAsync(ListItemIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Reference_WithLevelCore_ShouldReturnSuccess()
        => await AssertElementReferenceAsync(CollectionIdShortPath, "level=core");

    [Fact]
    public async Task GetSubmodelElement_Reference_WithLevelDeep_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementUrl(CollectionIdShortPath, "$reference", "level=deep"), 400);

    // ---------- Submodel element: /{sm}/submodel-elements/{idShortPath}/$path ----------

    [Fact]
    public async Task GetSubmodelElement_Path_Collection_ShouldReturnOwnPathAndDescendants()
        => await AssertElementPathAsync(CollectionIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Path_List_ShouldReturnOwnPathAndIndexedItems()
        => await AssertElementPathAsync(ListIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Path_ListItemCollection_ShouldReturnIndexedPathAndDescendants()
        => await AssertElementPathAsync(ListItemCollectionIdShortPath);

    [Fact]
    public async Task GetSubmodelElement_Path_Property_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementUrl(LeafIdShortPath, "$path"), 400);

    [Fact]
    public async Task GetSubmodelElement_Path_MultiLanguageProperty_ShouldReturnBadRequest()
        => await AssertStatusAsync(ElementUrl(MlpIdShortPath, "$path"), 400);

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodelElement_Modifier_UnknownSubmodel_ShouldReturnNotFound(string modifier)
        => await AssertStatusAsync($"{UnknownSubmodelRoute}/submodel-elements/{CollectionIdShortPath}/{modifier}", 404);

    // ---------- helpers ----------

    protected string ElementsUrl(string? modifier = null, string? query = null)
        => $"{SubmodelRoute}/submodel-elements{(modifier is null ? string.Empty : "/" + modifier)}?{(query is null ? string.Empty : query + "&")}limit={ElementsLimit}";

    protected string ElementUrl(string idShortPath, string? modifier = null, string? query = null)
        => $"{SubmodelRoute}/submodel-elements/{idShortPath}{(modifier is null ? string.Empty : "/" + modifier)}{(query is null ? string.Empty : "?" + query)}";

    protected async Task<JsonNode?> GetJsonAsync(string url)
    {
        var response = await ApiContext.GetAsync(url);
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        return ContentModifierExpectations.ParseNode(content);
    }

    protected async Task AssertStatusAsync(string url, int expectedStatus)
    {
        var response = await ApiContext.GetAsync(url);

        Assert.Equal(expectedStatus, response.Status);
    }

    private async Task AssertElementMetadataAsync(string idShortPath)
    {
        // Arrange
        var normal = await GetJsonAsync(ElementUrl(idShortPath));

        // Act
        var actual = await GetJsonAsync(ElementUrl(idShortPath, "$metadata"));

        // Assert
        ContentModifierExpectations.AssertJsonEquals(ContentModifierExpectations.ElementMetadata(normal!), actual, $"{idShortPath} $metadata");
    }

    private async Task AssertElementValueAsync(string idShortPath)
    {
        // Arrange
        var normal = await GetJsonAsync(ElementUrl(idShortPath));

        // Act
        var actual = await GetJsonAsync(ElementUrl(idShortPath, "$value"));

        // Assert
        ContentModifierExpectations.AssertValueOnly(actual, normal!, isListItem: false, idShortPath);
    }

    private async Task AssertElementReferenceAsync(string idShortPath, string? query = null)
    {
        // Arrange
        var normalSubmodel = await GetJsonAsync(SubmodelRoute);
        var expected = ContentModifierExpectations.ElementReference(DecodedSubmodelId, normalSubmodel!, ContentModifierExpectations.DecodeIdShortPath(idShortPath));

        // Act
        var actual = await GetJsonAsync(ElementUrl(idShortPath, "$reference", query));

        // Assert
        ContentModifierExpectations.AssertJsonEquals(expected, actual, $"{idShortPath} $reference");
    }

    private async Task AssertElementPathAsync(string idShortPath)
    {
        // Arrange
        var normal = await GetJsonAsync(ElementUrl(idShortPath));
        Assert.True(ContentModifierExpectations.SupportsPath(normal!), $"{idShortPath} must be a container element to support $path.");
        var expected = ContentModifierExpectations.ToJsonArray(
            ContentModifierExpectations.ElementPaths(normal!, ContentModifierExpectations.DecodeIdShortPath(idShortPath)));

        // Act
        var actual = await GetJsonAsync(ElementUrl(idShortPath, "$path"));

        // Assert
        ContentModifierExpectations.AssertJsonEquals(expected, actual, $"{idShortPath} $path");
    }
}
