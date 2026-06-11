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

                var reporter = new ArcGISMonitorExcelReporter();
                await reporter.GenerateExcelAsync(configuration, reportPath);

                Log.Information("Report generation completed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error during report generation");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        /// <summary>
        /// Advanced logging with debug level and custom formatting
        /// </summary>
        public static async Task AdvancedLoggingExample()
        {
            // Configure Serilog with more detailed settings
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // Show all debug messages
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Reduce noise from framework
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File("logs/detailed/arcgis-monitor-.log",
                    rollingInterval: RollingInterval.Hour,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    fileSizeLimitBytes: 10_000_000, // 10 MB per file
                    retainedFileCountLimit: 31) // Keep logs for ~31 hours
                .CreateLogger();

            try
            {
                Log.Information("=== Advanced Logging Example ===");

                var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json");
                var reporter = new ArcGISMonitorExcelReporter();
                await reporter.GenerateExcelAsync(configuration, "report.xlsx");

                Log.Information("=== Report Completed ===");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error occurred");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        /// <summary>
        /// Minimal logging for production environments
        /// </summary>
        public static async Task ProductionLoggingExample()
        {
            // Configure Serilog for production: only warnings and errors
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/production/errors-.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30) // Keep 30 days of error logs
                .CreateLogger();

            try
            {
                var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json");
                var reporter = new ArcGISMonitorExcelReporter();
                await reporter.GenerateExcelAsync(configuration, "report.xlsx");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error in production environment");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        /// <summary>
        /// Logging without file output (console only)
        /// </summary>
        public static async Task ConsoleOnlyLoggingExample()
        {
            // Configure Serilog for console output only
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Console-only logging enabled");

                var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json");
                var reporter = new ArcGISMonitorExcelReporter();
                await reporter.GenerateExcelAsync(configuration, "report.xlsx");

                Log.Information("Process completed");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
