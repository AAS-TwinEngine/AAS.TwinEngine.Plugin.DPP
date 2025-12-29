using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class MetaDataNotFoundException : NotFoundException
{
    public const string ServiceName = "MetaData";

    public MetaDataNotFoundException() { }

    public MetaDataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public MetaDataNotFoundException(string message) : base(message)
    {
    }

    public MetaDataNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
