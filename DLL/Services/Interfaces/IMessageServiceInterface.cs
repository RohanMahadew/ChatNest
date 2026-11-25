using BLL.Models.DTOs;

namespace BLL.Services.Interfaces
{
    public interface IMessageServiceInterface
    {
        Task<MessageDTO?> SendMessageAsync(string channelUuid, int senderUserId, string content);
        Task<List<MessageDTO>> GetMessagesForChannelAsync(string channelUuid, int userId);
        Task<MessageDTO?> UpdateMessageAsync(string channelUuid, int messageId, int userId, string content);
        Task<bool> DeleteMessageAsync(string channelUuid, int messageId, int userId);
    }
}
