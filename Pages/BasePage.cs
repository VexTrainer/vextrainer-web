using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
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
            // Read UserId from the authentication cookie claims.
            // SignIn writes it as both ClaimTypes.NameIdentifier and a custom "UserId" claim;
            // we check both for resilience.
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User?.FindFirst("UserId")?.Value;

            if (int.TryParse(idClaim, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
