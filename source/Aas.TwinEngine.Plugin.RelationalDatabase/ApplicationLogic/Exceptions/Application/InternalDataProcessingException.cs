using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class InternalDataProcessingException : InternalServerException
{
    public const string DefaultMessage = "Internal Server Error.";

    public InternalDataProcessingException() : base(DefaultMessage) { }

    public InternalDataProcessingException(Exception ex) : base(DefaultMessage, ex) { }
}
