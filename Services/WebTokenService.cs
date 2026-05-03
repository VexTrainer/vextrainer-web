using VexTrainer.Data.Services;

namespace VexTrainerWeb.Services;

/// <summary>
/// Simple token service for web (doesn't generate JWT, just placeholder)
/// Web uses cookie authentication instead of JWT tokens
/// </summary>
public class WebTokenService : ITokenService
{
  public (string token, DateTime expiryDate) GenerateAccessToken(
  int userId, string userName, string roleName, string email = "") {
    // Web doesn't use JWT tokens (uses cookies instead)
    return (Guid.NewGuid().ToString(), DateTime.UtcNow.AddHours(6));
  }

  public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}
