using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class SubmodelDataNotFoundException : NotFoundException
{
    public const string ServiceName = "Submodel Data";

    public SubmodelDataNotFoundException() : base(ServiceName) { }

    public SubmodelDataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public SubmodelDataNotFoundException(string message) : base(message)
    {
    }

    public SubmodelDataNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
