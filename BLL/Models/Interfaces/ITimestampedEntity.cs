using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models.Interfaces
{
    public interface ITimestampedEntity
    {
        DateTime CreatedAt { get; }
        DateTime UpdatedAt { get; }
    }
}
