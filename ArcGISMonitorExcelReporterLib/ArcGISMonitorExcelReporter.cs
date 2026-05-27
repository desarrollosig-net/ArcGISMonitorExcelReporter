using ArcGISMonitorExcelReporterLib.Client;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;
using ArcGISMonitorExcelReporterLib.Reporting;

namespace ArcGISMonitorExcelReporterLib;

public sealed class ArcGISMonitorExcelReporter(HttpClient? httpClient = null)
{
    private readonly HttpClient? _httpClient = httpClient;

    public async Task<MonitorExcelReport> BuildReportAsync(
        ReporterConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        using var client = CreateClient(configuration);
        await client.AuthenticateAsync(
            configuration.Server.Username,
            configuration.Server.GetPassword(),
            cancellationToken).ConfigureAwait(false);

        var queryService = new ArcGisMonitorQueryService(client);
        var reportService = new MonitorReportService(queryService);
        return await reportService.BuildReportAsync(configuration.ToReportRequest(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GenerateExcelAsync(
        ReporterConfiguration configuration,
        string outputExcelPath,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(outputExcelPath))
        {
            throw new ArgumentException("Debe indicar la ruta del archivo Excel de salida.", nameof(outputExcelPath));
        }

        var report = await BuildReportAsync(configuration, cancellationToken).ConfigureAwait(false);
        new MonitorExcelReportWriter().Save(report, outputExcelPath);
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
