using BLL.Models.DTOs;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PL.Hubs
{
    [Authorize]
    public sealed class ChatHub : Hub
    {
        private readonly IChannelServiceInterface _channelService;
        private readonly IMessageServiceInterface _messageService;

        public ChatHub(
            IChannelServiceInterface channelService,
            IMessageServiceInterface messageService)
        {
            _channelService = channelService;
            _messageService = messageService;
        }

        public async Task JoinChannel(string channelUuid)
        {
            var (_, channel) = await AuthorizeChannelAsync(channelUuid);
            await Groups.AddToGroupAsync(Context.ConnectionId, channel.Uuid);
        }

        public async Task<List<MessageDTO>> GetMessages(string channelUuid)
        {
            var (userId, channel) = await AuthorizeChannelAsync(channelUuid);
            return await _messageService.GetMessagesForChannelAsync(channel.Uuid, userId);
        }

        public async Task SendMessage(string channelUuid, string content)
        {
            var (userId, channel) = await AuthorizeChannelAsync(channelUuid);

            MessageDTO? message;
            try
            {
                message = await _messageService.SendMessageAsync(channel.Uuid, userId, content);
            }
            catch (ArgumentException)
            {
                throw new HubException("Message content must be between 1 and 4,000 characters.");
            }

            if (message is null)
            {
                throw new HubException("You do not have access to this channel.");
            }

            await Clients.Group(channel.Uuid).SendAsync("ReceiveMessage", message);
        }

        public async Task UpdateMessage(string channelUuid, int messageId, string content)
        {
            var (userId, channel) = await AuthorizeChannelAsync(channelUuid);

            MessageDTO? updatedMessage;
            try
            {
                updatedMessage = await _messageService.UpdateMessageAsync(channel.Uuid, messageId, userId, content);
            }
            catch (ArgumentException)
            {
                throw new HubException("Message content must be between 1 and 4,000 characters.");
            }

            if (updatedMessage is null)
            {
                throw new HubException("The message could not be updated.");
            }

            await Clients.Group(channel.Uuid).SendAsync("MessageUpdated", updatedMessage);
        }

        public async Task DeleteMessage(string channelUuid, int messageId)
        {
            var (userId, channel) = await AuthorizeChannelAsync(channelUuid);

            var deleted = await _messageService.DeleteMessageAsync(channel.Uuid, messageId, userId);
            if (!deleted)
            {
                throw new HubException("The message could not be deleted.");
            }

            await Clients.Group(channel.Uuid).SendAsync("MessageDeleted", messageId);
        }

        private async Task<(int UserId, ChannelDTO Channel)> AuthorizeChannelAsync(string channelUuid)
        {
            var userId = GetCurrentUserId();
            if (!Guid.TryParse(channelUuid, out var parsedUuid))
            {
                throw new HubException("The channel could not be found.");
            }

            var channel = await _channelService.GetChatChannelForUserAsync(parsedUuid.ToString("D"), userId);
            if (channel is null)
            {
                throw new HubException("You do not have access to this channel.");
            }

            return (userId, channel);
        }

        private int GetCurrentUserId()
        {
            if (int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return userId;
            }

            throw new HubException("You must be signed in to use chat.");
        }
    }
}
