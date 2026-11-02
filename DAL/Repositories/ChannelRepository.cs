using BLL.Engines.Interfaces;
using BLL.Models;
using BLL.Models.Enums;
using BLL.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace DAL.Repositories
{
    public class ChannelRepository : IChannelRepositoryInterface
    {
        private readonly ISqlConnectionInterface _SqlConnection;

        public ChannelRepository(ISqlConnectionInterface sqlConnection)
        {
            _SqlConnection = sqlConnection;
        }

        public async Task<bool> Delete(int Channelid)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "DELETE FROM Channels WHERE Id = @ChannelId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChannelId", Channelid);

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public Task<List<Channel?>> FindByCreator(int Channelid)
        {
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.CreatedAt, c.UpdatedAt
                FROM Channels c
                WHERE EXISTS (
                    SELECT 1 FROM ChannelUsers cu
                    WHERE cu.ChannelId = c.Id AND cu.UserId = @UserId AND cu.ChannelRole = @ChannelRole)";
            return FindChannels(query, Channelid, (int)ChannelRole.SuperAdmin);
        }

        public async Task<Channel?> FindById(int Channelid)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "SELECT Id, Uuid, Type, CreatedAt, UpdatedAt FROM Channels WHERE Id = @ChannelId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChannelId", Channelid);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapChannelFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public Task<List<Channel?>> FindByParticipant(int Channelid)
        {
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.CreatedAt, c.UpdatedAt
                FROM Channels c
                WHERE EXISTS (
                    SELECT 1 FROM ChannelUsers cu
                    WHERE cu.ChannelId = c.Id AND cu.UserId = @UserId)";
            return FindChannels(query, Channelid);
        }

        public Task<List<Channel?>> FindByUser(int Channelid)
        {
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.CreatedAt, c.UpdatedAt
                FROM Channels c
                WHERE EXISTS (
                    SELECT 1 FROM ChannelUsers cu
                    WHERE cu.ChannelId = c.Id AND cu.UserId = @UserId)";
            return FindChannels(query, Channelid);
        }

        public async Task<Channel?> Store(Channel channel)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"INSERT INTO Channels
                    (Uuid, Type, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id, INSERTED.Uuid, INSERTED.Type, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    VALUES (@Uuid, @Type, GETDATE(), GETDATE())";

                using (var command = new SqlCommand(query, connection))
                {
                    var uuid = string.IsNullOrWhiteSpace(channel.Uuid) ? Guid.NewGuid().ToString() : channel.Uuid;
                    command.Parameters.AddWithValue("@Uuid", uuid);
                    command.Parameters.AddWithValue("@Type", (int)channel.Type);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapChannelFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public async Task<Channel?> Update(Channel channel)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"UPDATE Channels
                    SET Uuid = @Uuid, Type = @Type, UpdatedAt = GETDATE()
                    OUTPUT INSERTED.Id, INSERTED.Uuid, INSERTED.Type, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    WHERE Id = @ChannelId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChannelId", channel.Id);
                    command.Parameters.AddWithValue("@Uuid", channel.Uuid);
                    command.Parameters.AddWithValue("@Type", (int)channel.Type);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapChannelFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        private async Task<List<Channel?>> FindChannels(string query, int userId, int? channelRole = null)
        {
            var channels = new List<Channel?>();

            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    if (channelRole.HasValue)
                    {
                        command.Parameters.AddWithValue("@ChannelRole", channelRole.Value);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            channels.Add(MapChannelFromReader(reader));
                        }
                    }
                }
            }

            return channels;
        }

        private Channel MapChannelFromReader(SqlDataReader reader)
        {
            var uuidValue = reader["Uuid"];
            var uuid = uuidValue is Guid guid ? guid.ToString() : (string)uuidValue;

            return new Channel(
                id: (int)reader["Id"],
                uuid: uuid,
                type: (ChannelType)(int)reader["Type"],
                createdAt: (DateTime)reader["CreatedAt"],
                updatedAt: (DateTime)reader["UpdatedAt"]
            );
        }
    }
}
