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
}
