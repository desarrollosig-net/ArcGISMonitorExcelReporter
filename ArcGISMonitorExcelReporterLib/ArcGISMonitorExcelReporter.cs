using ArcGISMonitorExcelReporterLib.Client;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;
using ArcGISMonitorExcelReporterLib.Reporting;
using Serilog;

namespace ArcGISMonitorExcelReporterLib;

public sealed class ArcGISMonitorExcelReporter(HttpClient? httpClient = null)
{
    private readonly HttpClient? _httpClient = httpClient;

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
        new MonitorExcelReportWriter().Save(report, outputExcelPath);
        Log.Information("Excel file written successfully: {OutputPath}", outputExcelPath);

        return outputExcelPath;
    }

    public async Task<string> GenerateExcelFromConfigurationFileAsync(
        string configurationPath,
        string outputExcelPath,
        CancellationToken cancellationToken = default)
    {
        var configuration = await ReporterConfiguration.LoadAsync(configurationPath, cancellationToken).ConfigureAwait(false);
        return await GenerateExcelAsync(configuration, outputExcelPath, cancellationToken).ConfigureAwait(false);
    }

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

        return new ArcGisMonitorClient(baseUri, clientToUse);
    }
}
