namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Requests;

public record GetShellDescriptorsRequest(int? Limit, string? Cursor, string? AssetIdsFilter = null, string? IdShortFilter = null);
