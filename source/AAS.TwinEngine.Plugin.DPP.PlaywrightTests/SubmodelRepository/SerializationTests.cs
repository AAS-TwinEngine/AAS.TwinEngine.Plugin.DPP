using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRegistry;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for appropriate serialization endpoints
/// </summary>
public class SerializationTests : ApiTestBase
{
    [Fact]
    public async Task GetAppropriateSerialization_WithMultipleSubmodels_ShouldReturnSuccess()
    {
        // Arrange
        var url = $"/serialization" +
                  $"?aasIds={AasIdentifier1}" +
                  $"&submodelIds={SubmodelIdentifierMaintenanceInstructions}" +
                  $"&submodelIds={SubmodelIdentifierHandoverDocumentation}" +
                  $"&submodelIds={SubmodelIdentifierNameplate}" +
                  $"&submodelIds={SubmodelIdentifierCarbonFootprint}" +
                  $"&submodelIds={SubmodelIdentifierTechnicalData}" +
                  $"&includeConceptDescriptions=false";

        // Act
        var response = await ApiContext.GetAsync(url);
        var content = await response.TextAsync();

        Assert.False(string.IsNullOrEmpty(content));

        Assert.Contains("https://mm-software.com/submodel/000-001/HandoverDocumentation", content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/idta/SubmodelTemplate/MaintenanceInstructions/1/0", content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/idta/CarbonFootprint/ProductCarbonFootprint/1/0", content, StringComparison.Ordinal);
        Assert.Contains("0173-1#02-ABK161#002/0173-1#01-AHX838#002", content, StringComparison.Ordinal);
    }
}
