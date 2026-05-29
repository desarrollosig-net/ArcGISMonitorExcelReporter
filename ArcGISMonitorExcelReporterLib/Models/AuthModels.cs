using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models;

/// <summary>
/// Represents an authentication request to ArcGIS Monitor.
/// Used to obtain an access token for subsequent API calls.
/// </summary>
/// <remarks>
/// <para>
/// This request is sent to the authentication endpoint (<c>/arcgis/auth/token</c>) to obtain
/// a bearer token required for accessing protected ArcGIS Monitor resources.
/// </para>
/// <para>
/// The request supports both initial authentication (using username/password) and
/// token refresh scenarios (using an existing refresh token).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var request = new TokenRequest
/// {
///     Username = "admin",
///     Password = "securePassword123",
///     IssueRefreshToken = true
/// };
/// </code>
/// </example>
public sealed class TokenRequest
{
    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    /// <remarks>
    /// Required for initial authentication. This should be a valid ArcGIS Monitor user account.
    /// </remarks>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    /// <remarks>
    /// Required for initial authentication. For security, passwords should be stored encrypted
    /// or in secure configuration (e.g., Base64 encoded or in Azure Key Vault).
    /// </remarks>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token for token renewal.
    /// </summary>
    /// <remarks>
    /// Optional. Used when refreshing an existing token instead of providing username/password.
    /// If provided, <see cref="ExchangeRefreshToken"/> should be set to <c>true</c>.
    /// </remarks>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to issue a refresh token in the response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional. When <c>true</c>, the server will include a refresh token in the response
    /// that can be used to renew the access token without re-entering credentials.
    /// </para>
    /// <para>
    /// Refresh tokens typically have a longer lifetime than access tokens and can be used
    /// for extended authentication sessions.
    /// </para>
    /// </remarks>
    [JsonPropertyName("issue_refresh_token")]
    public bool? IssueRefreshToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to exchange a refresh token for a new access token.
    /// </summary>
    /// <remarks>
    /// Optional. When <c>true</c>, the <see cref="RefreshToken"/> property should contain
    /// a valid refresh token obtained from a previous authentication. This allows renewing
    /// the session without providing username and password again.
    /// </remarks>
    [JsonPropertyName("exchange_refresh_token")]
    public bool? ExchangeRefreshToken { get; set; }
}

/// <summary>
/// Represents the authentication response from ArcGIS Monitor.
/// Contains the access token and metadata required for authenticated API calls.
/// </summary>
/// <remarks>
/// <para>
/// This response is returned from the authentication endpoint (<c>/arcgis/auth/token</c>)
/// after a successful authentication request.
/// </para>
/// <para>
/// The <see cref="AccessToken"/> should be included as a Bearer token in the Authorization
/// header of subsequent API requests:
/// <code>
/// Authorization: Bearer {access_token}
/// </code>
/// </para>
/// <para>
/// Access tokens typically expire after a short period (indicated by <see cref="ExpiresIn"/>).
/// Applications should monitor token expiration and re-authenticate or use refresh tokens
/// before the token expires.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // After authentication
/// if (response.Success)
/// {
///     var token = response.AccessToken;
///     var expiresAt = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn);
///     
///     // Use token in subsequent requests
///     httpClient.DefaultRequestHeaders.Authorization = 
///         new AuthenticationHeaderValue("Bearer", token);
/// }
/// </code>
/// </example>
public sealed class TokenResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the authentication was successful.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>true</c> if authentication succeeded and a valid token was issued;
    /// otherwise, <c>false</c>.
    /// </para>
    /// <para>
    /// Always check this property before using <see cref="AccessToken"/>.
    /// If <c>false</c>, the request may have failed due to invalid credentials,
    /// server errors, or other authentication issues.
    /// </para>
    /// </remarks>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the access token for authenticated API requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This token must be included in the Authorization header as a Bearer token
    /// for all protected ArcGIS Monitor API endpoints.
    /// </para>
    /// <para>
    /// The token is a JWT (JSON Web Token) or opaque token string that identifies
    /// and authenticates the user session.
    /// </para>
    /// <para>
    /// The token expires after the period specified in <see cref="ExpiresIn"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);
    /// </code>
    /// </example>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token for renewing the access token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional. Only present if <see cref="TokenRequest.IssueRefreshToken"/> was
    /// set to <c>true</c> in the authentication request.
    /// </para>
    /// <para>
    /// Refresh tokens typically have a longer lifetime than access tokens and can be
    /// used to obtain new access tokens without re-entering credentials.
    /// </para>
    /// <para>
    /// To use the refresh token, send a new <see cref="TokenRequest"/> with:
    /// <list type="bullet">
    /// <item><description><see cref="TokenRequest.RefreshToken"/> set to this value</description></item>
    /// <item><description><see cref="TokenRequest.ExchangeRefreshToken"/> set to <c>true</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiration time in seconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Indicates how many seconds from the time of issuance the access token will remain valid.
    /// </para>
    /// <para>
    /// Applications should track token expiration and re-authenticate before the token expires
    /// to avoid authentication errors during API calls. A common practice is to refresh
    /// the token 1-2 minutes before expiration.
    /// </para>
    /// <para>
    /// Typical values range from 900 seconds (15 minutes) to 3600 seconds (1 hour),
    /// depending on server configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var expiresAt = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - 60); // Refresh 1 min early
    /// </code>
    /// </example>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
