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
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.Name, c.CreatedAt, c.UpdatedAt
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
                const string query = "SELECT Id, Uuid, Type, Name, CreatedAt, UpdatedAt FROM Channels WHERE Id = @ChannelId";

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
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.Name, c.CreatedAt, c.UpdatedAt
                FROM Channels c
                WHERE EXISTS (
                    SELECT 1 FROM ChannelUsers cu
                    WHERE cu.ChannelId = c.Id AND cu.UserId = @UserId)";
            return FindChannels(query, Channelid);
        }

        public Task<List<Channel?>> FindByUser(int Channelid)
        {
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.Name, c.CreatedAt, c.UpdatedAt
                FROM Channels c
                WHERE EXISTS (
                    SELECT 1 FROM ChannelUsers cu
                    WHERE cu.ChannelId = c.Id AND cu.UserId = @UserId)";
            return FindChannels(query, Channelid);
        }

        public async Task<Channel?> GetOrCreateDirectChannelAsync(Channel channel, ChannelUser firstMember, ChannelUser secondMember)
        {
            if (channel.Type != ChannelType.DIRECT
                || firstMember.UserId == secondMember.UserId
                || firstMember.ChannelRole != ChannelRole.DirectMessage
                || secondMember.ChannelRole != ChannelRole.DirectMessage
                || string.IsNullOrWhiteSpace(channel.Uuid))
            {
                throw new ArgumentException("A direct channel requires a UUID and two members.");
            }

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);

            try
            {
                var existingChannel = await FindDirectChannelBetweenUsersAsync(
                    connection,
                    transaction,
                    firstMember.UserId,
                    secondMember.UserId);
                if (existingChannel is not null)
                {
                    await transaction.CommitAsync();
                    return existingChannel;
                }

                const string insertChannelQuery = @"INSERT INTO dbo.Channels (Uuid, Type, Name, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id, INSERTED.Uuid, INSERTED.Type, INSERTED.Name, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    VALUES (@Uuid, @Type, NULL, SYSUTCDATETIME(), SYSUTCDATETIME())";

                Channel? createdChannel;
                using (var command = new SqlCommand(insertChannelQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Uuid", channel.Uuid);
                    command.Parameters.AddWithValue("@Type", (int)ChannelType.DIRECT);

                    using var reader = await command.ExecuteReaderAsync();
                    createdChannel = await reader.ReadAsync() ? MapChannelFromReader(reader) : null;
                }

                if (createdChannel is null)
                {
                    throw new InvalidOperationException("The direct channel could not be created.");
                }

                firstMember.ChannelId = createdChannel.Id;
                secondMember.ChannelId = createdChannel.Id;

                const string insertMembersQuery = @"INSERT INTO dbo.ChannelUsers (UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt)
                    VALUES (@FirstUserId, @ChannelId, @FirstRole, SYSUTCDATETIME(), SYSUTCDATETIME()),
                           (@SecondUserId, @ChannelId, @SecondRole, SYSUTCDATETIME(), SYSUTCDATETIME())";

                using (var command = new SqlCommand(insertMembersQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@FirstUserId", firstMember.UserId);
                    command.Parameters.AddWithValue("@SecondUserId", secondMember.UserId);
                    command.Parameters.AddWithValue("@ChannelId", createdChannel.Id);
                    command.Parameters.AddWithValue("@FirstRole", (int)firstMember.ChannelRole);
                    command.Parameters.AddWithValue("@SecondRole", (int)secondMember.ChannelRole);
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return createdChannel;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Task<List<Channel>> FindDirectChannelsForUserAsync(int userId)
        {
            return FindDirectChannelSummariesAsync(userId, null);
        }

        public async Task<Channel?> FindDirectChannelForUserAsync(string uuid, int userId)
        {
            var channels = await FindDirectChannelSummariesAsync(userId, uuid);
            return channels.FirstOrDefault();
        }

        public async Task<Channel?> CreatePrivateGroupAsync(Channel channel, int creatorId)
        {
            if (channel.Type != ChannelType.PRIVATE
                || creatorId <= 0
                || string.IsNullOrWhiteSpace(channel.Uuid)
                || string.IsNullOrWhiteSpace(channel.Name))
            {
                throw new ArgumentException("A private group requires a UUID, name, and creator.");
            }

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertChannelQuery = @"INSERT INTO dbo.Channels (Uuid, Type, Name, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id, INSERTED.Uuid, INSERTED.Type, INSERTED.Name, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    VALUES (@Uuid, @Type, @Name, SYSUTCDATETIME(), SYSUTCDATETIME())";

                Channel? createdChannel;
                using (var command = new SqlCommand(insertChannelQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Uuid", channel.Uuid);
                    command.Parameters.AddWithValue("@Type", (int)ChannelType.PRIVATE);
                    command.Parameters.AddWithValue("@Name", channel.Name);

                    using var reader = await command.ExecuteReaderAsync();
                    createdChannel = await reader.ReadAsync() ? MapChannelFromReader(reader) : null;
                }

                if (createdChannel is null)
                {
                    throw new InvalidOperationException("The private group could not be created.");
                }

                const string insertCreatorQuery = @"INSERT INTO dbo.ChannelUsers (UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt)
                    VALUES (@CreatorId, @ChannelId, @SuperAdminRole, SYSUTCDATETIME(), SYSUTCDATETIME())";
                using (var command = new SqlCommand(insertCreatorQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@CreatorId", creatorId);
                    command.Parameters.AddWithValue("@ChannelId", createdChannel.Id);
                    command.Parameters.AddWithValue("@SuperAdminRole", (int)ChannelRole.SuperAdmin);
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return createdChannel;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Task<List<ChannelMembership>> FindPrivateGroupsForUserAsync(int userId)
        {
            return FindPrivateGroupMembershipsAsync(userId, null);
        }

        public async Task<ChannelMembership?> FindPrivateGroupForUserAsync(string uuid, int userId)
        {
            var memberships = await FindPrivateGroupMembershipsAsync(userId, uuid);
            return memberships.FirstOrDefault();
        }

        public async Task<ChannelMembership?> FindChatChannelForUserAsync(string uuid, int userId)
        {
            using var connection = await _SqlConnection.GetConnectionAsync();
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.Name, c.CreatedAt, c.UpdatedAt, cu.ChannelRole
                FROM dbo.Channels c
                INNER JOIN dbo.ChannelUsers cu ON cu.ChannelId = c.Id
                WHERE c.Uuid = @Uuid AND cu.UserId = @UserId
                    AND c.Type IN (@DirectType, @PrivateType)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@DirectType", (int)ChannelType.DIRECT);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync()
                ? new ChannelMembership(MapChannelFromReader(reader), (ChannelRole)(int)reader["ChannelRole"])
                : null;
        }

        public async Task<bool> InviteUserToPrivateGroupAsync(string uuid, int creatorId, int targetUserId)
        {
            if (creatorId <= 0 || targetUserId <= 0 || creatorId == targetUserId)
            {
                return false;
            }

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);

            try
            {
                const string findChannelQuery = @"SELECT c.Id
                    FROM dbo.Channels c WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.ChannelUsers creator ON creator.ChannelId = c.Id
                        AND creator.UserId = @CreatorId AND creator.ChannelRole = @SuperAdminRole
                    WHERE c.Uuid = @Uuid AND c.Type = @PrivateType";

                int? channelId;
                using (var command = new SqlCommand(findChannelQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Uuid", uuid);
                    command.Parameters.AddWithValue("@CreatorId", creatorId);
                    command.Parameters.AddWithValue("@SuperAdminRole", (int)ChannelRole.SuperAdmin);
                    command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
                    var result = await command.ExecuteScalarAsync();
                    channelId = result is null || result is DBNull ? null : Convert.ToInt32(result);
                }

                if (channelId is null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                const string insertInviteQuery = @"INSERT INTO dbo.ChannelUsers (UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt)
                    SELECT @TargetUserId, @ChannelId, @InvitedRole, SYSUTCDATETIME(), SYSUTCDATETIME()
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.ChannelUsers WITH (UPDLOCK, HOLDLOCK)
                        WHERE UserId = @TargetUserId AND ChannelId = @ChannelId)";
                int inserted;
                using (var command = new SqlCommand(insertInviteQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@TargetUserId", targetUserId);
                    command.Parameters.AddWithValue("@ChannelId", channelId.Value);
                    command.Parameters.AddWithValue("@InvitedRole", (int)ChannelRole.Invited);
                    inserted = await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return inserted > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RespondToPrivateGroupInvitationAsync(string uuid, int userId, bool accept)
        {
            const string updateQuery = @"UPDATE cu
                SET ChannelRole = @MemberRole, UpdatedAt = SYSUTCDATETIME()
                FROM dbo.ChannelUsers cu
                INNER JOIN dbo.Channels c ON c.Id = cu.ChannelId
                WHERE c.Uuid = @Uuid AND c.Type = @PrivateType
                    AND cu.UserId = @UserId AND cu.ChannelRole = @InvitedRole";
            const string deleteQuery = @"DELETE cu
                FROM dbo.ChannelUsers cu
                INNER JOIN dbo.Channels c ON c.Id = cu.ChannelId
                WHERE c.Uuid = @Uuid AND c.Type = @PrivateType
                    AND cu.UserId = @UserId AND cu.ChannelRole = @InvitedRole";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(accept ? updateQuery : deleteQuery, connection);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@InvitedRole", (int)ChannelRole.Invited);
            if (accept)
            {
                command.Parameters.AddWithValue("@MemberRole", (int)ChannelRole.Member);
            }

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<Channel?> Store(Channel channel)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"INSERT INTO Channels
                    (Uuid, Type, Name, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id, INSERTED.Uuid, INSERTED.Type, INSERTED.Name, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    VALUES (@Uuid, @Type, @Name, GETDATE(), GETDATE())";

                using (var command = new SqlCommand(query, connection))
                {
                    var uuid = string.IsNullOrWhiteSpace(channel.Uuid) ? Guid.NewGuid().ToString() : channel.Uuid;
                    command.Parameters.AddWithValue("@Uuid", uuid);
                    command.Parameters.AddWithValue("@Type", (int)channel.Type);
                    command.Parameters.AddWithValue("@Name", (object?)channel.Name ?? DBNull.Value);

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
                    SET Uuid = @Uuid, Type = @Type, Name = @Name, UpdatedAt = GETDATE()
                    OUTPUT INSERTED.Id, INSERTED.Uuid, INSERTED.Type, INSERTED.Name, INSERTED.CreatedAt, INSERTED.UpdatedAt
                    WHERE Id = @ChannelId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChannelId", channel.Id);
                    command.Parameters.AddWithValue("@Uuid", channel.Uuid);
                    command.Parameters.AddWithValue("@Type", (int)channel.Type);
                    command.Parameters.AddWithValue("@Name", (object?)channel.Name ?? DBNull.Value);

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

        private async Task<List<ChannelMembership>> FindPrivateGroupMembershipsAsync(int userId, string? uuid)
        {
            var memberships = new List<ChannelMembership>();
            var query = @"SELECT c.Id, c.Uuid, c.Type, c.Name, c.CreatedAt, c.UpdatedAt, cu.ChannelRole
                FROM dbo.Channels c
                INNER JOIN dbo.ChannelUsers cu ON cu.ChannelId = c.Id AND cu.UserId = @UserId
                WHERE c.Type = @PrivateType";
            if (uuid is not null)
            {
                query += " AND c.Uuid = @Uuid";
            }
            query += " ORDER BY CASE WHEN cu.ChannelRole = @InvitedRole THEN 0 ELSE 1 END, c.Name";

            using (var connection = await _SqlConnection.GetConnectionAsync())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
                command.Parameters.AddWithValue("@InvitedRole", (int)ChannelRole.Invited);
                if (uuid is not null)
                {
                    command.Parameters.AddWithValue("@Uuid", uuid);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    memberships.Add(new ChannelMembership(
                        MapChannelFromReader(reader),
                        (ChannelRole)(int)reader["ChannelRole"]));
                }
            }

            return memberships;
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

        private async Task<Channel?> FindDirectChannelBetweenUsersAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int firstUserId,
            int secondUserId)
        {
            const string query = @"SELECT TOP (1) c.Id, c.Uuid, c.Type, c.Name, c.CreatedAt, c.UpdatedAt
                FROM dbo.Channels c
                WHERE c.Type = @DirectType
                    AND EXISTS (SELECT 1 FROM dbo.ChannelUsers cu WHERE cu.ChannelId = c.Id AND cu.UserId = @FirstUserId)
                    AND EXISTS (SELECT 1 FROM dbo.ChannelUsers cu WHERE cu.ChannelId = c.Id AND cu.UserId = @SecondUserId)
                    AND (SELECT COUNT(*) FROM dbo.ChannelUsers cu WHERE cu.ChannelId = c.Id) = 2";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@DirectType", (int)ChannelType.DIRECT);
            command.Parameters.AddWithValue("@FirstUserId", firstUserId);
            command.Parameters.AddWithValue("@SecondUserId", secondUserId);

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapChannelFromReader(reader) : null;
        }

        private async Task<List<Channel>> FindDirectChannelSummariesAsync(int userId, string? uuid)
        {
            var channels = new List<Channel>();
            var query = @"SELECT c.Id, c.Uuid, c.Type, otherUser.UserName AS Name, c.CreatedAt, c.UpdatedAt
                FROM dbo.Channels c
                INNER JOIN dbo.ChannelUsers currentMember ON currentMember.ChannelId = c.Id AND currentMember.UserId = @UserId
                INNER JOIN dbo.ChannelUsers otherMember ON otherMember.ChannelId = c.Id AND otherMember.UserId <> @UserId
                INNER JOIN dbo.Users otherUser ON otherUser.Id = otherMember.UserId
                WHERE c.Type = @DirectType
                    AND (SELECT COUNT(*) FROM dbo.ChannelUsers cu WHERE cu.ChannelId = c.Id) = 2";
            if (uuid is not null)
            {
                query += " AND c.Uuid = @Uuid";
            }
            query += " ORDER BY otherUser.UserName";

            using (var connection = await _SqlConnection.GetConnectionAsync())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@DirectType", (int)ChannelType.DIRECT);
                if (uuid is not null)
                {
                    command.Parameters.AddWithValue("@Uuid", uuid);
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        channels.Add(MapChannelFromReader(reader));
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
                name: reader["Name"] is DBNull ? null : (string)reader["Name"],
                createdAt: (DateTime)reader["CreatedAt"],
                updatedAt: (DateTime)reader["UpdatedAt"]
            );
        }
    }
}
