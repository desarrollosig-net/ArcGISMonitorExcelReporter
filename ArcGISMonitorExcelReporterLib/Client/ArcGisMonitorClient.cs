using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArcGISMonitorExcelReporterLib.Models;

namespace ArcGISMonitorExcelReporterLib.Client;

public sealed class ArcGisMonitorClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAtUtc;

    public ArcGisMonitorClient(Uri baseUri, HttpClient? httpClient = null, JsonSerializerOptions? jsonOptions = null)
    {
        _disposeClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = baseUri;
        _jsonOptions = jsonOptions ?? MonitorJson.Options;
    }

    public async Task<TokenResponse> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
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
            throw new InvalidOperationException("ArcGIS Monitor no retornó un access_token válido.");

        _accessToken = token.AccessToken;
        _tokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, token.ExpiresIn - 60));
        return token;
    }

    public void SetBearerToken(string accessToken, DateTimeOffset? expiresAtUtc = null)
    {
        _accessToken = accessToken;
        _tokenExpiresAtUtc = expiresAtUtc ?? DateTimeOffset.UtcNow.AddHours(1);
    }

    public async Task<QueryResponse<CollectionFeature>> QueryCollectionsAsync(
        MonitoringQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<MonitoringQueryRequest, QueryResponse<CollectionFeature>>(
            "arcgis/monitoring/collections/query",
            request,
            requiresBearer: true,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<QueryResponse<MetricFeature>> QueryMetricsAsync(
        MonitoringQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<MonitoringQueryRequest, QueryResponse<MetricFeature>>(
            "arcgis/monitoring/metrics/query",
            request,
            requiresBearer: true,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        bool requiresBearer,
        CancellationToken cancellationToken)
    {
        if (requiresBearer)
        {
            if(string.IsNullOrWhiteSpace(_accessToken))
                throw new InvalidOperationException("No hay token configurado. Ejecute AuthenticateAsync o SetBearerToken antes de consultar ArcGIS Monitor.");

            if (_tokenExpiresAtUtc <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("El token está vencido o próximo a vencer. Renueve autenticación antes de ejecutar la consulta.");
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
        return result ?? throw new JsonException($"Respuesta JSON vacía o inválida para {relativeUrl}.");
    }

    public void Dispose()
    {
        if (_disposeClient)
            _httpClient.Dispose();
    }
}
