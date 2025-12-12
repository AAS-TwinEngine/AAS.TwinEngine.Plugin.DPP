using Json.Schema;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public interface IJsonSchemaValidator
{
    void ValidateResponseContent(string responseJson, JsonSchema requestSchema);

    void ValidateRequestSchema(JsonSchema schema);
}
