using BLL.Engines.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DAL
{
    public class SqlConnectionEngine : ISqlConnectionInterface
    {
        private readonly string _connectionString;

        public SqlConnectionEngine(IConfiguration configuration)
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
