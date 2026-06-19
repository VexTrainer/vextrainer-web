using Microsoft.AspNetCore.Mvc;

namespace VexTrainerWeb.Pages;

public class DonateModel : BasePage
{
    private readonly IWebHostEnvironment _env;

    // Public page — no authentication required
    protected override bool RequiresAuthentication => false;

    public string DonateContent { get; private set; } = "";

    public DonateModel(IWebHostEnvironment env) : base()
    {
        _env = env;
    }

    public async Task OnGetAsync()
    {
        var path = Path.Combine(_env.WebRootPath, "content", "donate.html");
        if (System.IO.File.Exists(path))
            DonateContent = await System.IO.File.ReadAllTextAsync(path);
    }
}
