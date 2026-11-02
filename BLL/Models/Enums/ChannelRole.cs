using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Enums
{
    public enum ChannelRole
    {
        DirectMessage = 1,
        Blocked = 2,
        Member = 3,
        Admin = 4,
        SuperAdmin = 5,
        Banned = 6,
        Invited = 7
    }
}
