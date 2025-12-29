using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

public class SqlQueryNotFoundException : NotFoundException
{
    public const string DefaultMessage = "Internal Server Error.";

    public SqlQueryNotFoundException() : base(DefaultMessage) { }

    public SqlQueryNotFoundException(Exception ex) : base(DefaultMessage, ex) { }

    public SqlQueryNotFoundException(string message) : base(message)
    {
    }

    public SqlQueryNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
