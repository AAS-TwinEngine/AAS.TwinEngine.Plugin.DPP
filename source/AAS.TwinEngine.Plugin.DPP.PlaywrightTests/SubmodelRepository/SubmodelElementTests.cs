using System.Text.Json;

using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRegistry;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for Submodel Element endpoints
/// </summary>
public class SubmodelElementTests : ApiTestBase
{
    [Fact]
    public async Task GetSubmodelElement_MaintenanceInstructions_MaintenanceToolList_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierMaintenanceInstructions}/submodel-elements/MaintenanceToolList%255B0%255D.OrderCodeToolOfManufacturer";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_MaintenanceInstructions_MaintenanceToolList_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelElement_HandoverDocumentation_Documents0_DocumentVersions0_Language0_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements/Documents%255B0%255D.DocumentVersions%255B0%255D.Language%255B0%255D";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_HandoverDocumentation_Documents0_DocumentVersions0_Language0.json"));
    }

    [Fact]
    public async Task GetSubmodelElement_CarbonFootprint_OperatingConditionsOfReliabilityCharacteristics_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierCarbonFootprint}/submodel-elements/ProductCarbonFootprints";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_CarbonFootprint_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelElement_TechnicalData_GeneralInformation_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierTechnicalData}/submodel-elements/GeneralInformation";
        // Act
        var response = await ApiContext.GetAsync(url);
        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_TechnicalData_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelElement_Nameplate_AddressInformations_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierNameplate}/submodel-elements/AddressInformation";
        // Act
        var response = await ApiContext.GetAsync(url);
        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_Nameplate_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelElements_Nameplate_WithQueryParameters_ShouldReturnSuccess_AndContainExpectedElements()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierNameplate}/submodel-elements?limit=100&level=deep&extent=withBlobValue";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodels_Submodel_Identifiers_Nameplate_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelElements_Nameplate_WithLimit1_ShouldReturnSuccess_AndSingleResult()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierNameplate}/submodel-elements?limit=1";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodels_Submodel_Identifiers_Limit_Nameplate_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelElements_Nameplate_WithLimit1AndCursor_ShouldReturnSuccess_AndSingleResult()
    {
        // Arrange
        var cursor = "VVJJT2ZUaGVQcm9kdWN0";
        var url = $"/submodels/{SubmodelIdentifierNameplate}/submodel-elements?limit=1&cursor={cursor}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodels_Submodel_Identifiers_Limit_And_Cursor_Nameplate_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelElements_Nameplate_WithCursor_ShouldReturnSuccess_AndExcludeCursorElement()
    {
        // Arrange
        var cursor = "VVJJT2ZUaGVQcm9kdWN0";
        var url = $"/submodels/{SubmodelIdentifierNameplate}/submodel-elements?cursor={cursor}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodels_Submodel_Identifiers_Cursor_Nameplate_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelElements_Nameplate_WithPagination()
    {
        // Arrange
        var urlLimit2 = $"/submodels/{SubmodelIdentifierNameplate}/submodel-elements?limit=2";
        var urlLimit3 = $"/submodels/{SubmodelIdentifierNameplate}/submodel-elements?limit=3";

        // Act
        var responseLimit2 = await ApiContext.GetAsync(urlLimit2);
        var responseLimit3 = await ApiContext.GetAsync(urlLimit3);

        // Assert
        AssertSuccessResponse(responseLimit2);
        AssertSuccessResponse(responseLimit3);

        var contentLimit2 = await responseLimit2.TextAsync();
        var contentLimit3 = await responseLimit3.TextAsync();

        Assert.False(string.IsNullOrEmpty(contentLimit2));
        Assert.False(string.IsNullOrEmpty(contentLimit3));

        var jsonLimit2 = JsonDocument.Parse(contentLimit2);
        var jsonLimit3 = JsonDocument.Parse(contentLimit3);

        Assert.NotNull(jsonLimit2);
        Assert.NotNull(jsonLimit3);

        // Verify that limit 3 contains one more element than limit 2
        var resultLimit2 = jsonLimit2.RootElement.GetProperty("result");
        var resultLimit3 = jsonLimit3.RootElement.GetProperty("result");

        var countLimit2 = resultLimit2.GetArrayLength();
        var countLimit3 = resultLimit3.GetArrayLength();

        Assert.Equal(countLimit2 + 1, countLimit3);
    }
}
