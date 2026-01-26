namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

public class InternalServerException : Exception
{
    public InternalServerException(string message)
        : base(message)
    {
    }

    public InternalServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InternalServerException()
    {
    }
}
