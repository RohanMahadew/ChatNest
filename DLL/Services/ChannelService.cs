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
    }
}
