using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public interface IJsonSchemaParser
{
    SemanticTreeNode ParseJsonSchema(JsonSchema jsonSchema);
}
