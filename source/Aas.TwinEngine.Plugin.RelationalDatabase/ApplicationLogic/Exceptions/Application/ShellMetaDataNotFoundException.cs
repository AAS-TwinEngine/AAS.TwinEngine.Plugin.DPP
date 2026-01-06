using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class ShellMetaDataNotFoundException : NotFoundException
{
    public const string ServiceName = "Shell MetaData";

    public ShellMetaDataNotFoundException() : base(ServiceName) { }

    public ShellMetaDataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public ShellMetaDataNotFoundException(string message) : base(message)
    {
    }

    public ShellMetaDataNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
