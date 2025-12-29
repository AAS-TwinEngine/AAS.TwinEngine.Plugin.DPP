using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class DataNotFoundException : NotFoundException
{
    public const string ServiceName = "Data";

    public DataNotFoundException() { }

    public DataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public DataNotFoundException(string message) : base(message)
    {
    }

    public DataNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
