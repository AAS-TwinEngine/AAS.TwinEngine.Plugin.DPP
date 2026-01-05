using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

[ExcludeFromCodeCoverage]
public class MappingDataInitializer(IHostEnvironment env, ILogger<MappingDataInitializer> logger)
{
    public void Initialize()
    {
        var dataPath = Path.Combine(env.ContentRootPath, "Data");

        MappingData.MappingJson = LoadData(Path.Combine(dataPath, "mapping.json"));
    }

    private JsonDocument LoadData(string filePath)
    {
        if (!File.Exists(filePath))
        {
            logger.LogCritical("Data file not found at {FilePath}", filePath);
            throw new ResourceNotFoundException();
        }

        try
        {
            var jsonContent = File.ReadAllText(filePath);
            return JsonDocument.Parse(jsonContent);
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "Invalid JSON in file {FilePath}", filePath);
            throw new ResourceNotValidException();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading data file {FilePath}", filePath);
            throw new ResourceNotValidException();
        }
    }
}
