using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Models;
using VexTrainer.Data.Services;
using VexTrainerWeb.Pages;

namespace VexTrainerWeb.Pages;

public class IndexModel : BasePage
{
    // Inject LessonService directly — Index uses the parameterless BasePage()
    // constructor since it does not need AuthService.
    private readonly LessonService _lessonService;

    public IndexModel(LessonService lessonService) : base()
    {
        _lessonService = lessonService;
    }

    protected override bool RequiresAuthentication => false;

    public SiteStats Stats { get; set; } = new()
    {
        // Fallback values shown while stats load or if the proc is unreachable.
        // Run sp_RefreshSiteStats in SSMS to seed t_site_stats with real data.
        TotalModules = 10,
        TotalLessons = 68,
        TotalTopics  = 654,
        Students     = 0,
        TopicsRead   = 0
    };

    public async Task<IActionResult> OnGetAsync()
    {
        // Authenticated users skip the home page and go straight to their dashboard
        if (IsAuthenticated)
            return RedirectToPage("/Dashboard");

        try
        {
            var result = await _lessonService.GetSiteStatsAsync();
            if (result.Success && result.Data != null)
                Stats = result.Data;
        }
        catch
        {
            // Non-fatal — page renders with fallback defaults above
        }

        return Page();
    }
}
