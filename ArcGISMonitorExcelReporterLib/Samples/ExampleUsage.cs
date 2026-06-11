using ArcGISMonitorExcelReporterLib;
using ArcGISMonitorExcelReporterLib.Configuration;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

namespace ArcGISMonitorExcelReporterLib.Samples
{
    public static class ExampleUsage
    {
        public static async Task GenerateFromJsonAsync(CancellationToken cancellationToken = default)
        {
            var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json", cancellationToken);
            var reporter = new ArcGISMonitorExcelReporter();

            await reporter.GenerateExcelAsync(
                configuration,
                "ArcGISMonitorReport.xlsx",
                cancellationToken);
        }

        public static async Task GenerateFromConfigurationObjectAsync(CancellationToken cancellationToken = default)
        {
            var configuration = new ReporterConfiguration
            {
                Server = new ServerConfiguration
                {
                    Url = "https://monitor.example.com:30443/arcgis",
                    Username = "user",
                    Password = "password",
                    PasswordEncoding = false,
                    TimeoutSeconds = 300
                },
                Report = new ReportConfiguration
                {
                    Collection = "Sample Collection",
                    Timezone = "America/Bogota",
                    EndTime = new EndTimeConfiguration { Now = true },
                    PastDays = 5,
                    PastHours = 0,
                    Types = ["host", "storage", "service", "database"],
                    Metrics = new MetricsConfiguration
                    {
                        AlertingOnOnly = false,
                        IncludeOnly = [],
                        ExcludeMetrics = []
                    }
                }
            };

            var reporter = new ArcGISMonitorExcelReporter();
            await reporter.GenerateExcelAsync(configuration, "ArcGISMonitorReport.xlsx", cancellationToken);
        }
    }
}
