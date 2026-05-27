using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models;

public sealed class TokenRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("issue_refresh_token")]
    public bool? IssueRefreshToken { get; set; }

    [JsonPropertyName("exchange_refresh_token")]
    public bool? ExchangeRefreshToken { get; set; }
}

public sealed class TokenResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
