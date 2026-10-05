using ChatNest.Service.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace ChatNest.Service
{
    public class SqlConnectionService : ISqlConnectionInterface
    {
        private readonly string _connectionString;

        public SqlConnectionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? configuration.GetConnectionString("PAWS_DB_Connection")
                ?? throw new InvalidOperationException("No valid connection string found in configuration.");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}

