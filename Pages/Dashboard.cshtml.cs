using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Models;
using VexTrainer.Data.Services;

namespace VexTrainerWeb.Pages;

public class DashboardModel : BasePage
{
    public DashboardModel(AuthService authService, LessonService lessonService)
        : base(authService, lessonService)
    {
    }

    // BasePage already redirects unauthenticated users to /Auth/SignIn via
    // OnPageHandlerExecuting + RequiresAuthentication. No [Authorize] attribute needed.
    protected override bool RequiresAuthentication => true;

    public UserWebDashboard? Dashboard    { get; set; }
    public string?           ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var result = await _lessonService!.GetUserWebDashboardAsync(CurrentUserId);
            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.Message;
                return Page();
            }
            Dashboard = result.Data;
            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading dashboard: {ex.Message}");
            ErrorMessage = "An error occurred while loading the dashboard.";
            return Page();
        }
    }
}
