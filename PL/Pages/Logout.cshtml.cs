using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PL.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly IAuthenticationServiceInterface _authService;

        public LogoutModel(IAuthenticationServiceInterface authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                await _authService.SignOutUserAsync();
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return RedirectToPage("/Index");
        }
    }
}
