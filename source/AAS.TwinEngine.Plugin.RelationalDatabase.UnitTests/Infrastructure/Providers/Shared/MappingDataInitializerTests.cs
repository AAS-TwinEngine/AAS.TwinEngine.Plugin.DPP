using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.Shared;

public class MappingDataInitializerTests
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<MappingDataInitializer> _logger;

    public MappingDataInitializerTests()
    {
        _env = Substitute.For<IHostEnvironment>();
        _logger = Substitute.For<ILogger<MappingDataInitializer>>();
        _env.ContentRootPath.Returns(Directory.GetCurrentDirectory());
    }

    [Fact]
    public void Initialize_WhenFileMissing_ThrowsResourceNotFoundException()
    {
        var testFolder = Path.Combine(_env.ContentRootPath, "Data");
        Directory.CreateDirectory(testFolder);

        var filePath = Path.Combine(testFolder, "mapping.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var sut = new MappingDataInitializer(_env, _logger);

        Assert.Throws<ResourceNotFoundException>(sut.Initialize);
    }

    [Fact]
    public void Initialize_WhenJsonInvalid_ThrowsResourceNotValidException()
    {
        var testFolder = Path.Combine(_env.ContentRootPath, "Data");
        Directory.CreateDirectory(testFolder);

        var filePath = Path.Combine(testFolder, "mapping.json");
        File.WriteAllText(filePath, "{ invalid json");

        var sut = new MappingDataInitializer(_env, _logger);

        Assert.Throws<ResourceNotValidException>(sut.Initialize);
    }

    [Fact]
    public void Initialize_WhenJsonValid_SetsMappingData()
    {
        var testFolder = Path.Combine(_env.ContentRootPath, "Data");
        Directory.CreateDirectory(testFolder);

        var filePath = Path.Combine(testFolder, "mapping.json");
        var json = "{\"name\": \"test\"}";
        File.WriteAllText(filePath, json);

        var sut = new MappingDataInitializer(_env, _logger);

        sut.Initialize();
        Assert.Equal("test", MappingData.MappingJson.GetProperty("name").GetString());
    }
}
