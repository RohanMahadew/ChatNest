using ChatNest.Service;
using ChatNest.Service.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatNest.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly IAuthenticationInterface _authService;
        public bool isAuthenticated = false;

        public LogoutModel(IAuthenticationInterface authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //await _authService.SignOutUserAsync();
            HttpContext httpContext = new DefaultHttpContext();
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Index");
        }
    }
}
