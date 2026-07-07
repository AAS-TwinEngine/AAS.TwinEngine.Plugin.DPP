using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISemanticIdToColumnMapper
{
    Dictionary<string, ColumnMapping> GetSemanticIdToColumnMapping(SemanticTreeNode requestNode);
}
