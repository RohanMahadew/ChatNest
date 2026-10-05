using Microsoft.Data.SqlClient;

namespace ChatNest.Service.Interface
{
    public interface ISqlConnectionInterface
    {
        public SqlConnection GetConnection();
        public Task<SqlConnection> GetConnectionAsync();
    }
}
