namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

public class SubmodelIdExtractionResult(string productId, SubmodelName submodelName)
{
    public string ProductId { get; } = productId;
    public SubmodelName SubmodelName { get; } = submodelName;
}
