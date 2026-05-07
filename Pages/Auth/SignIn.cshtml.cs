using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VexTrainer.Data.Models;
using VexTrainer.Data.Services;

namespace VexTrainerWeb.Pages.Auth;

public class SignInModel : BasePage
{
    public SignInModel(AuthService authService, LessonService lessonService) 
        : base(authService, lessonService)
    {
    }

    protected override bool RequiresAuthentication => false;

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string ErrorMessage { get; set; } = "";

    public IActionResult OnGet()
    {
        // If already authenticated, redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Email and password are required";
            return Page();
        }

        try
        {
            // Call AuthService to authenticate
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService!.LoginAsync(
                new LoginRequest 
                { 
                    Identifier = Email, 
                    Password = Password 
                },
                ipAddress
            );

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.Message ?? "Login failed. Please try again.";
                return Page();
            }

            // Check if user is active (email confirmed)
            // Note: You'll need to check is_active in the login stored proc
            // For now, assuming it's checked in AuthService

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.Data.UserId.ToString()),
                new Claim(ClaimTypes.Name, result.Data.UserName),
                new Claim(ClaimTypes.Email, Email),
                new Claim("UserId", result.Data.UserId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false, // Session cookie - expires when browser closes
                AllowRefresh = true
            };

            // Sign in the user
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // Redirect to dashboard
            return RedirectToPage("/Dashboard");
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred during sign in. Please try again.";
            // Log the exception
            Console.WriteLine($"Sign in error: {ex.Message}");
            return Page();
        }
    }
}
