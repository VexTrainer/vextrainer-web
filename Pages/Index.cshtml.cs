using Microsoft.AspNetCore.Mvc;

namespace VexTrainerWeb.Pages;

public class IndexModel : BasePage
{
    public IndexModel() { }
  protected override bool RequiresAuthentication => false;
  public IActionResult OnGet()
    {
        // If already logged in, redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard");
        }

        return Page();
    }
}
