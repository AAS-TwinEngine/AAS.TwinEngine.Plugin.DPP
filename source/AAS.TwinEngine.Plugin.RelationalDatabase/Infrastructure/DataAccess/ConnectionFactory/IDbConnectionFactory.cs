using System.Data.Common;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.ConnectionFactory;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
