using System.Data;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
