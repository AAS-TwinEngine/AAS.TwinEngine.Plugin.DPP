namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

public class SubmodelIdExtractionResult
{
    public string ProductId { get; }
    public SubmodelName SubmodelName { get; }

    public SubmodelIdExtractionResult(string productId, SubmodelName submodelName)
    {
        ProductId = productId;
        SubmodelName = submodelName;
    }
}
