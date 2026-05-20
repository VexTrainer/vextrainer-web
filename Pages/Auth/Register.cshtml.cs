using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

public class RegisterModel : BasePage {
  private readonly ApiClient _apiClient;

  public RegisterModel(
      AuthService authService,
      LessonService lessonService,
      ApiClient apiClient)
      : base(authService, lessonService) {
    _apiClient = apiClient;
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
  public string SuccessMessage { get; set; } = "";

  public void OnGet(string? email) {
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

    // Delegate registration + token generation + confirmation email to the API
    var result = await _apiClient.PostAsync<object>(
        "Auth/register",
        new {
          userName = FullName,
          email = Email,
          phone = (string?)null,
          password = Password
        }
    );

    if (result?.Success != true) {
      ErrorMessage = result?.Message ?? "Registration failed. Please try again.";
      return Page();
    }

    SuccessMessage = "Registration successful! Please check your email to confirm your account. " +
                     "The confirmation link expires in 24 hours.";

    // Clear form
    FullName = "";
    Email = "";
    Password = "";
    ConfirmPassword = "";
    AgreeToTerms = false;

    return Page();
  }

  private static bool IsStrongPassword(string password) {
    if (password.Length < 8) return false;
    if (!password.Any(char.IsUpper)) return false;
    if (!password.Any(char.IsLower)) return false;
    if (!password.Any(char.IsDigit)) return false;
    if (!password.Any(c => "!@#$%^&*".Contains(c))) return false;
    return true;
  }
}