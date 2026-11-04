using BLL.Models.Requests;
using BLL.Services.Interfaces;
using ChatNest.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PL.Pages
{
    public class RegisterModel: PageModel
    {
        private readonly IAuthenticationServiceInterface _authService;
        public RegisterModel(IAuthenticationServiceInterface authenticationService)
        {
            _authService = authenticationService;
        }

        [BindProperty]
        public RegisterInputDTO Input { get; set; } = new();

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

            var request = new StoreUserRequest(
                Input.Email,
                Input.Username,
                Input.DateOfBirth!.Value,
                Input.Password);

            try
            {
                await _authService.RegisterAsync(request);
                return LocalRedirect(
                    returnUrl ?? Url.Content("~/")!);
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
                return Page();
            }
        }
    }
}