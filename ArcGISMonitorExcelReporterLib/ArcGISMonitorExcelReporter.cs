using ArcGISMonitorExcelReporterLib.Client;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;
using ArcGISMonitorExcelReporterLib.Models;
using ArcGISMonitorExcelReporterLib.Reporting;
using Serilog;
using System.Diagnostics;

namespace ArcGISMonitorExcelReporterLib
{
    /// <summary>
    /// Main entry point for generating Excel reports from ArcGIS Monitor data.
    /// Coordinates authentication, data querying, and Excel file generation.
    /// </summary>
    /// <param name="httpClient">Optional HTTP client to use for all requests. If null, clients will be created internally.</param>
    public sealed class ArcGisMonitorExcelReporter(HttpClient? httpClient = null)
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

            // Fetch and log ArcGIS Monitor version information
            var monitoringInfo = await queryService.GetMonitoringInfoAsync(cancellationToken).ConfigureAwait(false);
            if(monitoringInfo != null && !string.IsNullOrEmpty(monitoringInfo.Version))
            {
                Log.Information("ArcGIS Monitor Version: {Version}", monitoringInfo);
            }
            else
            {
                Log.Debug("Could not retrieve ArcGIS Monitor version information");
            }

            // Fetch field information for all available resources
            var resourceFieldsDict = new Dictionary<string, ResourceFieldInfo>();
            if(monitoringInfo?.Resources?.Count > 0)
            {
                Log.Information("Fetching field information for {Count} resources", monitoringInfo.Resources.Count);
                resourceFieldsDict = await queryService.GetAllResourceFieldsAsync(monitoringInfo.Resources, cancellationToken).ConfigureAwait(false);
                Log.Information("Retrieved field information for {Count} resources", resourceFieldsDict.Count);
            }

            // Fetch component types information
            var componentTypesInfo = await queryService.GetComponentTypesAsync(cancellationToken).ConfigureAwait(false);
            if(componentTypesInfo?.Types?.Count > 0)
            {
                Log.Information("Retrieved {Count} component types", componentTypesInfo.Types.Count);
            }

            var reportService = new MonitorReportService(queryService);

            Log.Information("Building report from {FromUtc:yyyy-MM-dd HH:mm:ss} to {ToUtc:yyyy-MM-dd HH:mm:ss} UTC",
                configuration.ToReportRequest().FromUtc,
                configuration.ToReportRequest().ToUtc);

            var report = await reportService.BuildReportAsync(configuration.ToReportRequest(), cancellationToken).ConfigureAwait(false);

            // Attach monitoring information to the report
            report.MonitoringInfo = monitoringInfo;
            report.ResourceFields = resourceFieldsDict;
            report.ComponentTypes = componentTypesInfo;

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
            CancellationToken cancellationToken = default) => await GenerateExcelAsyncInternal(configuration, outputExcelPath, null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Builds a monitor report and writes it to an Excel file with execution time tracking.
        /// </summary>
        /// <param name="configuration">Configuration containing server connection details and report parameters.</param>
        /// <param name="outputExcelPath">Full path where the Excel file should be written.</param>
        /// <param name="stopwatch">Stopwatch to measure execution time.</param>
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
            Stopwatch stopwatch,
            CancellationToken cancellationToken = default) => await GenerateExcelAsyncInternal(configuration, outputExcelPath, stopwatch, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Internal implementation of GenerateExcelAsync.
        /// </summary>
        private async Task<string> GenerateExcelAsyncInternal(
            ReporterConfiguration configuration,
            string outputExcelPath,
            Stopwatch? stopwatch,
            CancellationToken cancellationToken)
        {
            if(string.IsNullOrWhiteSpace(outputExcelPath))
            {
                throw new ArgumentException("Output Excel file path must be specified.", nameof(outputExcelPath));
            }

            Log.Information("Starting report generation for output: {OutputPath}", outputExcelPath);
            var report = await BuildReportAsync(configuration, cancellationToken).ConfigureAwait(false);

            // Set execution time if stopwatch was provided
            if(stopwatch != null)
            {
                report.ExecutionTime = stopwatch.Elapsed;
            }

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
            var baseUri = new Uri(baseUrl + Path.DirectorySeparatorChar);

            var clientToUse = _httpClient;
            if(clientToUse is null && configuration.Server.IgnoreSslErrors)
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
}
