using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

public class RegisterModel : BasePage {
  private readonly ApiClient _apiClient;
  private readonly TurnstileService _turnstile;
  private readonly TurnstileSettings _turnstileSettings;

  public RegisterModel(
      AuthService authService,
      LessonService lessonService,
      ApiClient apiClient,
      TurnstileService turnstile,
      TurnstileSettings turnstileSettings)
      : base(authService, lessonService) {
    _apiClient = apiClient;
    _turnstile = turnstile;
    _turnstileSettings = turnstileSettings;
  }

  protected override bool RequiresAuthentication => false;

  [BindProperty]
  [Required(ErrorMessage = "Full name is required")]
  [MinLength(2, ErrorMessage = "Please enter your full name")]
  public string FullName { get; set; } = "";

  [BindProperty]
  [Required(ErrorMessage = "Email is required")]
  [EmailAddress(ErrorMessage = "Please enter a valid email address")]
  public string Email { get; set; } = "";

  [BindProperty]
  [Required(ErrorMessage = "Password is required")]
  [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
  public string Password { get; set; } = "";

  [BindProperty]
  [Required(ErrorMessage = "Please confirm your password")]
  [Compare("Password", ErrorMessage = "Passwords do not match")]
  public string ConfirmPassword { get; set; } = "";

  [BindProperty]
  [Required(ErrorMessage = "You must agree to the terms")]
  public bool AgreeToTerms { get; set; }

  public string ErrorMessage { get; set; } = "";

  public void OnGet(string? email) {
    if (TempData[RegisteredEmailKey] is string registeredEmail) {
      Registered = true;
      RegisteredEmail = registeredEmail;
      return;
    }
    if (!string.IsNullOrEmpty(email)) Email = email;
  }

  public async Task<IActionResult> OnPostAsync() {
    if (!ModelState.IsValid) {
      ErrorMessage = "Please fix the errors below";
      return Page();
    }

    if (!IsStrongPassword(Password)) {
      ErrorMessage = "Password must contain at least one uppercase letter, " +
                     "one lowercase letter, one number, and one special character (!@#$%^&*)";
      return Page();
    }

    // CAPTCHA: the website always requires the Turnstile token (when configured)
    // and forwards it to the API, which verifies it with Cloudflare. A token
    // can only be verified once, so it is not verified here as well.
    string? captchaToken = null;
    if (_turnstileSettings.IsActive(_turnstile)) {
      captchaToken = Request.Form[TurnstileService.FormFieldName].ToString();
      if (string.IsNullOrWhiteSpace(captchaToken)) {
        ErrorMessage = "Please complete the security check.";
        return Page();
      }
    }

    // Delegate registration + token generation + confirmation email to the API
    var result = await _apiClient.PostAsync<object>(
        "Auth/register",
        new {
          userName = FullName,
          email = Email,
          phone = (string?)null,
          password = Password,
          captchaToken
        }
    );

    if (result?.Success != true) {
      ErrorMessage = result?.Message ?? "Registration failed. Please try again.";
      return Page();
    }

    // Post/Redirect/Get: show the confirmation on a fresh GET so the form (and
    // the Turnstile widget) is not rendered again, posted values are not
    // re-filled, and a browser refresh cannot resubmit the registration.
    TempData[RegisteredEmailKey] = Email.Trim();
    return RedirectToPage();
  }

  private const string RegisteredEmailKey = "RegisteredEmail";

  /// <summary>True on the GET that follows a successful registration.</summary>
  public bool Registered { get; private set; }
  public string RegisteredEmail { get; private set; } = "";

  private static bool IsStrongPassword(string password) {
    if (password.Length < 8) return false;
    if (!password.Any(char.IsUpper)) return false;
    if (!password.Any(char.IsLower)) return false;
    if (!password.Any(char.IsDigit)) return false;
    if (!password.Any(c => "!@#$%^&*".Contains(c))) return false;
    return true;
  }
}