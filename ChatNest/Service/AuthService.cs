using ChatNest.Models;
using ChatNest.Request.Authentication;
using ChatNest.Service.Interface;
using ChatNest.Repositories;
using Microsoft.AspNetCore.Authentication;
using ChatNest.Repositories.Interface;

namespace ChatNest.Service
{
    public class AuthService : IAuthenticationInterface
    {
        private const string AuthTokenCookieName = "_authtoken";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthTokenInterface _tokenService;
        private readonly IUserRepositoryInterface _repository;

        public AuthService(
            IHttpContextAccessor httpContextAccessor,
            IAuthTokenInterface tokenService,
            IUserRepositoryInterface repository)
        {
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            _repository = repository;
        }

        public async Task<StoreUserResult> RegisterAsync(StoreUserRequest request)
        {
            // Check if user already exists
            var existingUser = await _repository.FindUserByUsernameOrEmailAsync(request.Username, request.Email);

            if (existingUser != null)
            {
                var errors = new List<string>();
                if (existingUser.UserName.ToUpper() == request.Username.ToUpper())
                    errors.Add("Username already exists.");
                if (existingUser.Email.ToUpper() == request.Email.ToUpper())
                    errors.Add("Email already in use.");
                return StoreUserResult.Failure(errors);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                UserName = request.Username,
                NormalizedUserName = request.Username.ToUpper(),
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper(),
                PasswordHash = hashedPassword,
                DateOfBirth = request.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var inserted = await _repository.InsertUserAsync(user);

            if (!inserted)
            {
                return StoreUserResult.Failure(new[] { "Failed to create user." });
            }

            // Retrieve the user from the database to get the generated Id
            var createdUser = await _repository.FindUserByUsernameAsync(request.Username);

            if (createdUser != null)
            {
                // Set custom auth cookie
                await _tokenService.SetAuthCookie(createdUser, _httpContextAccessor);
            }
                
            return StoreUserResult.Success();
        }

        public async Task<AuthenticateUserResult> LoginAsync(AuthenticateUserRequest request)
        {
            var user = await _repository.FindUserByUsernameAsync(request.Username);

            if (user == null)
            {
                return AuthenticateUserResult.Failure(LoginFailureReason.InvalidCredentials);
            }

            // Verify password
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isValidPassword) 
            {
                return AuthenticateUserResult.Failure(LoginFailureReason.InvalidCredentials);
            }

            // Set custom auth cookie
            await _tokenService.SetAuthCookie(user, _httpContextAccessor);

            return AuthenticateUserResult.Success();
        }

        public Task SignOutUserAsync()
        {
            // Clear the auth cookie
            _httpContextAccessor.HttpContext!.Response.Cookies.Delete(AuthTokenCookieName);
            return Task.CompletedTask;
        }

        //private async Task SetAuthCookie(User user)
        //{
        //    using (var connection = await _connectionService.GetConnectionAsync())
        //    {
        //        var token = _tokenService.GenerateToken(user.Id, user.UserName, user.Email);
        //        var cookieOptions = _tokenService.GetCookieOptions();
        //        _httpContextAccessor.HttpContext!.Response.Cookies.Append(AuthTokenCookieName, token, cookieOptions);
        //        var result = await _repository.InsertAuthToken(connection, user.Id, token);
        //    }
        //}
    }
}
