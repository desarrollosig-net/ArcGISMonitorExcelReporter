# Logging

The ArcGIS Monitor Excel Reporter library includes comprehensive logging using [Serilog](https://serilog.net/). Logging provides visibility into every step of the report generation process.

## Features

- **Console Output**: Real-time progress feedback during execution
- **File Output**: Persistent logs with rolling files by date
- **Structured Logging**: Rich, queryable log events
- **Error Tracking**: Detailed exception logging with stack traces
- **Performance Monitoring**: Visibility into query timing and data volumes

## Default Configuration

The console application (`Program.cs`) includes a default Serilog configuration that writes logs relative to the configuration file directory:

```csharp
using Serilog;

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
```

## Output Directories

The application creates two folders relative to the configuration file location:

- **`logs/`**: Contains rolling log files
- **`reports/`**: Contains generated Excel reports

Example structure:
```
D:\ExcelReport\dist\
├── agm2023x.json              (configuration file)
├── logs\
│   ├── arcgis-monitor-reporter-20250108.log
│   └── arcgis-monitor-reporter-20250109.log
└── reports\
    ├── agm2023x_20250108_1015.xlsx
    └── agm2023x_20250108_1430.xlsx
```

## Log Levels

The library uses the following log levels:

| Level | Usage |
|-------|-------|
| **Debug** | Detailed information for diagnosing issues (pagination, page counts, HTTP details) |
| **Information** | General progress information (authentication, queries, file operations) |
| **Warning** | Unexpected situations that don't prevent operation |
| **Error** | Errors that prevent a specific operation but allow the process to continue |
| **Fatal** | Critical errors that cause application termination |

## Logged Operations

### Configuration & Startup
- Configuration file loading
- Configuration validation
- Server URL normalization
- Output path creation

### Authentication
- Authentication request
- Token acquisition
- Token expiration time

### Data Retrieval
- Collection queries with counts
- Component pagination (offset, page size, records retrieved)
- Metric filtering
- Time series data fetching
- Data point counts

### Excel Generation
- Workbook creation
- Sheet writing with row counts
- File size after saving
- Output path confirmation

### Error Handling
- Exception details with stack traces
- Operation context when errors occur
- Failed HTTP requests with status codes

## Example Log Output

```
[10:15:32 INF] === ArcGIS Monitor Excel Reporter Started ===
[10:15:32 INF] Configuration file: D:\ExcelReport\dist\agm2023x.json
[10:15:32 INF] Reports folder: D:\ExcelReport\dist\reports
[10:15:32 INF] Logs folder: D:\ExcelReport\dist\logs
[10:15:32 INF] Output Excel file: D:\ExcelReport\dist\reports\agm2023x_20250108_1015.xlsx
[10:15:32 INF] Loading configuration...
[10:15:32 INF] Configuration loaded successfully
[10:15:32 INF] Validating configuration...
[10:15:32 INF] Configuration validated successfully
[10:15:32 INF] Creating ArcGIS Monitor client for URL: https://monitor.example.com:30443
[10:15:32 INF] Authenticating with ArcGIS Monitor as user: admin
[10:15:32 DBG] Requesting authentication token for user: admin
[10:15:33 DBG] Authentication token acquired, expires at: 2025-01-08 11:15:33 UTC
[10:15:33 INF] Authentication successful
[10:15:33 INF] Building report from 2025-01-03 10:15:33 to 2025-01-08 10:15:33 UTC
[10:15:33 INF] Building report for 1 collections and 4 component types
[10:15:33 INF] Querying collection: Sample Collection, component type: host
[10:15:33 DBG] Getting component count for Sample Collection/host...
[10:15:34 DBG] Total components to retrieve: 25
[10:15:34 DBG] Fetching components page: offset 0, size 100
[10:15:35 DBG] Retrieved 25 components in this page
[10:15:35 DBG] Completed fetching 25 components with metrics
[10:15:35 INF] Retrieved 25 components for Sample Collection/host
[10:15:35 INF] Querying collection: Sample Collection, component type: database
[10:15:36 INF] Retrieved 12 components for Sample Collection/database
[10:15:36 INF] Applying metric filters...
[10:15:36 INF] Fetching metric time series data...
[10:15:36 DBG] Requesting time series for 450 metrics
[10:15:37 DBG] Fetching time series for 450 metrics with bucket: observed_at:15m
[10:15:39 DBG] Retrieved time series data for 450 metrics
[10:15:39 INF] Retrieved 25600 time series data points
[10:15:39 INF] Report build completed: 4 collections, 125 components, 450 metrics, 23 alerts, 25600 data points
[10:15:39 INF] Writing Excel file...
[10:15:39 INF] Creating Excel workbook...
[10:15:39 DBG] Output directory: D:\ExcelReport\dist\reports
[10:15:39 DBG] Writing Summary sheet...
[10:15:40 DBG] Writing Collections sheet (4 rows)...
[10:15:40 DBG] Writing Components sheet (125 rows)...
[10:15:41 DBG] Writing Metrics sheet (450 rows)...
[10:15:42 DBG] Writing Metric_Data sheet (25600 rows)...
[10:15:45 DBG] Writing Alerts sheet (23 rows)...
[10:15:45 DBG] Writing collection-specific sheets...
[10:15:46 DBG] Writing metric-specific sheets...
[10:15:47 INF] Saving Excel file to: D:\ExcelReport\dist\reports\agm2023x_20250108_1015.xlsx
[10:15:48 INF] Excel file saved successfully. Size: 3,456,789 bytes
[10:15:48 INF] Excel file written successfully: D:\ExcelReport\dist\reports\agm2023x_20250108_1015.xlsx
[10:15:48 INF] === Excel report generated successfully: D:\ExcelReport\dist\reports\agm2023x_20250108_1015.xlsx ===
```

## Custom Logging Configuration

You can customize logging by configuring Serilog before running the reporter. See `Samples/LoggingExample.cs` for examples:

### Debug Mode (Verbose)
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/debug-.log", rollingInterval: RollingInterval.Hour)
    .CreateLogger();
```

### Production Mode (Errors Only)
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.File("logs/errors-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

### Console Only
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();
```

## Log Files

By default, log files are created in the `logs/` directory relative to the configuration file location with the following naming pattern:

```
<config-directory>/logs/arcgis-monitor-reporter-20250108.log
<config-directory>/logs/arcgis-monitor-reporter-20250109.log
<config-directory>/logs/arcgis-monitor-reporter-20250110.log
```

Each file contains logs for a single day and includes:
- Precise timestamps (milliseconds)
- Log level
- Detailed messages
- Exception stack traces (when errors occur)

## Output Files Structure

When you run the reporter with a configuration file, the application creates:

```
<config-directory>/
├── agm2023x.json              (your configuration file)
├── logs/
│   └── arcgis-monitor-reporter-20250108.log
└── reports/
    └── agm2023x_20250108_1015.xlsx
```

## Troubleshooting

### Enabling Debug Logs

To see detailed pagination and HTTP request information, change the minimum level to `Debug`:

```csharp
.MinimumLevel.Debug()
```

### Log File Location

If log files aren't appearing, check:
1. The application has write permissions to the configuration file directory
2. The `logs/` folder is created automatically in the same directory as the config file
3. For custom locations, configure Serilog with an absolute path:

```csharp
.WriteTo.File("C:\\CustomLogs\\app-.log", 
    rollingInterval: RollingInterval.Day)
```

### Too Many Logs

To reduce log volume:
1. Use `.MinimumLevel.Information()` or `.MinimumLevel.Warning()`
2. Add overrides for noisy components:
```csharp
.MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
```

## Integration with Other Logging Systems

Serilog supports many additional sinks for integration with logging systems:
- **Application Insights**: `Serilog.Sinks.ApplicationInsights`
- **Elasticsearch**: `Serilog.Sinks.Elasticsearch`
- **Seq**: `Serilog.Sinks.Seq`
- **Splunk**: `Serilog.Sinks.Splunk`

Example with Application Insights:
```csharp
.WriteTo.ApplicationInsights(
    telemetryConfiguration,
    TelemetryConverter.Traces)
```

## Performance Impact

Logging has minimal performance impact:
- **Console output**: Negligible (<1% overhead)
- **File output**: Buffered writes (~1-2% overhead)
- **Debug level**: Additional 2-3% overhead due to string formatting

For maximum performance in production, use `.MinimumLevel.Warning()`.
