using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

public class ResetPasswordModel : BasePage {
  private readonly ApiClient _apiClient;

  public ResetPasswordModel(
      AuthService authService,
      LessonService lessonService,
      ApiClient apiClient)
      : base(authService, lessonService) {
    _apiClient = apiClient;
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

  public bool IsTokenValid { get; set; }
  public string ErrorMessage { get; set; } = "";

  public void OnGet(string? token) {
    // Don't validate the token here — it would require either calling the API
    // for a pre-check (extra round-trip) or duplicating crypto in the web layer.
    // Just show the form if a token is present; the API will validate on POST.
    if (string.IsNullOrEmpty(token)) {
      IsTokenValid = false;
      ErrorMessage = "Invalid reset link. Please request a new password reset.";
      return;
    }

    Token = token;
    IsTokenValid = true;
  }

  public async Task<IActionResult> OnPostAsync() {
    if (!ModelState.IsValid) {
      IsTokenValid = true;
      ErrorMessage = "Please fix the errors below.";
      return Page();
    }

        // Delegate token validation + password hash + DB update to the API
    var result = await _apiClient.PostAsync<object>(
        "Auth/reset-password",
        new { token = Token, newPassword = NewPassword }
    );

    if (result?.Success == true) {
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