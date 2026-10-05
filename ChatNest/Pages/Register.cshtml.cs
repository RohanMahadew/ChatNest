using ChatNest.Models.DTO;
using ChatNest.Request.Authentication;
using ChatNest.Service;
using ChatNest.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ChatNest.Pages
{
    public class RegisterModel: PageModel
    {
        private readonly IAuthenticationInterface _authService;
        public RegisterModel(IAuthenticationInterface authenticationService)
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

            var result =
                await _authService.RegisterAsync(request);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        error.ToString(),
                        error);
                }

                return Page();
            }

            return LocalRedirect(
                returnUrl ?? Url.Content("~/")!);
        }
    }
}