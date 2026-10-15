using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatNest.Pages
{
    [Authorize] 
    public class TestModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
