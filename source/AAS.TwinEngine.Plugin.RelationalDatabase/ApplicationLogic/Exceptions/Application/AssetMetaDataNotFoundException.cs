using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class AssetMetaDataNotFoundException : NotFoundException
{
    public const string ServiceName = "Asset MetaData";

    public AssetMetaDataNotFoundException() : base(ServiceName) { }

    public AssetMetaDataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public AssetMetaDataNotFoundException(string message) : base(ServiceName)
    {
    }
}
