using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRegistry;

namespace AAS.TwinEngine.Plugin.DPP.PlaywrightTests.AasRepository;

public class AasRepositoryTests : ApiTestBase
{
    [Fact]
    public async Task GetShellById_Product1_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier1}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        // Verify it's valid JSON
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetShellById_Product1_Expected.json"));
    }

    [Fact]
    public async Task GetAssetInformationById_Product1_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier1}/asset-information";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAssetInformationById_Product1_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelRefById_Product1_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier1}/submodel-refs";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetSubmodelRefById_Product1_Expected.json"));
    }

    [Fact]
    public async Task GetShellById_Product2_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier2}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetShellById_Product2_Expected.json"));
    }

    [Fact]
    public async Task GetAssetInformationById_Product2_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier2}/asset-information";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAssetInformationById_Product2_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelRefById_Product2_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier2}/submodel-refs";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetSubmodelRefById_Product2_Expected.json"));
    }

    [Fact]
    public async Task GetShellById_Product3_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier3}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetShellById_Product3_Expected.json"));
    }

    [Fact]
    public async Task GetAssetInformationById_Product3_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier3}/asset-information";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAssetInformationById_Product3_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelRefById_Product3_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier3}/submodel-refs";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetSubmodelRefById_Product3_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByAssetId_And_ByIdShort()
    {
        // Arrange
        var assetId = EncodeBase64Url("{\"name\":\"SerialNumber\",\"value\":\"SN-FMABC1234-9804820\"}");
        var idShort = "Product1";
        var url = $"/shells?assetIds={assetId}&idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByAssetId_And_ByIdShort_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByAssetIds()
    {
        // Arrange
        var assetId = EncodeBase64Url("{\"name\":\"SerialNumber\",\"value\":\"SN-FMABC1234-9804820\"}");
        var url = $"/shells?assetIds={assetId}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByAssetIds_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByIdShort()
    {
        // Arrange
        var idShort = "Product2";
        var url = $"/shells?idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByIdShort_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByMultipleAssetIds_ShouldApplyAndFilter()
    {
        // Arrange
        var serialFilter = EncodeBase64Url("{\"name\":\"SerialNumber\",\"value\":\"SN-FMABC1234-9804820\"}");
        var batchFilter = EncodeBase64Url("{\"name\":\"BatchId\",\"value\":\"BATCH-2022-001\"}");
        var url = $"/shells?assetIds={serialFilter}&assetIds={batchFilter}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        foreach (var shell in result.EnumerateArray())
        {
            AssertShellContainsSpecificAssetId(shell, "SerialNumber", "SN-FMABC1234-9804820");
            AssertShellContainsSpecificAssetId(shell, "BatchId", "BATCH-2022-001");
        }

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByMultipleAssetIds_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_WithCursor_ShouldReturnNextPage()
    {
        // Arrange
        const string firstPageUrl = "/shells?limit=1";

        // Act
        var firstPageResponse = await ApiContext.GetAsync(firstPageUrl);

        // Assert first page
        AssertSuccessResponse(firstPageResponse);

        var firstRoot = await ParseResponseRootAsync(firstPageResponse);
        var firstResult = firstRoot.GetProperty("result");
        Assert.Equal(1, firstResult.GetArrayLength());

        var cursor = firstRoot.GetProperty("paging_metadata").GetProperty("cursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(cursor));

        var firstPageId = firstResult[0].GetProperty("id").GetString();

        var secondPageResponse = await ApiContext.GetAsync($"/shells?limit=1&cursor={cursor}");
        AssertSuccessResponse(secondPageResponse);

        var secondRoot = await ParseResponseRootAsync(secondPageResponse);
        var secondResult = secondRoot.GetProperty("result");
        Assert.Equal(1, secondResult.GetArrayLength());

        var secondPageId = secondResult[0].GetProperty("id").GetString();
        Assert.NotEqual(firstPageId, secondPageId);
        var json = JsonDocument.Parse(secondRoot.GetRawText());
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_WithCursor_Expected.json"));
    }
    [Fact]
    public async Task GetAllShells_WithInvalidCursorEncoding_ShouldReturnBadRequest()
    {
        // Arrange
        const string url = "/shells?cursor=https://mm-software.com/ids/assets/000-001";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task GetAllShells_WithInvalidLimit_ShouldReturnBadRequest()
    {
        // Arrange
        const string url = "/shells?limit=0";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task GetAllShells_WithLimit_ShouldReturnExpectedPageSize()
    {
        // Arrange
        const string url = "/shells?limit=1";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_WithLimit_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_WithUnknownCursorValue_ShouldReturnBadRequest()
    {
        // Arrange
        var unknownCursor = "https://mm-software.com/ids/aas/000-004";

        // Act
        var unknownCursorResponse = await ApiContext.GetAsync($"/shells?limit=1&cursor={unknownCursor}");

        // Assert
        Assert.Equal(400, unknownCursorResponse.Status);
    }

    private static async Task<JsonElement> ParseResponseRootAsync(Microsoft.Playwright.IAPIResponse response)
    {
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        return json.RootElement.Clone();
    }

    private static void AssertShellContainsSpecificAssetId(JsonElement shell, string expectedName, string expectedValue)
    {
        Assert.True(shell.TryGetProperty("assetInformation", out var assetInformation));
        Assert.True(assetInformation.TryGetProperty("specificAssetIds", out var specificAssetIds));
        Assert.Equal(JsonValueKind.Array, specificAssetIds.ValueKind);

        var hasExpectedAssetId = specificAssetIds.EnumerateArray().Any(assetId =>
            assetId.TryGetProperty("name", out var name) &&
            assetId.TryGetProperty("value", out var value) &&
            string.Equals(name.GetString(), expectedName, StringComparison.Ordinal) &&
            string.Equals(value.GetString(), expectedValue, StringComparison.Ordinal));

        Assert.True(hasExpectedAssetId, $"Expected shell to contain specificAssetId '{{name: {expectedName}, value: {expectedValue}}}'.");
    }

    private static string EncodeBase64Url(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

