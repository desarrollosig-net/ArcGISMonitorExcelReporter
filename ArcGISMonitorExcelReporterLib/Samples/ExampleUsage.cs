// Ignore Spelling: Json

using ArcGISMonitorExcelReporterLib;
using ArcGISMonitorExcelReporterLib.Configuration;

using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

namespace ArcGISMonitorExcelReporterLib.Samples
{
    /// <summary>
    /// Provides example usage scenarios for the ArcGIS Monitor Excel Reporter.
    /// </summary>
    /// <remarks>
    /// This class demonstrates two different approaches to generating Excel reports from ArcGIS Monitor:
    /// <list type="bullet">
    /// <item><description>Loading configuration from a JSON file</description></item>
    /// <item><description>Programmatically creating configuration objects</description></item>
    /// </list>
    /// </remarks>
    public static class ExampleUsage
    {
        /// <summary>
        /// Generates an Excel report from a JSON configuration file asynchronously.
        /// </summary>
        /// <remarks>
        /// This method demonstrates the simplest approach: load a pre-configured JSON file
        /// and generate an Excel report with minimal code. The JSON file should contain all
        /// necessary configuration including server connection details and report parameters.
        /// 
        /// Recommended for:
        /// <list type="bullet">
        /// <item><description>Production environments using predefined configurations</description></item>
        /// <item><description>Scheduled batch jobs with static parameters</description></item>
        /// <item><description>Configuration management scenarios</description></item>
        /// </list>
        /// </remarks>
        /// <param name="cancellationToken">Cancellation token to stop the asynchronous operation if needed. Defaults to <see cref="CancellationToken.None"/>.</param>
        /// <exception cref="FileNotFoundException">Thrown if the configuration file does not exist.</exception>
        /// <exception cref="System.Text.Json.JsonException">Thrown if the configuration file cannot be deserialized.</exception>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails or authentication fails.</exception>
        /// <exception cref="HttpRequestException">Thrown if communication with ArcGIS Monitor fails.</exception>
        /// <exception cref="IOException">Thrown if the Excel file cannot be written.</exception>
        /// <example>
        /// <code>
        /// // Generate report from JSON configuration file
        /// await ExampleUsage.GenerateFromJsonAsync();
        /// 
        /// // With cancellation support
        /// var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        /// await ExampleUsage.GenerateFromJsonAsync(cts.Token);
        /// </code>
        /// </example>
        public static async Task GenerateFromJsonAsync(CancellationToken cancellationToken = default)
        {
            var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json", cancellationToken);
            var reporter = new ArcGisMonitorExcelReporter();

            await reporter.GenerateExcelAsync(
                configuration,
                "ArcGISMonitorReport.xlsx",
                cancellationToken);
        }

        /// <summary>
        /// Generates an Excel report from a programmatically constructed configuration object asynchronously.
        /// </summary>
        /// <remarks>
        /// This method demonstrates dynamic configuration where all parameters are set via code.
        /// This approach provides maximum flexibility and is useful for:
        /// <list type="bullet">
        /// <item><description>Parameterized reports with runtime-determined values</description></item>
        /// <item><description>Integration scenarios where configuration comes from external systems</description></item>
        /// <item><description>Ad-hoc reporting with specific, non-standard parameters</description></item>
        /// </list>
        /// 
        /// The example configuration includes:
        /// <list type="bullet">
        /// <item><description>Server connection to https://monitor.example.com:30443/arcgis</description></item>
        /// <item><description>Authentication with username and password (not encoded)</description></item>
        /// <item><description>5-minute timeout for HTTP requests</description></item>
        /// <item><description>Report collection: "Sample Collection"</description></item>
        /// <item><description>Timezone: America/Bogota</description></item>
        /// <item><description>Time period: last 5 days ending now</description></item>
        /// <item><description>Resource types: host, storage, service, and database</description></item>
        /// <item><description>All metrics included (no filtering by alerting status)</description></item>
        /// </list>
        /// </remarks>
        /// <param name="cancellationToken">Cancellation token to stop the asynchronous operation if needed. Defaults to <see cref="CancellationToken.None"/>.</param>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails or authentication fails.</exception>
        /// <exception cref="HttpRequestException">Thrown if communication with ArcGIS Monitor fails.</exception>
        /// <exception cref="IOException">Thrown if the Excel file cannot be written.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
        /// <example>
        /// <code>
        /// // Generate report with programmatic configuration
        /// await ExampleUsage.GenerateFromConfigurationObjectAsync();
        /// 
        /// // With cancellation token for timeout protection
        /// var cts = new CancellationTokenSource();
        /// cts.CancelAfter(TimeSpan.FromMinutes(15));
        /// await ExampleUsage.GenerateFromConfigurationObjectAsync(cts.Token);
        /// 
        /// // Example of modifying the configuration
        /// // (this requires extracting and modifying the code)
        /// var config = new ReporterConfiguration
        /// {
        ///     Server = new ServerConfiguration
        ///     {
        ///         Url = "https://your-monitor.server.com:30443/arcgis",
        ///         Username = "admin",
        ///         Password = "your-password",
        ///         TimeoutSeconds = 600
        ///     },
        ///     Report = new ReportConfiguration
        ///     {
        ///         Timezone = "UTC",
        ///         PastDays = 7,
        ///         Types = ["host", "database"]
        ///     }
        /// };
        /// </code>
        /// </example>
        public static async Task GenerateFromConfigurationObjectAsync(CancellationToken cancellationToken = default)
        {
            // Base URL of the ArcGIS Monitor server
            var monitorUrl = "https://monitor.example.com:30443/arcgis";

            // Complete reporter configuration including credentials and report parameters
            var configuration = new ReporterConfiguration
            {
                // ArcGIS Monitor server configuration
                Server = new ServerConfiguration
                {
                    Url = monitorUrl,
                    Username = "user",
                    Password = "password",
                    PasswordEncoding = false,           // Credentials are not encoded
                    TimeoutSeconds = 300                // 5-minute timeout for requests
                },
                // Specific configuration of the report to generate
                Report = new ReportConfiguration
                {
                    Collection = "Sample Collection",    // Name of the collection to report
                    Timezone = "America/Bogota",         // Timezone for report data
                    EndTime = new EndTimeConfiguration { Now = true }, // Report ends at current time
                    PastDays = 5,                        // Include data from the last 5 days
                    PastHours = 0,                       // No additional hours to include
                    Types = ["host", "storage", "service", "database"], // Types of resources to report
                    Metrics = new MetricsConfiguration
                    {
                        AlertingOnOnly = false,          // Include all metrics, not just alerts
                        IncludeOnly = [],                // No specific inclusion restrictions
                        ExcludeMetrics = []              // No metrics excluded
                    }
                }
            };

            // Instantiate the Excel report generator
            var reporter = new ArcGisMonitorExcelReporter();

            // Generate the Excel file asynchronously
            await reporter.GenerateExcelAsync(configuration, "ArcGISMonitorReport.xlsx", cancellationToken);
        }
    }
}
