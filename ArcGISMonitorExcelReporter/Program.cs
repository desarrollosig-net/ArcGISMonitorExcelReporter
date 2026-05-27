using ArcGISMonitorExcelReporterLib;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;
using Serilog;

// This application creates two folders relative to the configuration file location:
// - logs/: Contains rolling log files (arcgis-monitor-reporter-{date}.log)
// - reports/: Contains generated Excel reports ({config-name}_{yyyyMMdd_HHmm}.xlsx)

try
{
    var cancellationToken = CancellationToken.None;
    var configFilePath = "D:\\ExcelReport\\dist\\agm2023x.json";

    // Get the directory containing the configuration file
    var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configFilePath)) ?? Directory.GetCurrentDirectory();

    // Create logs folder relative to config file
    var logsFolder = Path.Combine(configDirectory, "logs");
    Directory.CreateDirectory(logsFolder);

    // Create reports folder relative to config file
    var reportsFolder = Path.Combine(configDirectory, "reports");
    Directory.CreateDirectory(reportsFolder);

    // Configure Serilog with logs folder relative to config
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(Path.Combine(logsFolder, "arcgis-monitor-reporter-.log"),
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("=== ArcGIS Monitor Excel Reporter Started ===");
    Log.Information("Configuration file: {ConfigPath}", configFilePath);
    Log.Information("Reports folder: {ReportsFolder}", reportsFolder);
    Log.Information("Logs folder: {LogsFolder}", logsFolder);

    // Generate output filename
    var outputFileName = Path.GetFileNameWithoutExtension(configFilePath) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
    var outputExcelPath = Path.Combine(reportsFolder, outputFileName);

    Log.Information("Output Excel file: {OutputPath}", outputExcelPath);

    Log.Information("Loading configuration...");
    var configuration = await ReporterConfiguration.LoadAsync(configFilePath, cancellationToken);
    Log.Information("Configuration loaded successfully");

    var reporter = new ArcGISMonitorExcelReporter();

    Log.Information("Starting Excel report generation...");
    await reporter.GenerateExcelAsync(
        configuration,
        outputExcelPath,
        cancellationToken);

    Log.Information("=== Excel report generated successfully: {OutputPath} ===", outputExcelPath);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fatal error occurred during report generation");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
