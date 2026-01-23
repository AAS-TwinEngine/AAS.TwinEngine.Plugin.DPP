using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class SubmodelDataNotFoundException : NotFoundException
{
    public const string ServiceName = "Submodel Data";

    public SubmodelDataNotFoundException() : base(ServiceName) { }

    public SubmodelDataNotFoundException(Exception ex) : base(ServiceName, ex) { }

    public SubmodelDataNotFoundException(string message) : base(ServiceName)
    {
    }
}
