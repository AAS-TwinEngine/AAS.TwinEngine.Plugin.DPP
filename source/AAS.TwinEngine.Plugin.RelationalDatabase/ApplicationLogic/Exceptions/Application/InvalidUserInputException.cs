using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class InvalidUserInputException : BadRequestException
{
    public const string DefaultMessage = "Invalid User Input.";

    public InvalidUserInputException() : base(DefaultMessage) { }

    public InvalidUserInputException(Exception ex) : base(DefaultMessage, ex) { }

    public InvalidUserInputException(string message) : base(DefaultMessage)
    {
    }
}
