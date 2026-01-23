namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;

public class ResourceNotValidException : Exception
{
    public ResourceNotValidException(string message) : base(message)
    {
    }

    public ResourceNotValidException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ResourceNotValidException()
    {
    }
}
