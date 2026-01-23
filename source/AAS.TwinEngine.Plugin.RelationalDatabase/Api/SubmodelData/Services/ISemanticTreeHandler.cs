using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public interface ISemanticTreeHandler
{
    JsonObject GetJson(SemanticTreeNode semanticTreeNodeWithValues, JsonSchema dataQuery);
}
