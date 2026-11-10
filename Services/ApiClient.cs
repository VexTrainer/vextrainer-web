using System.Net.Http.Json;
using System.Text.Json;
using VexTrainer.Data.Models;

namespace VexTrainerWeb.Services;

/// <summary>
/// Thin wrapper around HttpClient for calling the VexTrainer API.
/// Injected as a scoped service so each request gets its own instance.
/// Base URL configured via Site:ApiUrl in appsettings.json.
/// </summary>
public class ApiClient {
    private readonly HttpClient    _http;
    private readonly ILogger<ApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly JsonSerializerOptions JsonOpts = new() {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http, ILogger<ApiClient> logger, IHttpContextAccessor httpContextAccessor) {
        _http   = http;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>POST body, return typed ApiResponse.</summary>
    public async Task<ApiResponse<T>?> PostAsync<T>(string path, object body) {
        try {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) {
                Content = JsonContent.Create(body)
            };

            // Pass the visitor's IP so the API rate-limits per visitor, not per
            // web server. Only the connection IP is used (never an incoming
            // X-Forwarded-For), and the API trusts this header only from IPs in
            // its ForwardedHeaders:KnownProxies setting.
            var visitorIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
            if (visitorIp != null)
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", visitorIp.ToString());

            using var response = await _http.SendAsync(request);
            var json     = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("API {Path} => {Status}: {Body}", path, (int)response.StatusCode, json);
            return JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOpts);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "ApiClient.PostAsync failed for {Path}", path);
            return null;
        }
    }
}
