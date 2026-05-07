using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VexTrainer.Data.Services;

namespace VexTrainerWeb.Pages;

public class BasePage : PageModel
{
    protected readonly AuthService? _authService;
    protected readonly LessonService? _lessonService;

    // Virtual property - override in pages that don't need authentication
    protected virtual bool RequiresAuthentication { get { return true; } }

    public BasePage()
    {
        // Parameterless constructor for pages that don't need services
    }

    public BasePage(AuthService authService, LessonService lessonService)
    {
        _authService = authService;
        _lessonService = lessonService;
    }

    // Check authentication before page handler executes
    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        // If page doesn't require authentication, allow access
        if (!RequiresAuthentication)
        {
            base.OnPageHandlerExecuting(context);
            return;
        }

        // Page requires authentication - check if user is logged in
        if (!IsAuthenticated)
        {
            context.Result = RedirectToPage("/Auth/SignIn");
            return;
        }

        base.OnPageHandlerExecuting(context);
    }

    protected bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    protected int CurrentUserId
    {
        get
        {
            if (HttpContext?.Session.GetInt32("UserId") is int userId)
            {
                return userId;
            }
            return 0;
        }
    }
}
