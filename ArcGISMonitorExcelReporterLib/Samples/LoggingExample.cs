using ArcGISMonitorExcelReporterLib;

using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

using Serilog;
using Serilog.Events;

namespace ArcGISMonitorExcelReporterLib.Samples
{
    /// <summary>
    /// Example showing how to configure logging for the ArcGIS Monitor Excel Reporter
    /// </summary>
    public static class LoggingExample
    {
        /// <summary>
        /// Basic logging setup with console and file output
        /// </summary>
        public static async Task BasicLoggingExample()
        {
            var configFilePath = "agm2023x.json";

            // Get the directory containing the configuration file
            var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configFilePath)) ?? Directory.GetCurrentDirectory();

            // Create logs folder relative to config file
            var logsFolder = Path.Combine(configDirectory, "logs");
            Directory.CreateDirectory(logsFolder);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine(logsFolder, "arcgis-monitor-reporter-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Starting report generation");

                var configuration = await ReporterConfiguration.LoadAsync(configFilePath);

                // Create reports folder
                var reportsFolder = Path.Combine(configDirectory, "reports");
                Directory.CreateDirectory(reportsFolder);

                var reportPath = Path.Combine(reportsFolder, "report.xlsx");

                var reporter = new ArcGisMonitorExcelReporter();
                await reporter.GenerateExcelAsync(configuration, reportPath);

                Log.Information("Report generation completed successfully");
            }
            catch(Exception ex)
            {
                Log.Fatal(ex, "Fatal error during report generation");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
