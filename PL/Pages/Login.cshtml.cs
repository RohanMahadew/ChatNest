using BLL.Models.Requests;
using BLL.Services.Interfaces;
using ChatNest.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PL.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAuthenticationServiceInterface _authService;
        public LoginModel(IAuthenticationServiceInterface authenticationService)
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

            try
            {
                await _authService.LoginAsync(new AuthenticateUserRequest(
                    Input.Username,
                    Input.Password));

                return LocalRedirect(
                    returnUrl ?? Url.Content("~/")!);
            }
            catch (UnauthorizedAccessException)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return Page();
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, "Unable to sign in right now. Please try again.");
                return Page();
            }

        }
    }
}
