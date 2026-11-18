using System.Text.Json;

using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRegistry;

using Microsoft.Playwright;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for appropriate serialization endpoints
/// </summary>
public class SerializationTests : ApiTestBase
{
    private const string AasxMediaType = "application/asset-administration-shell-package+xml";
    private const string HandoverDocumentationId = "https://mm-software.com/submodel/000-001/HandoverDocumentation";

    private string SerializationUrl =>
        $"/serialization" +
        $"?aasIds={AasIdentifier1}" +
        $"&submodelIds={SubmodelIdentifierMaintenanceInstructions}" +
        $"&submodelIds={SubmodelIdentifierHandoverDocumentation}" +
        $"&submodelIds={SubmodelIdentifierNameplate}" +
        $"&submodelIds={SubmodelIdentifierCarbonFootprint}" +
        $"&submodelIds={SubmodelIdentifierTechnicalData}" +
        $"&includeConceptDescriptions=false";

    [Fact]
    public async Task GetAppropriateSerialization_WithMultipleSubmodels_ShouldReturnSuccess()
    {
        // Act
        var response = await ApiContext.GetAsync(SerializationUrl);
        var content = await response.TextAsync();

        Assert.False(string.IsNullOrEmpty(content));

        Assert.Contains(HandoverDocumentationId, content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/idta/SubmodelTemplate/MaintenanceInstructions/1/0", content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/idta/CarbonFootprint/ProductCarbonFootprint/1/0", content, StringComparison.Ordinal);
        Assert.Contains("0173-1#02-ABK161#002/0173-1#01-AHX838#002", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAppropriateSerialization_WithAasxAcceptHeader_ShouldReturnAasxPackage()
    {
        // Act
        var response = await GetSerializationAsync(AasxMediaType);
        var content = await response.BodyAsync();

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal(AasxMediaType, GetContentType(response));
        Assert.True(content.Length > 2 && content[0] == 'P' && content[1] == 'K', "AASX package must be a ZIP archive.");
    }

    [Fact]
    public async Task GetAppropriateSerialization_WithoutSpecificPreference_ShouldReturnAasxPackage()
    {
        // Act
        var response = await GetSerializationAsync("*/*");

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal(AasxMediaType, GetContentType(response));
    }

    [Fact]
    public async Task GetAppropriateSerialization_WithJsonAcceptHeader_ShouldReturnJsonEnvironment()
    {
        // Act
        var response = await GetSerializationAsync("application/json");
        var content = await response.TextAsync();

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal("application/json", GetContentType(response));

        using var environment = JsonDocument.Parse(content);
        Assert.True(environment.RootElement.TryGetProperty("assetAdministrationShells", out _));
        Assert.True(environment.RootElement.TryGetProperty("submodels", out _));

        Assert.Contains(HandoverDocumentationId, content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAppropriateSerialization_WithXmlAcceptHeader_ShouldReturnXmlEnvironment()
    {
        // Act
        var response = await GetSerializationAsync("application/xml");
        var content = await response.TextAsync();

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal("application/xml", GetContentType(response));
        Assert.StartsWith("<?xml", content, StringComparison.Ordinal);
        Assert.Contains("environment", content, StringComparison.Ordinal);
        Assert.Contains(HandoverDocumentationId, content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("application/xml, application/json", "application/xml")]
    [InlineData("text/csv, application/json", "application/json")]
    [InlineData("application/xml;q=0.2, application/json;q=0.9", "application/xml")]
    public async Task GetAppropriateSerialization_WithMultipleAcceptValues_ShouldReturnFirstSupportedInHeaderOrder(string acceptHeader, string expectedContentType)
    {
        // Act
        var response = await GetSerializationAsync(acceptHeader);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal(expectedContentType, GetContentType(response));
    }

    [Theory]
    [InlineData("text/csv")]
    [InlineData("application/pdf")]
    public async Task GetAppropriateSerialization_WithUnsupportedAcceptHeader_ShouldReturnBadRequest(string acceptHeader)
    {
        // Act
        var response = await GetSerializationAsync(acceptHeader);

        // Assert
        Assert.Equal(400, response.Status);
    }

    private Task<IAPIResponse> GetSerializationAsync(string acceptHeader)
        => ApiContext.GetAsync(SerializationUrl,
                               new APIRequestContextOptions
                               {
                                   Headers = new Dictionary<string, string> { { "Accept", acceptHeader } }
                               });

    private static string? GetContentType(IAPIResponse response)
        => response.Headers.TryGetValue("content-type", out var contentType)
               ? contentType.Split(';')[0].Trim()
               : null;
}