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
                  $"&submodelIds={SubmodelIdentifierContact}" +
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
        Assert.Contains("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation", content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/AddressInformation", content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/idta/CarbonFootprint/ProductCarbonFootprint/1/0", content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/ZVEI/TechnicalData/GeneralInformation/1/1", content, StringComparison.Ordinal);
    }
}
