using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class ValidationFailedException : InternalServerException
{
    public const string DefaultMessage = "Internal Server Error.";

    public ValidationFailedException() : base(DefaultMessage) { }

    public ValidationFailedException(Exception ex) : base(DefaultMessage, ex) { }
}
