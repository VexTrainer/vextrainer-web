using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VexTrainerWeb.Services;
using System.Text.Json;

namespace VexTrainerWeb.Pages.Auth;

[IgnoreAntiforgeryToken]  // ← ADD THIS LINE!
public class ForgotPasswordModel : PageModel
{
    private readonly EmailService _emailService;
    private readonly ConfirmationTokenService _tokenService;

    public ForgotPasswordModel(
        EmailService emailService,
        ConfirmationTokenService tokenService)
    {
        _emailService = emailService;
        _tokenService = tokenService;
    }

    // POST handler for AJAX requests
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Read JSON body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ForgotPasswordRequest>(body, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (request == null || string.IsNullOrEmpty(request.Email))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Email is required"
                });
            }

            // Generate password reset token (1-hour expiry)
            var token = _tokenService.GeneratePasswordResetToken(request.Email);

            // Send password reset email
            await _emailService.SendPasswordResetEmailAsync(request.Email, token);

            // Return success (always return success even if email doesn't exist - security)
            return new JsonResult(new
            {
                success = true,
                message = "If an account exists with this email, a password reset link has been sent."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Forgot password error: {ex.Message}");
            
            // Don't reveal if email exists or if sending failed
            return new JsonResult(new
            {
                success = true,
                message = "If an account exists with this email, a password reset link has been sent."
            });
        }
    }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = "";
}
