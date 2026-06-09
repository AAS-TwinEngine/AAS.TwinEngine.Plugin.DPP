using System.Text.Json;

using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRegistry;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for Submodel endpoints
/// </summary>
public class SubmodelTests : ApiTestBase
{
    [Fact]
    public async Task GetSubmodel_ContactInfo_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierContact}/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_ContactInfo_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodel_HandoverDocumentation_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierHandoverDocumentation}/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_HandoverDocumentation_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodel_CarbonFootprint_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierCarbonFootprint}/";
        // Act
        var response = await ApiContext.GetAsync(url);
        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_CarbonFootprint_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodel_TechnicalData_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierTechnicalData}/";
        // Act
        var response = await ApiContext.GetAsync(url);
        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_TechnicalData_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodel_Nameplate_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierNameplate}/";
        // Act
        var response = await ApiContext.GetAsync(url);
        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_Nameplate_Expected.json"));
    }
}
