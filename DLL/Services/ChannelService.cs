using BLL.Models;
using BLL.Models.DTOs;
using BLL.Models.Enums;
using BLL.Repositories.Interfaces;
using BLL.Services.Interfaces;

namespace BLL.Services
{
    public class ChannelService : IChannelServiceInterface
    {
        private readonly IChannelRepositoryInterface _channelRepository;
        private readonly IUserRepositoryInterface _userRepository;

        public ChannelService(
            IChannelRepositoryInterface channelRepository,
            IUserRepositoryInterface userRepository)
        {
            _channelRepository = channelRepository;
            _userRepository = userRepository;
        }

        public async Task<ChannelDTO> StartDirectChatAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId <= 0 || targetUserId <= 0 || currentUserId == targetUserId)
            {
                throw new ArgumentException("A direct chat requires two distinct users.");
            }

            var currentUser = await _userRepository.FindUserById(currentUserId);
            var targetUser = await _userRepository.FindUserById(targetUserId);
            if (currentUser is null || targetUser is null)
            {
                throw new InvalidOperationException("One or both users could not be found.");
            }

            var channel = new Channel(Guid.NewGuid().ToString("D"), ChannelType.DIRECT);
            var currentMember = new ChannelUser(currentUserId, 0, ChannelRole.DirectMessage);
            var targetMember = new ChannelUser(targetUserId, 0, ChannelRole.DirectMessage);
            var directChannel = await _channelRepository.GetOrCreateDirectChannelAsync(channel, currentMember, targetMember)
                ?? throw new InvalidOperationException("This direct chat is blocked or could not be created.");
            return ChannelDTOExtensions.fromChannel(directChannel) with { Name = targetUser.UserName };
        }

        public async Task<List<ChannelDTO>> GetDirectChatsForUserAsync(int userId)
        {
            var channels = await _channelRepository.FindDirectChannelsForUserAsync(userId);
            return channels.Select(ChannelDTOExtensions.fromChannel).ToList();
        }

        public async Task<ChannelDTO?> GetDirectChatForUserAsync(string uuid, int userId)
        {
            var channel = await _channelRepository.FindDirectChannelForUserAsync(uuid, userId);
            return channel is null ? null : ChannelDTOExtensions.fromChannel(channel);
        }

        public async Task<bool> BlockDirectChatAsync(string uuid, int userId)
        {
            if (userId <= 0 || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.BlockDirectChannelAsync(channelUuid.ToString("D"), userId);
        }

        public async Task<GroupChannelDTO> CreatePrivateGroupAsync(int creatorId, string name)
        {
            var groupName = name?.Trim();
            if (creatorId <= 0 || string.IsNullOrWhiteSpace(groupName) || groupName.Length > 50)
            {
                throw new ArgumentException("A private group requires a creator and a name of 1 to 50 characters.");
            }

            if (await _userRepository.FindUserById(creatorId) is null)
            {
                throw new InvalidOperationException("The creator could not be found.");
            }

            var channel = new Channel(Guid.NewGuid().ToString("D"), ChannelType.PRIVATE)
            {
                Name = groupName
            };
            var createdChannel = await _channelRepository.CreatePrivateGroupAsync(channel, creatorId)
                ?? throw new InvalidOperationException("The private group could not be created.");

            return ToGroupChannelDTO(createdChannel, ChannelRole.SuperAdmin);
        }

        public async Task<List<GroupChannelDTO>> GetPrivateGroupsForUserAsync(int userId)
        {
            if (userId <= 0)
            {
                return [];
            }

            var memberships = await _channelRepository.FindPrivateGroupsForUserAsync(userId);
            return memberships
                .Where(membership => IsVisibleGroupRole(membership.Role))
                .Select(membership => ToGroupChannelDTO(membership.Channel, membership.Role))
                .ToList();
        }

        public async Task<GroupChannelDTO?> GetPrivateGroupForUserAsync(string uuid, int userId)
        {
            if (userId <= 0 || !Guid.TryParse(uuid, out var channelUuid))
            {
                return null;
            }

            var membership = await _channelRepository.FindPrivateGroupForUserAsync(channelUuid.ToString("D"), userId);
            return membership is null || !IsVisibleGroupRole(membership.Role)
                ? null
                : ToGroupChannelDTO(membership.Channel, membership.Role);
        }

        public async Task<bool> InviteUserToPrivateGroupAsync(string uuid, int creatorId, int targetUserId)
        {
            if (creatorId <= 0 || targetUserId <= 0 || creatorId == targetUserId
                || !Guid.TryParse(uuid, out var channelUuid)
                || await _userRepository.FindUserById(targetUserId) is null)
            {
                return false;
            }

            return await _channelRepository.InviteUserToPrivateGroupAsync(
                channelUuid.ToString("D"), creatorId, targetUserId);
        }

        public async Task<bool> RespondToPrivateGroupInvitationAsync(string uuid, int userId, GroupInvitationAction action)
        {
            if (userId <= 0 || !Enum.IsDefined(action) || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.RespondToPrivateGroupInvitationAsync(
                channelUuid.ToString("D"), userId, action);
        }

        public async Task<ChannelDTO?> GetChatChannelForUserAsync(string uuid, int userId)
        {
            var membership = await GetChatMembershipForUserAsync(uuid, userId);
            return membership is null ? null : ChannelDTOExtensions.fromChannel(membership.Channel);
        }

        public async Task<ChannelMembership?> GetChatMembershipForUserAsync(string uuid, int userId)
        {
            if (userId <= 0 || !Guid.TryParse(uuid, out var channelUuid))
            {
                return null;
            }

            var membership = await _channelRepository.FindChatChannelForUserAsync(channelUuid.ToString("D"), userId);
            if (membership is null)
            {
                return null;
            }

            var isActiveMember = membership.Channel.Type switch
            {
                ChannelType.DIRECT => membership.Role == ChannelRole.DirectMessage,
                ChannelType.PRIVATE or ChannelType.PUBLIC => membership.Role is ChannelRole.Member or ChannelRole.Admin or ChannelRole.SuperAdmin,
                _ => false
            };

            return isActiveMember ? membership : null;
        }

        public async Task<List<ChannelMemberDTO>> GetChannelMembersAsync(string uuid, int userId)
        {
            var membership = await GetChatMembershipForUserAsync(uuid, userId);
            if (membership is null)
            {
                return [];
            }

            var members = await _channelRepository.FindChannelMembersAsync(membership.Channel.Id);
            return members
                .Select(member => new ChannelMemberDTO(
                    member.UserId,
                    member.UserName,
                    member.Role.ToString(),
                    (int)member.Role))
                .ToList();
        }

        public async Task<ChannelRole?> GetUserRoleInChannelAsync(int channelId, int userId)
        {
            if (channelId <= 0 || userId <= 0)
            {
                return null;
            }

            return await _channelRepository.FindUserRoleInChannelAsync(channelId, userId);
        }

        public async Task<bool> PromoteGroupMemberAsync(string uuid, int actorUserId, int targetUserId)
        {
            if (actorUserId <= 0 || targetUserId <= 0 || actorUserId == targetUserId || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.UpdateGroupMemberRoleAsync(
                channelUuid.ToString("D"), actorUserId, targetUserId, ChannelRole.Admin);
        }

        public async Task<bool> DemoteGroupMemberAsync(string uuid, int actorUserId, int targetUserId)
        {
            if (actorUserId <= 0 || targetUserId <= 0 || actorUserId == targetUserId || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.UpdateGroupMemberRoleAsync(
                channelUuid.ToString("D"), actorUserId, targetUserId, ChannelRole.Member);
        }

        public async Task<bool> RemoveGroupMemberAsync(string uuid, int actorUserId, int targetUserId)
        {
            if (actorUserId <= 0 || targetUserId <= 0 || actorUserId == targetUserId || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.RemoveGroupMemberAsync(
                channelUuid.ToString("D"), actorUserId, targetUserId);
        }

        public async Task<bool> BlockGroupMemberAsync(string uuid, int actorUserId, int targetUserId)
        {
            if (actorUserId <= 0 || targetUserId <= 0 || actorUserId == targetUserId || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.BlockGroupMemberAsync(
                channelUuid.ToString("D"), actorUserId, targetUserId);
        }

        public async Task<ChannelType?> ToggleGroupVisibilityAsync(string uuid, int actorUserId)
        {
            if (actorUserId <= 0 || !Guid.TryParse(uuid, out var channelUuid))
            {
                return null;
            }

            var normalizedUuid = channelUuid.ToString("D");
            var membership = await _channelRepository.FindPrivateGroupForUserAsync(normalizedUuid, actorUserId);
            if (membership is null || membership.Role != ChannelRole.SuperAdmin)
            {
                return null;
            }

            var targetType = membership.Channel.Type == ChannelType.PUBLIC
                ? ChannelType.PRIVATE
                : ChannelType.PUBLIC;
            return await _channelRepository.SetGroupVisibilityAsync(normalizedUuid, actorUserId, targetType);
        }

        public async Task<List<PublicGroupDTO>> SearchPublicGroupsAsync(string searchQuery, int userId)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length > 320)
            {
                return [];
            }

            var groups = await _channelRepository.SearchPublicGroupsAsync(searchQuery.Trim(), userId);
            return groups
                .Select(match => new PublicGroupDTO(
                    match.Channel.Id,
                    match.Channel.Uuid,
                    match.Channel.Name ?? "Public group",
                    match.Role is ChannelRole.Member or ChannelRole.Admin or ChannelRole.SuperAdmin))
                .ToList();
        }

        public async Task<bool> JoinPublicGroupAsync(string uuid, int userId)
        {
            if (userId <= 0 || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.JoinPublicGroupAsync(channelUuid.ToString("D"), userId);
        }

        private static GroupChannelDTO ToGroupChannelDTO(Channel channel, ChannelRole role)
        {
            return new GroupChannelDTO(channel.Id, channel.Uuid, channel.Name, role.ToString(), channel.Type.ToString());
        }

        private static bool IsVisibleGroupRole(ChannelRole role)
        {
            return role is ChannelRole.Invited or ChannelRole.Member or ChannelRole.Admin or ChannelRole.SuperAdmin;
        }
    }
}
