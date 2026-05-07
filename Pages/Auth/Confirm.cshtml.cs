using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

public class ConfirmModel : BasePage
{
    private readonly ApiClient _apiClient;

    public ConfirmModel(
        AuthService   authService,
        LessonService lessonService,
        ApiClient     apiClient)
        : base(authService, lessonService)
    {
        _apiClient = apiClient;
    }

    protected override bool RequiresAuthentication => false;

    public bool   IsSuccess    { get; set; }
    public bool   IsExpired    { get; set; }
    public string ErrorMessage { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            IsSuccess    = false;
            ErrorMessage = "Invalid confirmation link. Please check your email and try again.";
            return Page();
        }

        // Delegate token validation + DB activation to the API
        var result = await _apiClient.PostAsync<object>(
            "Auth/confirm-email",
            new { token }
        );

        if (result?.Success == true)
        {
            IsSuccess = true;
            return Page();
        }

        IsSuccess    = false;
        IsExpired    = true;
        ErrorMessage = result?.Message
                    ?? "This confirmation link has expired or is invalid.";
        return Page();
    }
}
