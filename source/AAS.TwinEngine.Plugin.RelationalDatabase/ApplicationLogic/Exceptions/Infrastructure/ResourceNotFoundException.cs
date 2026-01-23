namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message)
    {
    }

    public ResourceNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ResourceNotFoundException()
    {
    }
}
