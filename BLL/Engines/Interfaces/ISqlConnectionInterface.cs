using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace BLL.Engines.Interfaces
{
    public interface ISqlConnectionInterface
    {
        public SqlConnection GetConnection();
        public Task<SqlConnection> GetConnectionAsync();
    }
}
