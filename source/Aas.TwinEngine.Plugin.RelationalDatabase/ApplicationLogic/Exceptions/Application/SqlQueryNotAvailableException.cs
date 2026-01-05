using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class SqlQueryNotAvailableException : InternalServerException
{
    public const string DefaultMessage = "Internal Server Error.";

    public SqlQueryNotAvailableException() : base(DefaultMessage) { }

    public SqlQueryNotAvailableException(Exception ex) : base(DefaultMessage, ex) { }

    public SqlQueryNotAvailableException(string message) : base(message)
    {
    }

    public SqlQueryNotAvailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
