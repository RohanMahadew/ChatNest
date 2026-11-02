using BLL.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class User : ITimestampedEntity
    {
        public User()
        {
            UserName = string.Empty;
            Email = string.Empty;
            PasswordHash = string.Empty;
        }

        public User(int id, string userName, string email, string passwordHash, DateOnly dateOfBirth)
        {
            Id = id;
            UserName = userName;
            Email = email;
            PasswordHash = passwordHash;
            DateOfBirth = dateOfBirth;
        }

        public User(int id, string userName, string email, string passwordHash, DateOnly dateOfBirth, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            UserName = userName;
            Email = email;
            PasswordHash = passwordHash;
            DateOfBirth = dateOfBirth;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateOnly DateOfBirth { get; set; }

        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }
    }
}
