using ChatNest.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatNest.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly AuthService _authService;
        public bool isAuthenticated = false;

        public LogoutModel(AuthService authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _authService.SignOutUserAsync();

            return RedirectToPage("/Index");
        }
    }
}
