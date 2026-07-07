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
}

