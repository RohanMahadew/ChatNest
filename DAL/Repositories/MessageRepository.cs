using BLL.Engines.Interfaces;
using BLL.Models;
using BLL.Models.Enums;
using BLL.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace DAL.Repositories
{
    public class MessageRepository : IMessageRepositoryInterface
    {
        private readonly ISqlConnectionInterface _SqlConnection;

        public MessageRepository(ISqlConnectionInterface sqlConnection)
        {
            _SqlConnection = sqlConnection;
        }

        public async Task<bool> Delete(int MessageId)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "DELETE FROM Messages WHERE Id = @MessageId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", MessageId);

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public Task<List<Message?>> FindByChannel(int ChannelId)
        {
            const string query = "SELECT Id, UserId, ChannelId, MessageStatus, Content, CreatedAt, UpdatedAt FROM Messages WHERE ChannelId = @ChannelId";
            return FindMessages(query, "@ChannelId", ChannelId);
        }

        public Task<List<Message?>> FindByContent(string Content)
        {
            const string query = "SELECT Id, UserId, ChannelId, MessageStatus, Content, CreatedAt, UpdatedAt FROM Messages WHERE Content LIKE @Content";
            return FindMessages(query, "@Content", $"%{Content}%");
        }

        public async Task<Message?> FindById(int MessageId)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "SELECT Id, UserId, ChannelId, MessageStatus, Content, CreatedAt, UpdatedAt FROM Messages WHERE Id = @MessageId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", MessageId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapMessageFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public Task<List<Message?>> FindByReceiver(int ReceiverId)
        {
            const string query = @"SELECT m.Id, m.UserId, m.ChannelId, m.MessageStatus, m.Content, m.CreatedAt, m.UpdatedAt
                FROM Messages m
                WHERE EXISTS (
                    SELECT 1 FROM ChannelUsers cu
                    WHERE cu.ChannelId = m.ChannelId AND cu.UserId = @ReceiverId)";
            return FindMessages(query, "@ReceiverId", ReceiverId);
        }

        public Task<List<Message?>> FindBySender(int SenderId)
        {
            const string query = "SELECT Id, UserId, ChannelId, MessageStatus, Content, CreatedAt, UpdatedAt FROM Messages WHERE UserId = @SenderId";
            return FindMessages(query, "@SenderId", SenderId);
        }

        public Task<List<Message?>> FindByStatus(MessageStatus Status)
        {
            const string query = "SELECT Id, UserId, ChannelId, MessageStatus, Content, CreatedAt, UpdatedAt FROM Messages WHERE MessageStatus = @MessageStatus";
            return FindMessages(query, "@MessageStatus", (int)Status);
        }

        public Task<List<Message?>> FindByUser(int UserId)
        {
            const string query = "SELECT Id, UserId, ChannelId, MessageStatus, Content, CreatedAt, UpdatedAt FROM Messages WHERE UserId = @UserId";
            return FindMessages(query, "@UserId", UserId);
        }

        public async Task<Message?> Store(Message message)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"INSERT INTO Messages
                    (UserId, ChannelId, MessageStatus, Content, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.ChannelId, INSERTED.MessageStatus, INSERTED.Content, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    VALUES (@UserId, @ChannelId, @MessageStatus, @Content, GETDATE(), GETDATE())";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", message.UserId);
                    command.Parameters.AddWithValue("@ChannelId", message.ChannelId);
                    command.Parameters.AddWithValue("@MessageStatus", (int)message.MessageStatus);
                    command.Parameters.AddWithValue("@Content", message.Content);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapMessageFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public async Task<Message?> Update(Message message)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"UPDATE Messages
                    SET UserId = @UserId, ChannelId = @ChannelId, MessageStatus = @MessageStatus, Content = @Content, UpdatedAt = GETDATE()
                    OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.ChannelId, INSERTED.MessageStatus, INSERTED.Content, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    WHERE Id = @MessageId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", message.Id);
                    command.Parameters.AddWithValue("@UserId", message.UserId);
                    command.Parameters.AddWithValue("@ChannelId", message.ChannelId);
                    command.Parameters.AddWithValue("@MessageStatus", (int)message.MessageStatus);
                    command.Parameters.AddWithValue("@Content", message.Content);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapMessageFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        private async Task<List<Message?>> FindMessages(string query, string parameterName, object parameterValue)
        {
            var messages = new List<Message?>();

            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(parameterName, parameterValue);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            messages.Add(MapMessageFromReader(reader));
                        }
                    }
                }
            }

            return messages;
        }

        private Message MapMessageFromReader(SqlDataReader reader)
        {
            return new Message(
                id: (int)reader["Id"],
                userId: (int)reader["UserId"],
                channelId: (int)reader["ChannelId"],
                messageStatus: (MessageStatus)(int)reader["MessageStatus"],
                content: (string)reader["Content"],
                createdAt: (DateTime)reader["CreatedAt"],
                updatedAt: (DateTime)reader["UpdatedAt"]
            );
        }
    }
}
