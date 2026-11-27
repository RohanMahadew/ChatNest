namespace BLL.Models.DTOs
{
    public sealed record ChannelMemberDTO(
        int UserId,
        string UserName,
        string Role,
        int RoleId
    );
}

