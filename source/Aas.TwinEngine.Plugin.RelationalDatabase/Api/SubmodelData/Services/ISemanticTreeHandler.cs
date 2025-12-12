using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public interface ISemanticTreeHandler
{
    JsonObject GetJson(SemanticTreeNode semanticTreeNodeWithValues, JsonSchema dataQuery);
}
