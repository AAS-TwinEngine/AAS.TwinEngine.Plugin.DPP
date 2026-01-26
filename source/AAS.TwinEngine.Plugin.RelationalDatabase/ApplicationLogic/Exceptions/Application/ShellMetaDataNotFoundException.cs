using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class ShellMetaDataNotFoundException : NotFoundException
{
    public const string ServiceName = "Shell MetaData";

    public ShellMetaDataNotFoundException() : base(ServiceName) { }

    public ShellMetaDataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public ShellMetaDataNotFoundException(string message) : base(ServiceName)
    {
    }
}
