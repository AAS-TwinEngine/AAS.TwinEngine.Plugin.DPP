using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface ISubmodelMetadataExtractor
{
    SubmodelIdExtractionResult ExtractSubmodelMetadata(string submodelId);
}
