using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using VexTrainerWeb.Services;

namespace VexTrainerWeb.Pages.Auth;

[IgnoreAntiforgeryToken]
public class ForgotPasswordModel : PageModel {
  private readonly ApiClient _apiClient;

  public ForgotPasswordModel(ApiClient apiClient) {
    _apiClient = apiClient;
  }

  public async Task<IActionResult> OnPostAsync() {
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync();
    var request = JsonSerializer.Deserialize<ForgotPasswordRequest>(
        body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (request == null || string.IsNullOrEmpty(request.Email)) {
      return new JsonResult(new { success = false, message = "Email is required" });
    }

    // Delegate token generation + email send to the API.
    // The API already implements the anti-enumeration response, so we just forward it.
    var result = await _apiClient.PostAsync<object>(
        "Auth/forgot-password",
        new { email = request.Email }
    );

    return new JsonResult(new {
      success = true,
      message = result?.Message
             ?? "If an account exists with this email, a password reset link has been sent."
    });
  }
}

public class ForgotPasswordRequest {
  public string Email { get; set; } = "";
}