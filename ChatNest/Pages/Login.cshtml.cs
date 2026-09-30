using ChatNest.Models;
using ChatNest.Models.DTO;
using ChatNest.Request.Authentication;
using ChatNest.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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

        //public class LoginModel : PageModel
        //{
        //    private readonly SignInManager<ApplicationUser> _signInManager;

        //    public LoginModel(SignInManager<ApplicationUser> signInManager)
        //    {
        //        _signInManager = signInManager;
        //    }

        //    [BindProperty]
        //    public InputModel Input { get; set; } = new();

        //    public string? ReturnUrl { get; set; }

        //    public class InputModel
        //    {
        //        [Required]
        //        [Display(Name = "Username")]
        //        public string Username { get; set; } = string.Empty;

        //        [Required]
        //        [DataType(DataType.Password)]
        //        [Display(Name = "Password")]
        //        public string Password { get; set; } = string.Empty;
        //    }

        //    public void OnGet(string? returnUrl = null)
        //    {
        //        ReturnUrl = returnUrl;
        //    }

        //    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        //    {
        //        returnUrl ??= Url.Content("~/");

        //        if (!ModelState.IsValid)
        //        {
        //            return Page();
        //        }

        //        var result = await _signInManager.PasswordSignInAsync(
        //            Input.Username,
        //            Input.Password,
        //            isPersistent: false,
        //            lockoutOnFailure: false);

        //        if (result.Succeeded)
        //        {
        //            return LocalRedirect(returnUrl);
        //        }

        //        if (result.IsLockedOut)
        //        {
        //            ModelState.AddModelError(
        //                string.Empty,
        //                "This account has been locked out.");

        //            return Page();
        //        }

        //        if (result.IsNotAllowed)
        //        {
        //            ModelState.AddModelError(
        //                string.Empty,
        //                "You are not allowed to sign in.");

        //            return Page();
        //        }

        //        ModelState.AddModelError(
        //            string.Empty,
        //            "Invalid email or password.");

        //        return Page();
        //    }
        //}
    }
}
