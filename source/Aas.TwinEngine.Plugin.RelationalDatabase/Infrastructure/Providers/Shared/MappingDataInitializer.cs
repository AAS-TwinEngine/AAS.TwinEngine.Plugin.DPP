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
            logger.LogCritical("data file not found at {FilePath}", filePath);
            throw new ResourceNotFoundException();
        }

        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var streamReader = new StreamReader(fileStream);
            var jsonContent = streamReader.ReadToEnd();
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
