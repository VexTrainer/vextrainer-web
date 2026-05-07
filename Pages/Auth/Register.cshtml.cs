using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VexTrainer.Data.Models;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

public class RegisterModel : BasePage
{
    private readonly EmailService _emailService;
    private readonly ConfirmationTokenService _tokenService;

    public RegisterModel(
        AuthService authService, 
        LessonService lessonService,
        EmailService emailService,
        ConfirmationTokenService tokenService) 
        : base(authService, lessonService)
    {
        _emailService = emailService;
        _tokenService = tokenService;
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

    public void OnGet(string? email)
    {
        // Pre-fill email if coming from sign-in page
        if (!string.IsNullOrEmpty(email))
        {
            Email = email;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please fix the errors below";
            return Page();
        }

        // Validate password strength
        if (!IsStrongPassword(Password))
        {
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character (!@#$%^&*)";
            return Page();
        }

        try
        {
            // Register user (username = full name, phone = null)
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService!.RegisterAsync(
                new RegisterRequest
                {
                    UserName = FullName, // Store full name in username field
                    Email = Email,
                    Phone = null, // No phone field in form
                    Password = Password
                },
                ipAddress
            );

            if (!result.Success)
            {
                ErrorMessage = result.Message ?? "Registration failed. Please try again.";
                return Page();
            }

            // Generate email confirmation token (24-hour expiry)
            var token = _tokenService.GenerateEmailConfirmationToken(Email);

            // Send confirmation email
            await _emailService.SendConfirmationEmailAsync(Email, token);

            // Show success message
            SuccessMessage = "Registration successful! Please check your email to confirm your account. The confirmation link expires in 24 hours.";
            
            // Clear form
            FullName = "";
            Email = "";
            Password = "";
            ConfirmPassword = "";
            AgreeToTerms = false;
            
            return Page();
        }
        catch (Exception ex)
        {
            // Log detailed error for debugging
            var detailedError = $"Registration error: {ex.Message}";
            if (ex.InnerException != null)
            {
                detailedError += $" | Inner: {ex.InnerException.Message}";
            }
            detailedError += $" | Stack: {ex.StackTrace}";
            
            Console.WriteLine(detailedError);
            
            // Show generic error to user, but include exception type for debugging
            ErrorMessage = $"An error occurred during registration. Error type: {ex.GetType().Name}. Check console for details.";
            return Page();
        }
    }

    private bool IsStrongPassword(string password)
    {
        if (password.Length < 8) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(c => "!@#$%^&*".Contains(c))) return false;
        return true;
    }
}
