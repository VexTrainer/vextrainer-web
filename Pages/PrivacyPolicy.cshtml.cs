using VexTrainerWeb.Utilities;

namespace VexTrainerWeb.Pages;

public class PrivacyPolicyModel : BasePage
{
    private readonly IWebHostEnvironment _env;

    public PrivacyPolicyModel(IWebHostEnvironment env)
    {
        _env = env;
    }

    // Override to allow public access - no authentication required
    protected override bool RequiresAuthentication => false;

    public string  ContentHtml  { get; private set; } = "";
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        var (html, error) = await MarkdownPageRenderer.RenderAsync(_env, "privacy.md");
        ContentHtml  = html;
        ErrorMessage = error;
    }
}
