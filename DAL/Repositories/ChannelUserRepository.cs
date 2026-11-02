using BLL.Engines.Interfaces;
using BLL.Models;
using BLL.Models.Enums;
using BLL.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace DAL.Repositories
{
    public class ChannelUserRepository : IChannelUserRepositoryInterface
    {
        private readonly ISqlConnectionInterface _SqlConnection;

        public ChannelUserRepository(ISqlConnectionInterface sqlConnection)
        {
            _SqlConnection = sqlConnection;
        }

        public async Task<bool> Delete(int ChannelUserId)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "DELETE FROM ChannelUsers WHERE Id = @ChannelUserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChannelUserId", ChannelUserId);

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public Task<List<ChannelUser?>> FindByChannel(int ChannelId)
        {
            const string query = "SELECT Id, UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt FROM ChannelUsers WHERE ChannelId = @ChannelId";
            return FindChannelUsers(query, "@ChannelId", ChannelId);
        }

        public async Task<ChannelUser?> FindById(int ChannelUserId)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "SELECT Id, UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt FROM ChannelUsers WHERE Id = @ChannelUserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChannelUserId", ChannelUserId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapChannelUserFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public Task<List<ChannelUser?>> FindByRole(ChannelRole Role)
        {
            const string query = "SELECT Id, UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt FROM ChannelUsers WHERE ChannelRole = @ChannelRole";
            return FindChannelUsers(query, "@ChannelRole", (int)Role);
        }

        public Task<List<ChannelUser?>> FindByUser(int UserId)
        {
            const string query = "SELECT Id, UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt FROM ChannelUsers WHERE UserId = @UserId";
            return FindChannelUsers(query, "@UserId", UserId);
        }

        public async Task<ChannelUser?> Store(ChannelUser channelUser)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"INSERT INTO ChannelUsers
                    (UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.ChannelId, INSERTED.ChannelRole, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    VALUES (@UserId, @ChannelId, @ChannelRole, GETDATE(), GETDATE())";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", channelUser.UserId);
                    command.Parameters.AddWithValue("@ChannelId", channelUser.ChannelId);
                    command.Parameters.AddWithValue("@ChannelRole", (int)channelUser.ChannelRole);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapChannelUserFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public async Task<ChannelUser?> Update(ChannelUser channelUser)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"UPDATE ChannelUsers
                    SET UserId = @UserId, ChannelId = @ChannelId, ChannelRole = @ChannelRole, UpdatedAt = GETDATE()
                    OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.ChannelId, INSERTED.ChannelRole, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    WHERE Id = @ChannelUserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChannelUserId", channelUser.Id);
                    command.Parameters.AddWithValue("@UserId", channelUser.UserId);
                    command.Parameters.AddWithValue("@ChannelId", channelUser.ChannelId);
                    command.Parameters.AddWithValue("@ChannelRole", (int)channelUser.ChannelRole);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapChannelUserFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        private async Task<List<ChannelUser?>> FindChannelUsers(string query, string parameterName, object parameterValue)
        {
            var channelUsers = new List<ChannelUser?>();

            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(parameterName, parameterValue);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            channelUsers.Add(MapChannelUserFromReader(reader));
                        }
                    }
                }
            }

            return channelUsers;
        }

        private ChannelUser MapChannelUserFromReader(SqlDataReader reader)
        {
            return new ChannelUser(
                id: (int)reader["Id"],
                userId: (int)reader["UserId"],
                channelId: (int)reader["ChannelId"],
                channelRole: (ChannelRole)(int)reader["ChannelRole"],
                createdAt: (DateTime)reader["CreatedAt"],
                updatedAt: (DateTime)reader["UpdatedAt"]
            );
        }
    }
}
