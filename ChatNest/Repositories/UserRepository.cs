using ChatNest.Service;
using Microsoft.Data.SqlClient;
using ChatNest.Models;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace ChatNest.Repositories
{
    public class UserRepository
    {

        public async Task<User?> FindUserByUsernameAsync(SqlConnection connection, string username)
        {
            const string query = "SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, Authtoken, DateOfBirth, CreatedAt, UpdatedAt FROM Users WHERE NormalizedUserName = @NormalizedUserName";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NormalizedUserName", username.ToUpper());

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

        public async Task<User?> FindUserByUsernameOrEmailAsync(SqlConnection connection, string username, string email)
        {
            const string query = "SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, AuthToken, DateOfBirth, CreatedAt, UpdatedAt FROM Users WHERE NormalizedUserName = @NormalizedUserName OR NormalizedEmail = @NormalizedEmail";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@NormalizedUserName", username.ToUpper());
                command.Parameters.AddWithValue("@NormalizedEmail", email.ToUpper());

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

        public async Task<bool> InsertUserAsync(SqlConnection connection, User user)
        {
            const string query = @"
                INSERT INTO Users 
                (UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, DateOfBirth, CreatedAt, UpdatedAt) 
                VALUES 
                (@UserName, @NormalizedUserName, @Email, @NormalizedEmail, @PasswordHash, @DateOfBirth, @CreatedAt, @UpdatedAt)";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserName", user.UserName);
                command.Parameters.AddWithValue("@NormalizedUserName", user.NormalizedUserName);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@NormalizedEmail", user.NormalizedEmail);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);
                command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
                command.Parameters.AddWithValue("@UpdatedAt", user.UpdatedAt);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }

        public async Task<User> ValidateToken(SqlConnection conn, int userId, string token)
        {
            const string query = @"SELECT Id,UserName,NormalizedUserName,Email,NormalizedEmail,PasswordHash,AuthToken,
            DateOfBirth,CreatedAt,UpdatedAt,AuthToken FROM Users WHERE Id = @Id AND AuthToken = @AuthToken";

            using (var command = new SqlCommand(query, conn))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@AuthToken", token);

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

        public async Task<bool> InsertAuthToken(SqlConnection connection, int userId, string authToken)
        {
            const string query = @"
                UPDATE Users 
                SET AuthToken = @AuthToken
                WHERE Id = @Id";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@AuthToken", authToken);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }

        //Map helper
        private User MapUserFromReader(SqlDataReader reader)
        {
            return new User(
                id: reader.GetInt32(0),
                userName: reader.GetString(1),
                normalizedUserName: reader.GetString(2),
                email: reader.GetString(3),
                normalizedEmail: reader.GetString(4),
                passwordHash: reader.GetString(5),
                authToken: reader.GetString(6),
                dateOfBirth: DateOnly.FromDateTime(reader.GetDateTime(7))
            )
            {
                CreatedAt = reader.GetDateTime(8),
                UpdatedAt = reader.GetDateTime(9)
            };
        }
    }
}
