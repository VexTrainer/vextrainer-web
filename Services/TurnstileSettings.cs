using VexTrainer.Data.Services;

namespace VexTrainerWeb.Services;

/// <summary>
/// Public Turnstile settings for the website (the site key is not a secret;
/// it is embedded in the page HTML). Registered as a singleton in Program.cs.
/// </summary>
public sealed class TurnstileSettings {
  public TurnstileSettings(string? siteKey) {
    SiteKey = string.IsNullOrWhiteSpace(siteKey) ? null : siteKey.Trim();
  }

  public string? SiteKey { get; }

  /// <summary>
  /// The widget is shown and required only when both the site key and the
  /// secret key are configured; otherwise forms behave as before.
  /// </summary>
  public bool IsActive(TurnstileService service) => SiteKey is not null && service.IsEnabled;
}
