using System.Text;
using System.Text.Json;

using Microsoft.Playwright;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests;

/// <summary>
/// Base class for API tests providing common functionality and configuration
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

    protected IAPIRequestContext ApiContext { get; private set; } = null!;
    protected string BaseUrl { get; private set; } = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:8080";

    // Base64 encoded identifiers
    protected string AasIdentifier { get; private set; } = null!;
    protected string AasIdentifier2 { get; private set; } = null!;
    protected string AasIdentifier3 { get; private set; } = null!;
    protected string SubmodelIdentifierContact { get; private set; } = null!;
    protected string SubmodelIdentifierHandoverDocumentation { get; private set; } = null!;
    protected string SubmodelIdentifierNameplate { get; private set; } = null!;
    protected string SubmodelIdentifierCarbonFootprint { get; private set; } = null!;
    protected string SubmodelIdentifierTechnicalData { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Initialize Playwright
        var playwright = await Playwright.CreateAsync();

        // Create API request context
        ApiContext = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" }
            }
        });

        // Initialize base64 encoded identifiers
        AasIdentifier = Base64EncodeUrl("https://mm-software.com/ids/aas/000-001");
        AasIdentifier2 = Base64EncodeUrl("https://mm-software.com/ids/aas/000-002");
        AasIdentifier3 = Base64EncodeUrl("https://mm-software.com/ids/aas/001-001");
        SubmodelIdentifierContact = Base64EncodeUrl("https://mm-software.com/submodel/000-001/ContactInformation");
        SubmodelIdentifierHandoverDocumentation = Base64EncodeUrl("https://mm-software.com/submodel/000-001/HandoverDocumentation");
        SubmodelIdentifierNameplate = Base64EncodeUrl("https://mm-software.com/submodel/000-001/Nameplate");
        SubmodelIdentifierCarbonFootprint = Base64EncodeUrl("https://mm-software.com/submodel/000-001/CarbonFootprint");
        SubmodelIdentifierTechnicalData = Base64EncodeUrl("https://mm-software.com/submodel/000-001/TechnicalData");

    }

    public async Task DisposeAsync() => await ApiContext.DisposeAsync();

    /// <summary>
    /// Base64 URL encodes a string
    /// </summary>
    public static string Base64EncodeUrl(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Asserts that an API response is successful
    /// </summary>
    protected static void AssertSuccessResponse(IAPIResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        Assert.True(response.Ok, $"Expected successful response but got {response.Status}: {response.StatusText}");
    }

    /// <summary>
    /// Asserts that an JsonDocument is equals to the expected JSON content from a file
    /// </summary>
    protected static async Task CompareJsonAsync(JsonDocument actualDoc, string fullPath)
    {
        // Load expected test data and compare
        var expectedJson = await File.ReadAllTextAsync(fullPath);

        var expectedDoc = JsonDocument.Parse(expectedJson);
        Assert.NotNull(expectedDoc);

        // Compare JSON content (normalize formatting for comparison)
        var expectedNormalized = JsonSerializer.Serialize(expectedDoc, JsonSerializerOptions);
        var actualNormalized = JsonSerializer.Serialize(actualDoc, JsonSerializerOptions);
        Assert.Equal(expectedNormalized, actualNormalized);
    }
}
