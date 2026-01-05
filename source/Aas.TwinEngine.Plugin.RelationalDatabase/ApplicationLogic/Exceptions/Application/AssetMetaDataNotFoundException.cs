using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class AssetMetaDataNotFoundException : NotFoundException
{
    public const string ServiceName = "Asset MetaData";

    public AssetMetaDataNotFoundException() : base(ServiceName) { }

    public AssetMetaDataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public AssetMetaDataNotFoundException(string message) : base(message)
    {
    }

    public AssetMetaDataNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
