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

    private static readonly JsonSerializerOptions JsonOpts = new() {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http, ILogger<ApiClient> logger) {
        _http   = http;
        _logger = logger;
    }

    /// <summary>POST body, return typed ApiResponse.</summary>
    public async Task<ApiResponse<T>?> PostAsync<T>(string path, object body) {
        try {
            var response = await _http.PostAsJsonAsync(path, body);
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
