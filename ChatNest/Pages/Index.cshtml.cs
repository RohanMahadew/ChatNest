using ChatNest.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatNest.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuthTokenService _authTokenService;
        public bool isAuthenticated = false;

        public IndexModel(AuthTokenService authTokenService)
        {
            _authTokenService = authTokenService;
        }


        public async Task OnGet()
        {
            isAuthenticated = await _authTokenService.IsAuthenticated(Request);
        }
    }
}
