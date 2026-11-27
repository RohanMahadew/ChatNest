using BLL.Models;
using BLL.Models.DTOs;
using BLL.Models.Enums;
using BLL.Repositories.Interfaces;
using BLL.Services.Interfaces;

namespace BLL.Services
{
    public class MessageService : IMessageServiceInterface
    {
        private const int MaximumContentLength = 4000;
        private readonly IChannelServiceInterface _channelService;
        private readonly IMessageRepositoryInterface _messageRepository;
        private readonly IUserRepositoryInterface _userRepository;

        public MessageService(
            IChannelServiceInterface channelService,
            IMessageRepositoryInterface messageRepository,
            IUserRepositoryInterface userRepository)
        {
            _channelService = channelService;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public async Task<MessageDTO?> SendMessageAsync(string channelUuid, int senderUserId, string content)
        {
            ValidateContent(senderUserId, content);

            var membership = await _channelService.GetChatMembershipForUserAsync(channelUuid, senderUserId);
            if (membership is null)
            {
                return null;
            }

            var message = new Message(senderUserId, membership.Channel.Id, MessageStatus.Send, content.Trim());
            var stored = await _messageRepository.Store(message);
            if (stored is null)
            {
                return null;
            }

            var sender = await _userRepository.FindUserById(senderUserId);
            return MessageDTOExtensions.FromMessage(stored, sender?.UserName, membership.Role.ToString());
        }

        public async Task<List<MessageDTO>> GetMessagesForChannelAsync(string channelUuid, int userId)
        {
            var membership = await _channelService.GetChatMembershipForUserAsync(channelUuid, userId);
            if (membership is null)
            {
                return [];
            }

            var messages = (await _messageRepository.FindByChannel(membership.Channel.Id))
                .Where(message => message is not null)
                .Select(message => message!)
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .ToList();

            var senderNamesByUserId = new Dictionary<int, string?>();
            foreach (var senderId in messages.Select(message => message.UserId).Distinct())
            {
                var user = await _userRepository.FindUserById(senderId);
                senderNamesByUserId[senderId] = user?.UserName;
            }

            var members = await _channelService.GetChannelMembersAsync(membership.Channel.Uuid, userId);
            var rolesByUserId = members.ToDictionary(member => member.UserId, member => member.Role);

            return messages
                .Select(message => MessageDTOExtensions.FromMessage(
                    message,
                    senderNamesByUserId.GetValueOrDefault(message.UserId),
                    rolesByUserId.GetValueOrDefault(message.UserId)))
                .ToList();
        }

        public async Task<MessageDTO?> UpdateMessageAsync(string channelUuid, int messageId, int userId, string content)
        {
            if (messageId <= 0)
            {
                return null;
            }

            ValidateContent(userId, content);

            var membership = await _channelService.GetChatMembershipForUserAsync(channelUuid, userId);
            if (membership is null)
            {
                return null;
            }

            var existing = await _messageRepository.FindById(messageId);
            if (existing is null || existing.ChannelId != membership.Channel.Id || existing.UserId != userId)
            {
                return null;
            }

            var sender = await _userRepository.FindUserById(existing.UserId);
            var trimmedContent = content.Trim();
            if (string.Equals(existing.Content, trimmedContent, StringComparison.Ordinal))
            {
                return MessageDTOExtensions.FromMessage(existing, sender?.UserName, membership.Role.ToString());
            }

            existing.Content = trimmedContent;
            var updated = await _messageRepository.Update(existing);
            return updated is null ? null : MessageDTOExtensions.FromMessage(updated, sender?.UserName, membership.Role.ToString());
        }

        public async Task<bool> DeleteMessageAsync(string channelUuid, int messageId, int userId)
        {
            if (userId <= 0 || messageId <= 0)
            {
                return false;
            }

            var membership = await _channelService.GetChatMembershipForUserAsync(channelUuid, userId);
            if (membership is null)
            {
                return false;
            }

            var existing = await _messageRepository.FindById(messageId);
            if (existing is null || existing.ChannelId != membership.Channel.Id)
            {
                return false;
            }

            if (existing.UserId != userId)
            {
                if (membership.Channel.Type is not (ChannelType.PRIVATE or ChannelType.PUBLIC))
                {
                    return false;
                }

                if (membership.Role == ChannelRole.Admin)
                {
                    var authorRole = await _channelService.GetUserRoleInChannelAsync(membership.Channel.Id, existing.UserId);
                    if (authorRole is ChannelRole.SuperAdmin or ChannelRole.Admin)
                    {
                        return false;
                    }
                }
                else if (membership.Role != ChannelRole.SuperAdmin)
                {
                    return false;
                }
            }

            return await _messageRepository.Delete(messageId);
        }

        private static void ValidateContent(int userId, string content)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(content) || content.Length > MaximumContentLength)
            {
                throw new ArgumentException($"Message content must be between 1 and {MaximumContentLength} characters.");
            }
        }
    }
}
