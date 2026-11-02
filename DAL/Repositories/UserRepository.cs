using BLL.Engines.Interfaces;
using BLL.Models;
using BLL.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Repositories
{
    public class UserRepository : IUserRepositoryInterface
    {
        private readonly ISqlConnectionInterface _SqlConnection;

        public UserRepository(ISqlConnectionInterface sqlConnection)
        {
            _SqlConnection = sqlConnection;
        }

        public async Task<User?> FindUserByUsername(string username)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "SELECT Id, UserName, Email, PasswordHash, DateOfBirth, CreatedAt, UpdatedAt FROM Users WHERE UserName = @UserName";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", username);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapUserFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public async Task<User?> FindUserByUsernameOrEmail(string username, string email)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = "SELECT Id, UserName, Email, PasswordHash, DateOfBirth, CreatedAt, UpdatedAt FROM Users WHERE UserName = @UserName OR Email = @Email";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", username);
                    command.Parameters.AddWithValue("@Email", email);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapUserFromReader(reader);
                        }
                    }
                }

                return null;
            }
        }

        public async Task<bool> Store(User user)
        {
            using (var connection = await _SqlConnection.GetConnectionAsync())
            {
                const string query = @"
                INSERT INTO Users 
                (UserName, Email, PasswordHash, DateOfBirth, CreatedAt, UpdatedAt) 
                VALUES 
                (@UserName, @Email, @PasswordHash, @DateOfBirth, @CreatedAt, @UpdatedAt)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", user.UserName);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);
                    command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
                    command.Parameters.AddWithValue("@UpdatedAt", user.UpdatedAt);

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        // Map helper
        private User MapUserFromReader(SqlDataReader reader)
        {
            return new User(
                id: (int)reader["Id"],
                userName: (string)reader["UserName"],
                email: (string)reader["Email"],
                passwordHash: (string)reader["PasswordHash"],
                dateOfBirth: DateOnly.FromDateTime((DateTime)reader["DateOfBirth"]),
                createdAt: (DateTime)reader["CreatedAt"],
                updatedAt: (DateTime)reader["UpdatedAt"]
            );
        }
    }
}
