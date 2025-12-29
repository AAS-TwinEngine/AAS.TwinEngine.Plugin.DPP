namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;

public class RequestTimeoutException : Exception
{
    public RequestTimeoutException(string message) : base(message)
    {
    }

    public RequestTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public RequestTimeoutException()
    {
    }
}
