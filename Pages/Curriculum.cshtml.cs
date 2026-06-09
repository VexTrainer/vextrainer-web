using Microsoft.AspNetCore.Mvc;

namespace VexTrainerWeb.Pages;

public class CurriculumModel : BasePage
{
    private readonly IWebHostEnvironment _env;

    protected override bool RequiresAuthentication => false;

    public string TocHtml { get; private set; } = "<p>Curriculum content not available.</p>";

    public CurriculumModel(IWebHostEnvironment env) : base()
    {
        _env = env;
    }

    public async Task OnGetAsync()
    {
        var path = Path.Combine(_env.WebRootPath, "content", "curriculum.html");
        if (System.IO.File.Exists(path))
            TocHtml = await System.IO.File.ReadAllTextAsync(path);
    }
}
