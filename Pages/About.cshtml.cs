using VexTrainerWeb.Utilities;

namespace VexTrainerWeb.Pages;

public class AboutModel : BasePage
{
    private readonly IWebHostEnvironment _env;

    public AboutModel(IWebHostEnvironment env)
    {
        _env = env;
    }

    // Override to allow public access - no authentication required
    protected override bool RequiresAuthentication => false;

    public string  ContentHtml  { get; private set; } = "";
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        var (html, error) = await MarkdownPageRenderer.RenderAsync(_env, "about.md");
        ContentHtml  = html;
        ErrorMessage = error;
    }
}
