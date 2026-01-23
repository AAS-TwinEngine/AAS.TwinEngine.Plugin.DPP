namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;

public class ValidationFailedException : Exception
{
    public ValidationFailedException(string message) : base(message)
    {
    }

    public ValidationFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ValidationFailedException()
    {
    }
}
