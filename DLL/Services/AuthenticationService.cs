using BLL.Models;
using BLL.Models.DTOs;
using BLL.Models.Requests;
using BLL.Repositories.Interfaces;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace BLL.Services
{
    public class AuthenticationService : IAuthenticationServiceInterface
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepositoryInterface _repository;

        public AuthenticationService(
            IHttpContextAccessor httpContextAccessor,
            IUserRepositoryInterface repository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = repository;
        }

        public async Task<UserDTO> RegisterAsync(StoreUserRequest request)
        {
            // Check if user already exists
            User? existingUser = await _repository.FindUserByUsernameOrEmail(request.UserName, request.Email);

            if (existingUser is not null)
            {
                var errors = new List<string>();
                if (string.Equals(existingUser.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
                    errors.Add("Username already exists.");
                if (string.Equals(existingUser.Email, request.Email, StringComparison.OrdinalIgnoreCase))
                    errors.Add("Email already in use.");
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var now = DateTime.UtcNow;
            var user = new User(0, request.UserName, request.Email, hashedPassword, request.DateOfBirth, now, now);

            var inserted = await _repository.Store(user);

            if (!inserted)
            {
                throw new InvalidOperationException("Failed to create user.");
            }

            // Retrieve the user from the database to get the generated Id
            User? createdUser = await _repository.FindUserByUsername(request.UserName);

            if (createdUser is null)
            {
                throw new InvalidOperationException("User was created but could not be retrieved.");
            }

            await SignInAsync(createdUser);
            return UserDTOExtensions.fromUser(createdUser);
        }

        public async Task<UserDTO> LoginAsync(AuthenticateUserRequest request)
        {
            User? user = await _repository.FindUserByUsername(request.UserName);

            if (user is null)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Verify password
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            await SignInAsync(user);

            return UserDTOExtensions.fromUser(user);
        }

        public async Task<bool> SignOutUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No active HTTP context is available.");
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return true;
        }

        private async Task SignInAsync(User user)
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No active HTTP context is available.");
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                createClaims(user.Id.ToString(), user.UserName, user.Email));
        }

        private ClaimsPrincipal createClaims(string userId, string username, string email)
        {
            var claims = new List<Claim> {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            return principal;
        }
    }
}
