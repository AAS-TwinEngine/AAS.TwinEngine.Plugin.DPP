using System.Text.Json.Nodes;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public interface IJsonSchemaSecurityValidator
{
    void ValidateSchemaComplexity(JsonNode rootNode);

    void ValidateSchemaContent(JsonNode rootNode);
}
