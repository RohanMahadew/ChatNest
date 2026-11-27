using BLL.Models.Enums;

namespace BLL.Models
{
    public sealed record ChannelMember(int UserId, string UserName, ChannelRole Role);
}

