namespace VexTrainerWeb.Pages;

public class DonateModel : BasePage
{
    // Override to allow public access - no authentication required
    protected override bool RequiresAuthentication { get { return false; } }

    public void OnGet()
    {
        // Simple page, no data needed
        // Actual donations handled by PayPal or other third-party service
    }
}
