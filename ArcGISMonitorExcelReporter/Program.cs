using ArcGISMonitorExcelReporterLib;

using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;
using Reporter = ArcGISMonitorExcelReporterLib.ArcGisMonitorExcelReporter;

using Serilog;
using System.Diagnostics;

// This application creates two folders relative to the configuration file location:
// - logs/: Contains rolling log files (arcgis-monitor-reporter-{date}.log)
// - reports/: Contains generated Excel reports ({config-name}_{yyyyMMdd_HHmm}.xlsx)

// Start measuring execution time
var stopwatch = Stopwatch.StartNew();

// Parse command line arguments
if(!TryParseArguments(args, out var configFilePath))
{
    return 1; // Exit with error code
}

// In DEBUG mode, show parsed configuration for verification
Console.WriteLine($"ArcGIS Monitor Excel Reporter v{VersionInfo.Version}");
Console.WriteLine($"Build: {VersionInfo.BuildTimestamp}");
Console.WriteLine($"Configuration file: {configFilePath}");
Console.WriteLine($"Full path: {Path.GetFullPath(configFilePath)}");
Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Not set"}");
Console.WriteLine();

try
{
    var cancellationToken = CancellationToken.None;

    // Get the directory containing the configuration file
    var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configFilePath)) ?? Directory.GetCurrentDirectory();

    // Create logs folder relative to config file
    var logsFolder = Path.Combine(configDirectory, "logs");
    Directory.CreateDirectory(logsFolder);

    // Create reports folder relative to config file
    var reportsFolder = Path.Combine(configDirectory, "reports");
    Directory.CreateDirectory(reportsFolder);

    // Configure Serilog with logs folder relative to config
    var logLevel = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development"
        ? Serilog.Events.LogEventLevel.Debug
        : Serilog.Events.LogEventLevel.Information;

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(logLevel)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(Path.Combine(logsFolder, "arcgis-monitor-reporter-.log"),
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    var decoration = new string('=', 60);

#pragma warning disable S6664
    Log.Information(decoration);
#pragma warning restore S6664
    Log.Information("=== ArcGIS Monitor Excel Reporter {Version} ===", VersionInfo.Version);
    Log.Information("=== Build: {BuildTimestamp} ===", VersionInfo.BuildTimestamp);
    Log.Information(decoration);
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

    var reporter = new Reporter();

    Log.Information("Starting Excel report generation...");
    await reporter.GenerateExcelAsync(
        configuration,
        outputExcelPath,
        stopwatch,
        cancellationToken);

    stopwatch.Stop();
    var executionTime = stopwatch.Elapsed;

    Log.Information(decoration);
    Log.Information("=== Report generated successfully ===");
    Log.Information("=== Output: {OutputPath} ===", outputExcelPath);
    Log.Information("=== Execution time: {ExecutionTime} ===", executionTime.ToString("hh\\:mm\\:ss"));
    Log.Information("=== Version: {Version} ===", VersionInfo.Version);
    Log.Information(decoration);
    return 0; // Exit with success code
}
catch(Exception ex)
{
    Log.Error(ex, "Fatal error occurred during report generation");
    return 1; // Exit with error code
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Parses command line arguments to extract the configuration file path.
/// </summary>
/// <param name="args">Command line arguments.</param>
/// <param name="configFilePath">Output parameter containing the validated configuration file path.</param>
/// <returns>True if arguments are valid and file exists; otherwise false.</returns>
static bool TryParseArguments(string[] args, out string configFilePath)
{
    configFilePath = string.Empty;

    // Check for help argument first
    if(args.Length > 0 && (args[0] == "-h" || args[0] == "--help" || args[0] == "-?" || args[0] == "/?"))
    {
        ShowHelp();
        return false;
    }

    // Check if -f argument is provided
    var fIndex = Array.IndexOf(args, "-f");

    if(fIndex == -1 || fIndex + 1 >= args.Length)
    {
        Console.WriteLine("Error: Missing required argument -f <config-file>");
        Console.WriteLine();
        ShowHelp();
        return false;
    }

    configFilePath = args[fIndex + 1];

    // Validate that the file path is not empty
    if(string.IsNullOrWhiteSpace(configFilePath))
    {
        Console.WriteLine("Error: Configuration file path cannot be empty");
        Console.WriteLine();
        ShowHelp();
        return false;
    }

    // Validate that the file exists
    if(!File.Exists(configFilePath))
    {
        Console.WriteLine($"Error: Configuration file not found: {configFilePath}");
        Console.WriteLine($"       Full path attempted: {Path.GetFullPath(configFilePath)}");
        Console.WriteLine();
        Console.WriteLine("Please verify:");
        Console.WriteLine("  - The file path is correct");
        Console.WriteLine("  - The file exists");
        Console.WriteLine("  - You have read permissions");
        return false;
    }

    // Validate that it's a JSON file
    if(!configFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"Warning: Configuration file does not have .json extension: {configFilePath}");
        Console.WriteLine();
    }

    return true;
}

/// <summary>
/// Displays usage information and examples.
/// </summary>
static void ShowHelp()
{
    Console.WriteLine($"ArcGIS Monitor Excel Reporter v{VersionInfo.Version}");
    Console.WriteLine($"Build: {VersionInfo.BuildTimestamp}");
    var decoration = new string('=', 60);
    Console.WriteLine(decoration);
    Console.WriteLine("Usage:");
    Console.WriteLine("  ArcGISMonitorExcelReporter -f <config-file>");
    Console.WriteLine("  ArcGISMonitorExcelReporter -h | --help");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  -f <config-file>    Path to the JSON configuration file (required)");
    Console.WriteLine("  -h, --help          Display this help information");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  ArcGISMonitorExcelReporter -f config.json");
    Console.WriteLine("  ArcGISMonitorExcelReporter -f \"C:\\Reports\\production.json\"");
    Console.WriteLine("  ArcGISMonitorExcelReporter -f /var/config/monitor.json");
    Console.WriteLine("  ArcGISMonitorExcelReporter -f \"..\\..\\data\\inocar.json\"  (relative path)");
    Console.WriteLine();
    Console.WriteLine("Output:");
    Console.WriteLine("  - Excel reports are saved to: <config-directory>/reports/");
    Console.WriteLine("  - Log files are saved to: <config-directory>/logs/");
    Console.WriteLine();
    Console.WriteLine("Debug Mode:");
    Console.WriteLine("  Set DOTNET_ENVIRONMENT=Development for detailed logging");
    Console.WriteLine("  Use launchSettings.json profiles in Visual Studio for easy debugging");
    Console.WriteLine();
    Console.WriteLine("Configuration:");
    Console.WriteLine("  See sample configuration: agm2023x.sample.json");
    Console.WriteLine("  Documentation: https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter");
}
