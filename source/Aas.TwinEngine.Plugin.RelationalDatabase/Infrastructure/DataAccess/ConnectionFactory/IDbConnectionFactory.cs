using System.Data.Common;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
