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
                ?? throw new InvalidOperationException("The direct chat could not be created.");
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
            return memberships.Select(membership => ToGroupChannelDTO(membership.Channel, membership.Role)).ToList();
        }

        public async Task<GroupChannelDTO?> GetPrivateGroupForUserAsync(string uuid, int userId)
        {
            if (userId <= 0 || !Guid.TryParse(uuid, out var channelUuid))
            {
                return null;
            }

            var membership = await _channelRepository.FindPrivateGroupForUserAsync(channelUuid.ToString("D"), userId);
            return membership is null ? null : ToGroupChannelDTO(membership.Channel, membership.Role);
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

        public async Task<bool> RespondToPrivateGroupInvitationAsync(string uuid, int userId, bool accept)
        {
            if (userId <= 0 || !Guid.TryParse(uuid, out var channelUuid))
            {
                return false;
            }

            return await _channelRepository.RespondToPrivateGroupInvitationAsync(
                channelUuid.ToString("D"), userId, accept);
        }

        private static GroupChannelDTO ToGroupChannelDTO(Channel channel, ChannelRole role)
        {
            return new GroupChannelDTO(channel.Id, channel.Uuid, channel.Name, role.ToString());
        }
    }
}
