using BLL.Models;
using BLL.Models.DTOs;

namespace BLL.Services.Interfaces
{
    public interface IChannelServiceInterface
    {
        Task<ChannelDTO> StartDirectChatAsync(int currentUserId, int targetUserId);
        Task<List<ChannelDTO>> GetDirectChatsForUserAsync(int userId);
        Task<ChannelDTO?> GetDirectChatForUserAsync(string uuid, int userId);
        Task<GroupChannelDTO> CreatePrivateGroupAsync(int creatorId, string name);
        Task<List<GroupChannelDTO>> GetPrivateGroupsForUserAsync(int userId);
        Task<GroupChannelDTO?> GetPrivateGroupForUserAsync(string uuid, int userId);
        Task<bool> InviteUserToPrivateGroupAsync(string uuid, int creatorId, int targetUserId);
        Task<bool> RespondToPrivateGroupInvitationAsync(string uuid, int userId, bool accept);
    }
}
