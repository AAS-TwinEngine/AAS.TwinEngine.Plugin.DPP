using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISemanticIdToColumnMapper
{
    Dictionary<string, string> GetSemanticIdToColumnMapping(SemanticTreeNode requestNode);
}
