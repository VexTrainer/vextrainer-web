using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

public class DeleteAccountModel : BasePage
{
    private readonly ApiClient                  _apiClient;
    private readonly ILogger<DeleteAccountModel> _logger;

    protected override bool RequiresAuthentication => false;

    public DeleteAccountModel(
        AuthService              authService,
        LessonService            lessonService,
        ApiClient                apiClient,
        ILogger<DeleteAccountModel> logger)
        : base(authService, lessonService)
    {
        _apiClient = apiClient;
        _logger    = logger;
    }

    public IActionResult OnGet() => Page();

    /// <summary>
    /// Step 1 — user submits email.
    /// API generates token, stores in DB, sends deletion email.
    /// </summary>
    public async Task<IActionResult> OnPostRequestAsync(
        [FromBody] DeleteAccountRequestInput input)
    {
        const string genericMessage =
            "If an account with that email exists, a deletion link has been sent. Check your inbox.";

        _logger.LogInformation("[DeleteAccount] Request for {Email}", input?.Email ?? "(null)");

        if (string.IsNullOrWhiteSpace(input?.Email))
            return new JsonResult(new { success = false, message = "Email is required." });

        var result = await _apiClient.PostAsync<object>(
            "Auth/delete-account/request",
            new { email = input.Email.Trim() }
        );

        // Always return generic message regardless of result
        return new JsonResult(new { success = true, message = genericMessage });
    }

    /// <summary>
    /// Step 2 — user clicks link in email.
    /// API validates token, anonymises account, sends goodbye email.
    /// </summary>
    public async Task<IActionResult> OnPostConfirmAsync(
        [FromBody] DeleteAccountConfirmInput input)
    {
        _logger.LogInformation("[DeleteAccount] Confirm token={Prefix}",
            input?.Token?.Length > 10 ? input.Token[..10] + "..." : input?.Token ?? "(null)");

        if (string.IsNullOrWhiteSpace(input?.Token))
            return new JsonResult(new { success = false, message = "Invalid or missing token." });

        var result = await _apiClient.PostAsync<object>(
            "Auth/delete-account/confirm",
            new { token = input.Token }
        );

        if (result?.Success == true)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return new JsonResult(new
        {
            success = result?.Success ?? false,
            message = result?.Message ?? "An error occurred. Please try again."
        });
    }
}

public class DeleteAccountRequestInput { public string? Email { get; set; } }
public class DeleteAccountConfirmInput { public string? Token { get; set; } }
