namespace AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

// AlternateLeafColumns covers semanticIds reused across multiple tables (e.g. shared IDTA
// properties on MaintenanceTool/Consumable/SparePart) that would otherwise resolve to the
// wrong column when the mapping is looked up outside of its originating table context.
public record ColumnMapping(string BranchColumn, string LeafColumn, IReadOnlyList<string>? AlternateLeafColumns = null);
