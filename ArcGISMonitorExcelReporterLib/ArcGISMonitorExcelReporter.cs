using ArcGISMonitorExcelReporterLib.Client;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;
using ArcGISMonitorExcelReporterLib.Reporting;
using Serilog;

namespace ArcGISMonitorExcelReporterLib;

/// <summary>
/// Main entry point for generating Excel reports from ArcGIS Monitor data.
/// Coordinates authentication, data querying, and Excel file generation.
/// </summary>
/// <param name="httpClient">Optional HTTP client to use for all requests. If null, clients will be created internally.</param>
public sealed class ArcGISMonitorExcelReporter(HttpClient? httpClient = null)
{
    private readonly HttpClient? _httpClient = httpClient;

    /// <summary>
    /// Builds a monitor report by querying ArcGIS Monitor based on the provided configuration.
    /// </summary>
    /// <param name="configuration">Configuration containing server connection details and report parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="MonitorExcelReport"/> containing all queried data organized into normalized tables.</returns>
    /// <exception cref="ArgumentNullException">Thrown if configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if configuration validation fails or authentication fails.</exception>
    /// <exception cref="HttpRequestException">Thrown if communication with ArcGIS Monitor fails.</exception>
    public async Task<MonitorExcelReport> BuildReportAsync(
        ReporterConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Log.Information("Validating configuration...");
        configuration.Validate();
        Log.Information("Configuration validated successfully");

        Log.Information("Creating ArcGIS Monitor client for URL: {Url}", configuration.Server.Url);
        using var client = CreateClient(configuration);

        Log.Information("Authenticating with ArcGIS Monitor as user: {Username}", configuration.Server.Username);
        await client.AuthenticateAsync(
            configuration.Server.Username,
            configuration.Server.GetPassword(),
            cancellationToken).ConfigureAwait(false);
        Log.Information("Authentication successful");

        var queryService = new ArcGisMonitorQueryService(client);
        var reportService = new MonitorReportService(queryService);

        Log.Information("Building report from {FromUtc:yyyy-MM-dd HH:mm:ss} to {ToUtc:yyyy-MM-dd HH:mm:ss} UTC", 
            configuration.ToReportRequest().FromUtc, 
            configuration.ToReportRequest().ToUtc);

        var report = await reportService.BuildReportAsync(configuration.ToReportRequest(), cancellationToken).ConfigureAwait(false);

        Log.Information("Report built successfully: {Collections} collections, {Components} components, {Metrics} metrics", 
            report.Collections.Count, report.Components.Count, report.Metrics.Count);

        return report;
    }

    /// <summary>
    /// Builds a monitor report and writes it to an Excel file.
    /// </summary>
    /// <param name="configuration">Configuration containing server connection details and report parameters.</param>
    /// <param name="outputExcelPath">Full path where the Excel file should be written.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The path to the generated Excel file.</returns>
    /// <exception cref="ArgumentException">Thrown if outputExcelPath is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if configuration validation or authentication fails.</exception>
    /// <exception cref="HttpRequestException">Thrown if communication with ArcGIS Monitor fails.</exception>
    /// <exception cref="IOException">Thrown if Excel file cannot be written.</exception>
    public async Task<string> GenerateExcelAsync(
        ReporterConfiguration configuration,
        string outputExcelPath,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(outputExcelPath))
        {
            throw new ArgumentException("Output Excel file path must be specified.", nameof(outputExcelPath));
        }

        Log.Information("Starting report generation for output: {OutputPath}", outputExcelPath);
        var report = await BuildReportAsync(configuration, cancellationToken).ConfigureAwait(false);

        Log.Information("Writing Excel file...");
        MonitorExcelReportWriter.Save(report, outputExcelPath);
        Log.Information("Excel file written successfully: {OutputPath}", outputExcelPath);

        return outputExcelPath;
    }

    /// <summary>
    /// Loads configuration from a JSON file and generates an Excel report.
    /// </summary>
    /// <param name="configurationPath">Path to the JSON configuration file.</param>
    /// <param name="outputExcelPath">Full path where the Excel file should be written.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The path to the generated Excel file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if configuration file doesn't exist.</exception>
    /// <exception cref="JsonException">Thrown if configuration file is invalid JSON.</exception>
    /// <exception cref="ArgumentException">Thrown if outputExcelPath is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown if configuration validation or authentication fails.</exception>
    public async Task<string> GenerateExcelFromConfigurationFileAsync(
        string configurationPath,
        string outputExcelPath,
        CancellationToken cancellationToken = default)
    {
        var configuration = await ReporterConfiguration.LoadAsync(configurationPath, cancellationToken).ConfigureAwait(false);
        return await GenerateExcelAsync(configuration, outputExcelPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an ArcGIS Monitor client with the appropriate configuration.
    /// Handles URL normalization and SSL certificate validation settings.
    /// </summary>
    /// <param name="configuration">The reporter configuration.</param>
    /// <returns>A configured <see cref="ArcGisMonitorClient"/> instance.</returns>
    private ArcGisMonitorClient CreateClient(ReporterConfiguration configuration)
    {
        var baseUrl = configuration.Server.Url.TrimEnd('/');
        if(baseUrl.EndsWith("/arcgis", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl[..^"/arcgis".Length];
        }

        var baseUri = new Uri(baseUrl.TrimEnd('/') + "/");

        HttpClient? clientToUse = _httpClient;
        if (clientToUse is null && configuration.Server.IgnoreSslErrors)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            clientToUse = new HttpClient(handler);
        }

        return new ArcGisMonitorClient(baseUri, clientToUse, timeoutSeconds: configuration.Server.TimeoutSeconds);
    }
}
