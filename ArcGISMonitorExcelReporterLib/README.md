# ArcGISMonitorExcelReporterLib

.NET 8 library for querying ArcGIS Monitor, structuring HTTP calls for authentication, collections, and metrics, and exporting outputs to an Excel file.

## Objective

The project exposes a single entry point through the `ArcGISMonitorExcelReporter` class. Consumers can invoke it with a `Configuration` object loaded from a JSON file following the structure of `agm2023x.json`.

## Main Structure

```text
ArcGISMonitorExcelReporterLib/
├─ ArcGISMonitorExcelReporter.cs
├─ ArcGISMonitorExcelReporterLib.csproj
├─ Configuration/
│  └─ Configuration.cs
├─ Client/
│  ├─ ArcGisMonitorClient.cs
│  └─ ArcGisMonitorQueryService.cs
├─ Builders/
│  └─ MonitorQueryBuilders.cs
├─ Models/
│  ├─ AuthModels.cs
│  ├─ JsonOptions.cs
│  ├─ QueryModels.cs
│  └─ ResponseModels.cs
├─ Reporting/
│  ├─ MonitorExcelReportModels.cs
│  ├─ MonitorExcelReportWriter.cs
│  └─ MonitorReportService.cs
└─ Samples/
   ├─ ExampleUsage.cs
   └─ agm2023x.sample.json
```

## Main Dependency

```xml
<PackageReference Include="ClosedXML" Version="0.104.2" />
<PackageReference Include="Serilog" Version="4.2.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
```

## Logging

The library uses Serilog for comprehensive logging to both console and file. Each step of the process is logged:

- Configuration loading and validation
- Authentication with ArcGIS Monitor
- Collection and component queries with pagination details
- Metric data retrieval
- Time series data fetching
- Excel file creation and writing
- Error handling with detailed context

Logs are written to:
- **Console**: Real-time feedback with timestamp and log level
- **File**: Rolling log files in `logs/arcgis-monitor-reporter-{date}.log` relative to the configuration file directory

Excel reports are saved to:
- **Reports folder**: `reports/` directory relative to the configuration file location
- **Naming pattern**: `{config-name}_{yyyyMMdd_HHmm}.xlsx`

Example directory structure:
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

Example log output:
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
[10:15:33 INF] Authentication successful
[10:15:33 INF] Building report for 1 collections and 4 component types
[10:15:33 INF] Querying collection: Sample Collection, component type: host
[10:15:35 INF] Retrieved 25 components for Sample Collection/host
[10:15:35 INF] Excel file saved successfully. Size: 1,234,567 bytes
[10:15:35 INF] === Excel report generated successfully: D:\ExcelReport\dist\reports\agm2023x_20250108_1015.xlsx ===
```

## Usage from JSON File

```csharp
using ArcGISMonitorExcelReporterLib;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json");
var reporter = new ArcGISMonitorExcelReporter();

await reporter.GenerateExcelAsync(
    configuration,
    "ArcGISMonitorReport.xlsx");
```

## Direct Usage with Configuration Object

```csharp
using ArcGISMonitorExcelReporterLib;
using ArcGISMonitorExcelReporterLib.Configuration;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var configuration = new ReporterConfiguration
{
    Server = new ServerConfiguration
    {
        Url = "https://monitor.example.com:30443/arcgis",
        Username = "user",
        Password = "password",
        PasswordEncoding = false
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
await reporter.GenerateExcelAsync(configuration, "ArcGISMonitorReport.xlsx");
```

## Configuration Contract

The JSON file must contain these blocks:

- `server.url`: ArcGIS Monitor base URL. Can end with `/arcgis`; the library normalizes the URL to avoid duplicating the segment.
- `server.username`: authentication username.
- `server.password`: password. Should not be versioned in repositories.
- `server.password_encoding`: if `true`, the password is interpreted as Base64 UTF-8.
- `report.collection`: collection name.
- `report.timezone`: timezone used to calculate the time range.
- `report.end_time`: end date or `now=true`.
- `report.past_days` and `report.past_hours`: window backward from `end_time`.
- `report.types`: component types to query.
- `report.metrics.alerting_on_only`: keeps only metrics with active alerting.
- `report.metrics.include_only`: list of metric names or prefixes to include.
- `report.metrics.exclude_metrics`: list of metric names or fragments to exclude.

## Excel Output

The generated Excel file contains:

- `Summary`: general index with counts and internal links.
- `Collections`: summary by collection and component type.
- `Components`: queried components.
- `Metrics`: metrics associated with components.
- `Metric_Data`: metric series or aggregates (includes min, max, avg, stddev, percentile 95, sum, count).
- `Alerts`: associated alerts.
- `COL_*` sheets: separation by collection/type.
- `MET_*` sheets: separation by metric.

Sheet names are sanitized to comply with Excel restrictions: maximum 31 characters and removal of invalid characters.

## Documentation

**Complete documentation in English is available:**

### Quick Links
- 📖 [Complete API Documentation](Docs/api-documentation.md) - Comprehensive API reference for all classes
- ⚙️ [Configuration Guide](Docs/configuration.md) - Setup and configuration details
- 📊 [Excel Export Details](Docs/excel-export.md) - Output format and structure
- 📈 [Metric Statistics](Docs/metric-statistics.md) - Statistical calculations reference
- 📝 [Logging Guide](Docs/logging.md) - Logging configuration and usage
- 🗂️ [Folder Structure](Docs/folder-structure.md) - Output organization
- 🔌 [Extracted Endpoints](Docs/extracted-endpoints.md) - ArcGIS Monitor API integration
- ✅ [Documentation Status](Docs/documentation-completion-report.md) - Complete documentation report

### Documentation Features
- **IntelliSense Support**: Rich tooltips for main classes in Visual Studio
- **100% Coverage**: All public API documented
- **Code Examples**: Multiple real-world usage scenarios
- **Error Handling**: Comprehensive exception documentation
- **Best Practices**: Performance tips and recommendations
