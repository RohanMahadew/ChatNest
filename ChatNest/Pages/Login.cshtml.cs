using ChatNest.Models.DTO;
using ChatNest.Request.Authentication;
using ChatNest.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ChatNest.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;
        public LoginModel(AuthService authenticationService)
        {
            _authService = authenticationService;
        }
        [BindProperty]
        public LoginInputDTO Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new AuthenticateUserRequest(
                Input.Username,
                Input.Password);

            var result =
                await _authService.LoginAsync(request);

            if (result.Succeeded)
            {
                return LocalRedirect(
                    returnUrl ?? Url.Content("~/")!);
            }

            var errorMessage = result.FailureReason switch
            {
                LoginFailureReason.LockedOut =>
                    "This account has been locked out.",

                LoginFailureReason.NotAllowed =>
                    "You are not allowed to sign in.",

                _ =>
                    "Invalid email or password."
            };

            ModelState.AddModelError(
                string.Empty,
                errorMessage);

            return Page();
        }
    }
}
