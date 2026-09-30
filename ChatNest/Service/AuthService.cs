using ChatNest.Models;
using ChatNest.Request.Authentication;
using ChatNest.Service.Interface;
using Microsoft.AspNetCore.Identity;

namespace ChatNest.Service
{
    public class AuthService :IAuthenticationInterface
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        public async Task<StoreUserResult> RegisterAsync(StoreUserRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                DateOfBirth = request.DateOfBirth
            };

            var result = await userManager.CreateAsync(
                user,
                request.Password);

            if (!result.Succeeded)
            {
                return StoreUserResult.Failure(
                    result.Errors.Select(error => error.Description));
            }

            await signInManager.SignInAsync(
                user,
                isPersistent: false);

            return StoreUserResult.Success();
        }

        public async Task<AuthenticateUserResult> LoginAsync(AuthenticateUserRequest request)
        {
            var result = await signInManager.PasswordSignInAsync(
                request.Username,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return AuthenticateUserResult.Success();
            }

            if (result.IsLockedOut)
            {
                return AuthenticateUserResult.Failure(
                    LoginFailureReason.LockedOut);
            }

            if (result.IsNotAllowed)
            {
                return AuthenticateUserResult.Failure(
                    LoginFailureReason.NotAllowed);
            }

            return AuthenticateUserResult.Failure(
                LoginFailureReason.InvalidCredentials);
        }
    }
}
