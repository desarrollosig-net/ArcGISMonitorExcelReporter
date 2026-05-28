using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArcGISMonitorExcelReporterLib.Models;
using Serilog;

namespace ArcGISMonitorExcelReporterLib.Client;

/// <summary>
/// HTTP client for communicating with the ArcGIS Monitor REST API.
/// Handles authentication, token management, and API requests.
/// </summary>
public sealed class ArcGisMonitorClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAtUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArcGisMonitorClient"/> class.
    /// </summary>
    /// <param name="baseUri">The base URI for the ArcGIS Monitor server (e.g., https://monitor.example.com:30443/).</param>
    /// <param name="httpClient">Optional HTTP client instance. If null, a new client will be created and disposed by this instance.</param>
    /// <param name="jsonOptions">Optional JSON serialization options. If null, default Monitor JSON options will be used.</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds for HTTP requests. Default is 300 seconds (5 minutes). Use -1 for infinite timeout.</param>
    public ArcGisMonitorClient(Uri baseUri, HttpClient? httpClient = null, JsonSerializerOptions? jsonOptions = null, int timeoutSeconds = 300)
    {
        _disposeClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = baseUri;

        // Configure timeout
        if (timeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            Log.Debug("HttpClient timeout configured: {Timeout} seconds", timeoutSeconds);
        }
        else if (timeoutSeconds == -1)
        {
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
            Log.Debug("HttpClient timeout configured: Infinite");
        }

        _jsonOptions = jsonOptions ?? MonitorJson.Options;
    }

    /// <summary>
    /// Authenticates with ArcGIS Monitor and obtains a bearer token.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token response containing the access token and expiration information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if authentication fails or the server doesn't return a valid token.</exception>
    public async Task<TokenResponse> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        Log.Debug("Requesting authentication token for user: {Username}", username);

        var token = await PostAsync<TokenRequest, TokenResponse>(
            "arcgis/auth/token",
            new TokenRequest
            {
                Username = username,
                Password = password,
                RefreshToken = string.Empty,
                IssueRefreshToken = null,
                ExchangeRefreshToken = null
            },
            requiresBearer: false,
            cancellationToken).ConfigureAwait(false);

        if (!token.Success || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            Log.Error("Authentication failed: ArcGIS Monitor did not return a valid access_token");
            throw new InvalidOperationException("ArcGIS Monitor did not return a valid access_token.");
        }

        _accessToken = token.AccessToken;
        _tokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, token.ExpiresIn - 60));

        Log.Debug("Authentication token acquired, expires at: {ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC", _tokenExpiresAtUtc);

        return token;
    }

    /// <summary>
    /// Manually sets a bearer token for authentication without calling the authentication endpoint.
    /// Useful when you have a pre-existing valid token.
    /// </summary>
    /// <param name="accessToken">The bearer access token.</param>
    /// <param name="expiresAtUtc">Optional expiration time (UTC). If null, defaults to 1 hour from now.</param>
    public void SetBearerToken(string accessToken, DateTimeOffset? expiresAtUtc = null)
    {
        _accessToken = accessToken;
        _tokenExpiresAtUtc = expiresAtUtc ?? DateTimeOffset.UtcNow.AddHours(1);
    }

    /// <summary>
    /// Queries collections from ArcGIS Monitor.
    /// </summary>
    /// <param name="request">The collection query request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A query response containing collection features with components, metrics, and related data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no token is configured or the token is expired.</exception>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    public async Task<QueryResponse<CollectionFeature>> QueryCollectionsAsync(
        CollectionQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<CollectionQueryRequest, QueryResponse<CollectionFeature>>(
            "arcgis/monitoring/collections/query",
            request,
            requiresBearer: true,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Queries metrics from ArcGIS Monitor.
    /// </summary>
    /// <param name="request">The metric query request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A query response containing metric features with time series data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no token is configured or the token is expired.</exception>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    public async Task<QueryResponse<MetricFeature>> QueryMetricsAsync(
        MetricQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<MetricQueryRequest, QueryResponse<MetricFeature>>(
            "arcgis/monitoring/metrics/query",
            request,
            requiresBearer: true,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the ArcGIS Monitor API and deserializes the response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response object.</typeparam>
    /// <param name="relativeUrl">The relative API endpoint URL.</param>
    /// <param name="request">The request object to serialize and send.</param>
    /// <param name="requiresBearer">Whether a bearer token is required for this request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if bearer authentication is required but no valid token is available.</exception>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    /// <exception cref="JsonException">Thrown if the response cannot be deserialized.</exception>
    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        bool requiresBearer,
        CancellationToken cancellationToken)
    {
        if (requiresBearer)
        {
            if(string.IsNullOrWhiteSpace(_accessToken))
                throw new InvalidOperationException("No token configured. Execute AuthenticateAsync or SetBearerToken before querying ArcGIS Monitor.");

            if (_tokenExpiresAtUtc <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Token is expired or about to expire. Renew authentication before executing the query.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(request, options: _jsonOptions)
        };

        if (requiresBearer)
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Error HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");

        var result = JsonSerializer.Deserialize<TResponse>(body, _jsonOptions);
        return result ?? throw new JsonException($"Empty or invalid JSON response for {relativeUrl}.");
    }

    /// <summary>
    /// Disposes the HTTP client if it was created by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposeClient)
            _httpClient.Dispose();
    }
}
