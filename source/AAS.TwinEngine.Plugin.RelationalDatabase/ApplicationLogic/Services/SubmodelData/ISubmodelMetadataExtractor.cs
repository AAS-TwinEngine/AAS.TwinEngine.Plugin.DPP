using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISubmodelMetadataExtractor
{
    SubmodelIdExtractionResult ExtractSubmodelMetadata(string submodelId);
}
