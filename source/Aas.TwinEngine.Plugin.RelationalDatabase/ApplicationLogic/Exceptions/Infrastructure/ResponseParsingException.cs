namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;

public class ResponseParsingException : Exception
{
    public ResponseParsingException(string message) : base(message)
    {
    }

    public ResponseParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ResponseParsingException()
    {
    }
}
