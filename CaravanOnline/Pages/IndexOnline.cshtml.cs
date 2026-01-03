using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaravanOnline.Pages
{
    public class IndexOnlineModel : PageModel
    {
        public string Message { get; set; } = "Welcome to Caravan Online!";

        public void OnGet()
        {
        }
    }
}
