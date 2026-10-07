using ChatNest.Models.Base;

namespace ChatNest.Models
{
    public class User : BaseEntity
    {
        public User()
        {
            UserName = string.Empty;
            NormalizedUserName = string.Empty;
            Email = string.Empty;
            NormalizedEmail = string.Empty;
            PasswordHash = string.Empty;
        }

        public User(int id, string userName, string normalizedUserName, string email, string normalizedEmail, string passwordHash, DateOnly dateOfBirth)
        {
            Id = id;
            UserName = userName;
            NormalizedUserName = normalizedUserName;
            Email = email;
            NormalizedEmail = normalizedEmail;
            PasswordHash = passwordHash;
            DateOfBirth = dateOfBirth;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string PasswordHash { get; set; }
        public DateOnly DateOfBirth { get; set; }

    }
}
