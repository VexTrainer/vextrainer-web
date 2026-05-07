using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VexTrainerWeb.Pages.Auth;

public class SignOutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        // Sign out the user
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect to home page
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Also support GET for direct navigation
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
