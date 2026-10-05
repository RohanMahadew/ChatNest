using ChatNest.Service;
using ChatNest.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatNest.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IAuthTokenInterface _authTokenService;
        public bool isAuthenticated = false;

        public IndexModel(IAuthTokenInterface authTokenService)
        {
            _authTokenService = authTokenService;
        }


        public async Task OnGet()
        {
            isAuthenticated = await _authTokenService.IsAuthenticated(Request);
        }
    }
}
