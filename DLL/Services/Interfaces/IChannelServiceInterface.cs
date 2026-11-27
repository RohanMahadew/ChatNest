using BLL.Models;
using BLL.Models.DTOs;
using BLL.Models.Enums;

namespace BLL.Services.Interfaces
{
    public interface IChannelServiceInterface
    {
        Task<ChannelDTO> StartDirectChatAsync(int currentUserId, int targetUserId);
        Task<List<ChannelDTO>> GetDirectChatsForUserAsync(int userId);
        Task<ChannelDTO?> GetDirectChatForUserAsync(string uuid, int userId);
        Task<bool> BlockDirectChatAsync(string uuid, int userId);
        Task<GroupChannelDTO> CreatePrivateGroupAsync(int creatorId, string name);
        Task<List<GroupChannelDTO>> GetPrivateGroupsForUserAsync(int userId);
        Task<GroupChannelDTO?> GetPrivateGroupForUserAsync(string uuid, int userId);
        Task<bool> InviteUserToPrivateGroupAsync(string uuid, int creatorId, int targetUserId);
        Task<bool> RespondToPrivateGroupInvitationAsync(string uuid, int userId, GroupInvitationAction action);
        Task<ChannelDTO?> GetChatChannelForUserAsync(string uuid, int userId);
        Task<ChannelMembership?> GetChatMembershipForUserAsync(string uuid, int userId);
        Task<List<ChannelMemberDTO>> GetChannelMembersAsync(string uuid, int userId);
        Task<ChannelRole?> GetUserRoleInChannelAsync(int channelId, int userId);
        Task<bool> PromoteGroupMemberAsync(string uuid, int actorUserId, int targetUserId);
        Task<bool> DemoteGroupMemberAsync(string uuid, int actorUserId, int targetUserId);
        Task<bool> RemoveGroupMemberAsync(string uuid, int actorUserId, int targetUserId);
        Task<bool> BlockGroupMemberAsync(string uuid, int actorUserId, int targetUserId);
        Task<ChannelType?> ToggleGroupVisibilityAsync(string uuid, int actorUserId);
        Task<List<PublicGroupDTO>> SearchPublicGroupsAsync(string searchQuery, int userId);
        Task<bool> JoinPublicGroupAsync(string uuid, int userId);
    }
}
