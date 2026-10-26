using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISemanticIdToColumnMapper
{
    Dictionary<string, List<ColumnMapping>> GetSemanticIdToColumnMapping(SemanticTreeNode requestNode);
}
