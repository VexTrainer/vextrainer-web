using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

public class ResetPasswordModel : BasePage
{
    private readonly ConfirmationTokenService _tokenService;
    private readonly ApiClient               _apiClient;

    public ResetPasswordModel(
        AuthService               authService,
        LessonService             lessonService,
        ConfirmationTokenService  tokenService,
        ApiClient                 apiClient)
        : base(authService, lessonService)
    {
        _tokenService = tokenService;
        _apiClient    = apiClient;
    }

    protected override bool RequiresAuthentication => false;

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "Please confirm your password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = "";

    public bool   IsTokenValid { get; set; }
    public string ErrorMessage { get; set; } = "";

    public void OnGet(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            IsTokenValid = false;
            ErrorMessage = "Invalid reset link. Please request a new password reset.";
            return;
        }

        Token = token;

        // Validate token locally just to decide whether to show the form.
        // The actual validation + DB update happens on POST via the API.
        var (isValid, _) = _tokenService.ValidateToken(token);

        if (!isValid)
        {
            IsTokenValid = false;
            ErrorMessage = "This reset link has expired or is invalid. Please request a new password reset.";
            return;
        }

        IsTokenValid = true;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            IsTokenValid = true;
            ErrorMessage = "Please fix the errors below.";
            return Page();
        }

        // Delegate token validation + password hash + DB update to the API
        var result = await _apiClient.PostAsync<object>(
            "Auth/reset-password",
            new { token = Token, newPassword = NewPassword }
        );

        if (result?.Success == true)
        {
            TempData["SuccessMessage"] =
                "Password reset successful! You can now sign in with your new password.";
            return RedirectToPage("/Auth/SignIn");
        }

        IsTokenValid = true;
        ErrorMessage = result?.Message
                    ?? "Failed to reset password. Please try again or contact support.";
        return Page();
    }
}
