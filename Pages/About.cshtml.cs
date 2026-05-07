namespace VexTrainerWeb.Pages;

public class AboutModel : BasePage
{
    // Override to allow public access - no authentication required
    protected override bool RequiresAuthentication { get { return false; } }

    public void OnGet()
    {
        // Simple page, no data needed
    }
}
