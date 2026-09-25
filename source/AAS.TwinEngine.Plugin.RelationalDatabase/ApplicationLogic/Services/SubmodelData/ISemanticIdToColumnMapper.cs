using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISemanticIdToColumnMapper
{
    // A semanticId can map to more than one column when it's reused across tables
    // (e.g. shared IDTA properties on different SubmodelElementCollections).
    Dictionary<string, List<ColumnMapping>> GetSemanticIdToColumnMapping(SemanticTreeNode requestNode);
}
