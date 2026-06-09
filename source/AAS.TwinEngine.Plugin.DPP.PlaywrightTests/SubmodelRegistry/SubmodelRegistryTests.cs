using System.Text.Json;
using System.Threading.Tasks;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.SubmodelRegistry;

public class SubmodelRegistryTests : ApiTestBase
{
    [Fact]
    public async Task GetSubmodelDescriptorById_Contact_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierContact}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_Contact_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_HandoverDocumentation_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierHandoverDocumentation}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_HandoverDocumentation_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_Nameplate_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierNameplate}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_Nameplate_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_CarbonFootprint_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierCarbonFootprint}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_CarbonFootprint_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_TechnicalData_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierTechnicalData}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_TechnicalData_Expected.json"));
    }

}

