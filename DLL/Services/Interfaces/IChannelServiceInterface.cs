using BLL.Models;
using BLL.Models.DTOs;

namespace BLL.Services.Interfaces
{
    public interface IChannelServiceInterface
    {
        Task<ChannelDTO> StartDirectChatAsync(int currentUserId, int targetUserId);
        Task<List<ChannelDTO>> GetDirectChatsForUserAsync(int userId);
        Task<ChannelDTO?> GetDirectChatForUserAsync(string uuid, int userId);
    }
}
