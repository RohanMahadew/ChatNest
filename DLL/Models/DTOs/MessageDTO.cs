namespace BLL.Models.DTOs
{
    public sealed record MessageDTO(
        int Id,
        int UserId,
        string SenderName,
        int ChannelId,
        string MessageStatus,
        string Content,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsEdited,
        string? SenderRole = null
    );

    public static class MessageDTOExtensions
    {
        public static MessageDTO FromMessage(Message message, string? senderName = null, string? senderRole = null)
        {
            var resolvedSenderName = string.IsNullOrWhiteSpace(senderName)
                ? $"User {message.UserId}"
                : senderName;

            return new MessageDTO(
                message.Id,
                message.UserId,
                resolvedSenderName,
                message.ChannelId,
                message.MessageStatus.ToString(),
                message.Content,
                message.CreatedAt,
                message.UpdatedAt,
                message.UpdatedAt > message.CreatedAt,
                senderRole
            );
        }
    }
}
