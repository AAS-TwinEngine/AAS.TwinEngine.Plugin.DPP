namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;

public class ServiceAuthorizationException : Exception
{
    public ServiceAuthorizationException(string message) : base(message)
    {
    }

    public ServiceAuthorizationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ServiceAuthorizationException()
    {
    }
}
