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
                    if (await HasBlockedMemberAsync(connection, transaction, existingChannel.Id))
                    {
                        await transaction.RollbackAsync();
                        return null;
                    }

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
                    AND c.Type IN (@DirectType, @PrivateType, @PublicType)
                    AND (
                        c.Type <> @DirectType
                        OR NOT EXISTS (
                            SELECT 1 FROM dbo.ChannelUsers member
                            WHERE member.ChannelId = c.Id
                                AND member.ChannelRole <> @DirectMessageRole
                        )
                    )";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@DirectType", (int)ChannelType.DIRECT);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
            command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
            command.Parameters.AddWithValue("@DirectMessageRole", (int)ChannelRole.DirectMessage);

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
                    INNER JOIN dbo.ChannelUsers actor ON actor.ChannelId = c.Id
                        AND actor.UserId = @CreatorId AND actor.ChannelRole IN (@SuperAdminRole, @AdminRole)
                    WHERE c.Uuid = @Uuid AND c.Type = @PrivateType";

                int? channelId;
                using (var command = new SqlCommand(findChannelQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Uuid", uuid);
                    command.Parameters.AddWithValue("@CreatorId", creatorId);
                    command.Parameters.AddWithValue("@SuperAdminRole", (int)ChannelRole.SuperAdmin);
                    command.Parameters.AddWithValue("@AdminRole", (int)ChannelRole.Admin);
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

        public async Task<bool> RespondToPrivateGroupInvitationAsync(string uuid, int userId, GroupInvitationAction action)
        {
            const string updateQuery = @"UPDATE cu
                SET ChannelRole = @TargetRole, UpdatedAt = SYSUTCDATETIME()
                FROM dbo.ChannelUsers cu
                INNER JOIN dbo.Channels c ON c.Id = cu.ChannelId
                WHERE c.Uuid = @Uuid AND c.Type IN (@PrivateType, @PublicType)
                    AND cu.UserId = @UserId AND cu.ChannelRole = @InvitedRole";
            const string deleteQuery = @"DELETE cu
                FROM dbo.ChannelUsers cu
                INNER JOIN dbo.Channels c ON c.Id = cu.ChannelId
                WHERE c.Uuid = @Uuid AND c.Type IN (@PrivateType, @PublicType)
                    AND cu.UserId = @UserId AND cu.ChannelRole = @InvitedRole";

            var query = action == GroupInvitationAction.Deny ? deleteQuery : updateQuery;

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
            command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@InvitedRole", (int)ChannelRole.Invited);
            if (action != GroupInvitationAction.Deny)
            {
                var targetRole = action == GroupInvitationAction.Join
                    ? ChannelRole.Member
                    : ChannelRole.Blocked;
                command.Parameters.AddWithValue("@TargetRole", (int)targetRole);
            }

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> BlockDirectChannelAsync(string uuid, int userId)
        {
            const string query = @"UPDATE otherMember
                SET otherMember.ChannelRole = @BlockedRole, otherMember.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.ChannelUsers otherMember
                INNER JOIN dbo.Channels c ON c.Id = otherMember.ChannelId
                INNER JOIN dbo.ChannelUsers currentMember ON currentMember.ChannelId = c.Id
                    AND currentMember.UserId = @UserId
                    AND currentMember.ChannelRole = @DirectMessageRole
                WHERE c.Uuid = @Uuid
                    AND c.Type = @DirectType
                    AND otherMember.UserId <> @UserId
                    AND otherMember.ChannelRole = @DirectMessageRole";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BlockedRole", (int)ChannelRole.Blocked);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@DirectMessageRole", (int)ChannelRole.DirectMessage);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@DirectType", (int)ChannelType.DIRECT);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<ChannelMember>> FindChannelMembersAsync(int channelId)
        {
            var members = new List<ChannelMember>();
            const string query = @"SELECT u.Id AS UserId, u.UserName, cu.ChannelRole
                FROM dbo.ChannelUsers cu
                INNER JOIN dbo.Users u ON u.Id = cu.UserId
                WHERE cu.ChannelId = @ChannelId
                ORDER BY
                    CASE cu.ChannelRole
                        WHEN 5 THEN 0
                        WHEN 4 THEN 1
                        WHEN 3 THEN 2
                        WHEN 1 THEN 3
                        WHEN 7 THEN 4
                        WHEN 6 THEN 5
                        ELSE 6
                    END,
                    u.UserName";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ChannelId", channelId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                members.Add(new ChannelMember(
                    UserId: (int)reader["UserId"],
                    UserName: (string)reader["UserName"],
                    Role: (ChannelRole)(int)reader["ChannelRole"]));
            }

            return members;
        }

        public async Task<ChannelRole?> FindUserRoleInChannelAsync(int channelId, int userId)
        {
            const string query = @"SELECT ChannelRole
                FROM dbo.ChannelUsers
                WHERE ChannelId = @ChannelId AND UserId = @UserId";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ChannelId", channelId);
            command.Parameters.AddWithValue("@UserId", userId);

            var result = await command.ExecuteScalarAsync();
            return result is null || result is DBNull ? null : (ChannelRole)Convert.ToInt32(result);
        }

        public async Task<bool> UpdateGroupMemberRoleAsync(string uuid, int actorUserId, int targetUserId, ChannelRole targetRole)
        {
            if (actorUserId <= 0 || targetUserId <= 0 || actorUserId == targetUserId
                || targetRole is not (ChannelRole.Admin or ChannelRole.Member))
            {
                return false;
            }

            var requiredCurrentRole = targetRole == ChannelRole.Admin ? ChannelRole.Member : ChannelRole.Admin;
            const string query = @"UPDATE target
                SET target.ChannelRole = @TargetRole, target.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.ChannelUsers target
                INNER JOIN dbo.Channels c ON c.Id = target.ChannelId
                INNER JOIN dbo.ChannelUsers actor ON actor.ChannelId = c.Id
                    AND actor.UserId = @ActorUserId
                    AND actor.ChannelRole = @SuperAdminRole
                WHERE c.Uuid = @Uuid
                    AND c.Type IN (@PrivateType, @PublicType)
                    AND target.UserId = @TargetUserId
                    AND target.UserId <> @ActorUserId
                    AND target.ChannelRole = @RequiredCurrentRole";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TargetRole", (int)targetRole);
            command.Parameters.AddWithValue("@ActorUserId", actorUserId);
            command.Parameters.AddWithValue("@SuperAdminRole", (int)ChannelRole.SuperAdmin);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
            command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
            command.Parameters.AddWithValue("@TargetUserId", targetUserId);
            command.Parameters.AddWithValue("@RequiredCurrentRole", (int)requiredCurrentRole);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RemoveGroupMemberAsync(string uuid, int actorUserId, int targetUserId)
        {
            if (actorUserId <= 0 || targetUserId <= 0 || actorUserId == targetUserId)
            {
                return false;
            }

            const string query = @"DELETE target
                FROM dbo.ChannelUsers target
                INNER JOIN dbo.Channels c ON c.Id = target.ChannelId
                INNER JOIN dbo.ChannelUsers actor ON actor.ChannelId = c.Id
                    AND actor.UserId = @ActorUserId
                WHERE c.Uuid = @Uuid
                    AND c.Type IN (@PrivateType, @PublicType)
                    AND target.UserId = @TargetUserId
                    AND target.UserId <> @ActorUserId
                    AND target.ChannelRole <> @SuperAdminRole
                    AND (
                        actor.ChannelRole = @SuperAdminRole
                        OR (actor.ChannelRole = @AdminRole AND target.ChannelRole IN (@MemberRole, @InvitedRole, @BannedRole))
                    )";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ActorUserId", actorUserId);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
            command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
            command.Parameters.AddWithValue("@TargetUserId", targetUserId);
            command.Parameters.AddWithValue("@SuperAdminRole", (int)ChannelRole.SuperAdmin);
            command.Parameters.AddWithValue("@AdminRole", (int)ChannelRole.Admin);
            command.Parameters.AddWithValue("@MemberRole", (int)ChannelRole.Member);
            command.Parameters.AddWithValue("@InvitedRole", (int)ChannelRole.Invited);
            command.Parameters.AddWithValue("@BannedRole", (int)ChannelRole.Banned);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> BlockGroupMemberAsync(string uuid, int actorUserId, int targetUserId)
        {
            if (actorUserId <= 0 || targetUserId <= 0 || actorUserId == targetUserId)
            {
                return false;
            }

            const string query = @"UPDATE target
                SET target.ChannelRole = @BannedRole, target.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.ChannelUsers target
                INNER JOIN dbo.Channels c ON c.Id = target.ChannelId
                INNER JOIN dbo.ChannelUsers actor ON actor.ChannelId = c.Id
                    AND actor.UserId = @ActorUserId
                WHERE c.Uuid = @Uuid
                    AND c.Type IN (@PrivateType, @PublicType)
                    AND target.UserId = @TargetUserId
                    AND target.UserId <> @ActorUserId
                    AND target.ChannelRole <> @SuperAdminRole
                    AND target.ChannelRole <> @BannedRole
                    AND (
                        actor.ChannelRole = @SuperAdminRole
                        OR (actor.ChannelRole = @AdminRole AND target.ChannelRole IN (@MemberRole, @InvitedRole))
                    )";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BannedRole", (int)ChannelRole.Banned);
            command.Parameters.AddWithValue("@ActorUserId", actorUserId);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
            command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
            command.Parameters.AddWithValue("@TargetUserId", targetUserId);
            command.Parameters.AddWithValue("@SuperAdminRole", (int)ChannelRole.SuperAdmin);
            command.Parameters.AddWithValue("@AdminRole", (int)ChannelRole.Admin);
            command.Parameters.AddWithValue("@MemberRole", (int)ChannelRole.Member);
            command.Parameters.AddWithValue("@InvitedRole", (int)ChannelRole.Invited);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<ChannelType?> SetGroupVisibilityAsync(string uuid, int actorUserId, ChannelType targetType)
        {
            if (actorUserId <= 0 || targetType is not (ChannelType.PRIVATE or ChannelType.PUBLIC))
            {
                return null;
            }

            const string query = @"UPDATE c
                SET c.Type = @TargetType, c.UpdatedAt = SYSUTCDATETIME()
                OUTPUT INSERTED.Type
                FROM dbo.Channels c
                INNER JOIN dbo.ChannelUsers actor ON actor.ChannelId = c.Id
                    AND actor.UserId = @ActorUserId
                    AND actor.ChannelRole = @SuperAdminRole
                WHERE c.Uuid = @Uuid
                    AND c.Type IN (@PrivateType, @PublicType)";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TargetType", (int)targetType);
            command.Parameters.AddWithValue("@ActorUserId", actorUserId);
            command.Parameters.AddWithValue("@SuperAdminRole", (int)ChannelRole.SuperAdmin);
            command.Parameters.AddWithValue("@Uuid", uuid);
            command.Parameters.AddWithValue("@PrivateType", (int)ChannelType.PRIVATE);
            command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);

            var result = await command.ExecuteScalarAsync();
            return result is null || result is DBNull ? null : (ChannelType)Convert.ToInt32(result);
        }

        public async Task<List<PublicGroupMatch>> SearchPublicGroupsAsync(string searchQuery, int userId)
        {
            var groups = new List<PublicGroupMatch>();
            const string query = @"SELECT c.Id, c.Uuid, c.Type, c.Name, c.CreatedAt, c.UpdatedAt, cu.ChannelRole
                FROM dbo.Channels c
                LEFT JOIN dbo.ChannelUsers cu ON cu.ChannelId = c.Id AND cu.UserId = @UserId
                WHERE c.Type = @PublicType
                    AND c.Name LIKE @SearchPattern
                    AND (cu.ChannelRole IS NULL OR cu.ChannelRole NOT IN (@BannedRole, @BlockedRole))
                ORDER BY c.Name";

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
            command.Parameters.Add("@SearchPattern", System.Data.SqlDbType.NVarChar, 322).Value = $"%{searchQuery}%";
            command.Parameters.AddWithValue("@BannedRole", (int)ChannelRole.Banned);
            command.Parameters.AddWithValue("@BlockedRole", (int)ChannelRole.Blocked);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ChannelRole? role = reader["ChannelRole"] is DBNull
                    ? null
                    : (ChannelRole)(int)reader["ChannelRole"];
                groups.Add(new PublicGroupMatch(MapChannelFromReader(reader), role));
            }

            return groups;
        }

        public async Task<bool> JoinPublicGroupAsync(string uuid, int userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            using var connection = await _SqlConnection.GetConnectionAsync();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);

            try
            {
                const string findChannelQuery = @"SELECT Id
                    FROM dbo.Channels WITH (UPDLOCK, HOLDLOCK)
                    WHERE Uuid = @Uuid AND Type = @PublicType";

                int? channelId;
                using (var command = new SqlCommand(findChannelQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Uuid", uuid);
                    command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
                    var result = await command.ExecuteScalarAsync();
                    channelId = result is null || result is DBNull ? null : Convert.ToInt32(result);
                }

                if (channelId is null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                const string findMemberQuery = @"SELECT Id, ChannelRole
                    FROM dbo.ChannelUsers WITH (UPDLOCK, HOLDLOCK)
                    WHERE ChannelId = @ChannelId AND UserId = @UserId";

                int? existingMemberId = null;
                ChannelRole? existingRole = null;
                using (var command = new SqlCommand(findMemberQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@ChannelId", channelId.Value);
                    command.Parameters.AddWithValue("@UserId", userId);
                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        existingMemberId = (int)reader["Id"];
                        existingRole = (ChannelRole)(int)reader["ChannelRole"];
                    }
                }

                if (existingMemberId is null)
                {
                    const string insertQuery = @"INSERT INTO dbo.ChannelUsers (UserId, ChannelId, ChannelRole, CreatedAt, UpdatedAt)
                        VALUES (@UserId, @ChannelId, @MemberRole, SYSUTCDATETIME(), SYSUTCDATETIME())";
                    using var command = new SqlCommand(insertQuery, connection, transaction);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ChannelId", channelId.Value);
                    command.Parameters.AddWithValue("@MemberRole", (int)ChannelRole.Member);
                    await command.ExecuteNonQueryAsync();
                }
                else if (existingRole == ChannelRole.Invited)
                {
                    const string updateQuery = @"UPDATE dbo.ChannelUsers
                        SET ChannelRole = @MemberRole, UpdatedAt = SYSUTCDATETIME()
                        WHERE Id = @ChannelUserId";
                    using var command = new SqlCommand(updateQuery, connection, transaction);
                    command.Parameters.AddWithValue("@MemberRole", (int)ChannelRole.Member);
                    command.Parameters.AddWithValue("@ChannelUserId", existingMemberId.Value);
                    await command.ExecuteNonQueryAsync();
                }
                else if (existingRole is not (ChannelRole.Member or ChannelRole.Admin or ChannelRole.SuperAdmin))
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
                WHERE c.Type IN (@PrivateType, @PublicType)";
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
                command.Parameters.AddWithValue("@PublicType", (int)ChannelType.PUBLIC);
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

        private static async Task<bool> HasBlockedMemberAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int channelId)
        {
            const string query = @"SELECT TOP (1) 1
                FROM dbo.ChannelUsers WITH (UPDLOCK, HOLDLOCK)
                WHERE ChannelId = @ChannelId AND ChannelRole <> @DirectMessageRole";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@ChannelId", channelId);
            command.Parameters.AddWithValue("@DirectMessageRole", (int)ChannelRole.DirectMessage);

            var result = await command.ExecuteScalarAsync();
            return result is not null && result is not DBNull;
        }

        private async Task<List<Channel>> FindDirectChannelSummariesAsync(int userId, string? uuid)
        {
            var channels = new List<Channel>();
            var query = @"SELECT c.Id, c.Uuid, c.Type, otherUser.UserName AS Name, c.CreatedAt, c.UpdatedAt
                FROM dbo.Channels c
                INNER JOIN dbo.ChannelUsers currentMember ON currentMember.ChannelId = c.Id
                    AND currentMember.UserId = @UserId
                    AND currentMember.ChannelRole = @DirectMessageRole
                INNER JOIN dbo.ChannelUsers otherMember ON otherMember.ChannelId = c.Id
                    AND otherMember.UserId <> @UserId
                    AND otherMember.ChannelRole = @DirectMessageRole
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
                command.Parameters.AddWithValue("@DirectMessageRole", (int)ChannelRole.DirectMessage);
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
