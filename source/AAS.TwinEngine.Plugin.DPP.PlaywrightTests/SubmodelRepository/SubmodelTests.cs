using System.Text.Json;

using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRegistry;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for Submodel endpoints
/// </summary>
public class SubmodelTests : ApiTestBase
{
    [Fact]
    public async Task GetAllSubmodels_WithoutParameters_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetAllSubmodels_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodels_By_SemanticId_should_return_success_and_content_as_expected()
    {
        // Arrange
        var semanticId = EncodeBase64Url("https://admin-shell.io/idta/CarbonFootprint/CarbonFootprint/1/0");
        
        var url = $"/submodels?semanticId={semanticId}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetAllSubmodels_By_SemanticId_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodels_ByIdShort()
    {
        // Arrange
        var idShort = "Product2";
        var url = $"/submodels?idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetAllSubmodels_ByIdShort_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodels_ByIdShort_WithUnknownIdShort()
    {
        // Arrange
        var idShort = "UnknownProduct";
        var url = $"/submodels?idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        var result = json.RootElement.GetProperty("result");
        Assert.Equal(0, result.GetArrayLength());
    }

    [Fact]
    public async Task GetSubmodel_MaintenanceInstructions_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierMaintenanceInstructions}/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_MaintenanceInstructions_Expected.json"));
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

    private static string EncodeBase64Url(string plainText)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
