using ChatNest.Models.Base;
using Microsoft.AspNetCore.Identity;

namespace ChatNest.Models
{
    public class ApplicationUser : IdentityUser, ITimestampedEntity
    {
        public DateOnly DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
