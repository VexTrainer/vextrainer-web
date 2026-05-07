using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Services;

namespace VexTrainerWeb.Pages;

[Authorize] // Require authentication to access this page
public class DashboardModel : BasePage
{
    public DashboardModel(AuthService authService, LessonService lessonService) 
        : base(authService, lessonService)
    {
    }
  protected override bool RequiresAuthentication => true;
  public void OnGet()
    {
        // Dashboard loads - can add logic here to:
        // - Fetch user's progress from database
        // - Get recently viewed lessons
        // - Calculate statistics
        // For now, just display the page
    }
}
