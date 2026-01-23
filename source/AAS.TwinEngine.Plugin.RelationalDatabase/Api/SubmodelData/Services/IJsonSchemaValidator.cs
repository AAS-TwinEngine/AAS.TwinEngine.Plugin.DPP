using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public interface IJsonSchemaValidator
{
    void ValidateResponseContent(string responseJson, JsonSchema requestSchema);

    void ValidateRequestSchema(JsonSchema schema);
}
