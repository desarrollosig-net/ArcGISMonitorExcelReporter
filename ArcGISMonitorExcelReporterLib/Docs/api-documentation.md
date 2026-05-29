# API Documentation

## Overview

This document provides comprehensive documentation for the ArcGIS Monitor Excel Reporter library's public API.

## Namespace: ArcGISMonitorExcelReporterLib

### ArcGISMonitorExcelReporter Class

Main entry point for generating Excel reports from ArcGIS Monitor data.

**Constructor:**
```csharp
public ArcGISMonitorExcelReporter(HttpClient? httpClient = null)
```
- `httpClient`: Optional HTTP client to use for requests. If null, a new client will be created internally.

**Methods:**

#### BuildReportAsync
```csharp
public async Task<MonitorExcelReport> BuildReportAsync(
    ReporterConfiguration configuration,
    CancellationToken cancellationToken = default)
```
Builds a report from ArcGIS Monitor without writing to Excel.

**Parameters:**
- `configuration`: Configuration object specifying server connection and report parameters
- `cancellationToken`: Optional cancellation token

**Returns:** A `MonitorExcelReport` object containing all queried data

**Throws:**
- `ArgumentNullException`: If configuration is null
- `InvalidOperationException`: If configuration is invalid or authentication fails
- `HttpRequestException`: If communication with ArcGIS Monitor fails

#### GenerateExcelAsync
```csharp
public async Task<string> GenerateExcelAsync(
    ReporterConfiguration configuration,
    string outputExcelPath,
    CancellationToken cancellationToken = default)
```
Builds a report and writes it to an Excel file.

**Parameters:**
- `configuration`: Configuration object
- `outputExcelPath`: Full path where the Excel file should be written
- `cancellationToken`: Optional cancellation token

**Returns:** The path to the generated Excel file

**Throws:**
- `ArgumentException`: If outputExcelPath is null or empty
- All exceptions from `BuildReportAsync`

#### GenerateExcelFromConfigurationFileAsync
```csharp
public async Task<string> GenerateExcelFromConfigurationFileAsync(
    string configurationPath,
    string outputExcelPath,
    CancellationToken cancellationToken = default)
```
Loads configuration from a JSON file and generates an Excel report.

**Parameters:**
- `configurationPath`: Path to the JSON configuration file
- `outputExcelPath`: Full path where the Excel file should be written
- `cancellationToken`: Optional cancellation token

**Returns:** The path to the generated Excel file

---

## Namespace: ArcGISMonitorExcelReporterLib.Configuration

### Configuration Class

Represents the complete configuration for report generation, including server connection and report parameters.

**Properties:**
- `Server`: Server configuration (URL, credentials, SSL settings)
- `Report`: Report configuration (collections, types, time range, filters)

**Methods:**

#### LoadAsync
```csharp
public static async Task<Configuration> LoadAsync(
    string path, 
    CancellationToken cancellationToken = default)
```
Loads configuration from a JSON file asynchronously.

#### Load
```csharp
public static Configuration Load(string path)
```
Loads configuration from a JSON file synchronously.

#### Validate
```csharp
public void Validate()
```
Validates the configuration and throws exceptions if invalid.

**Throws:**
- `InvalidOperationException`: For any validation error with descriptive message

#### ToReportRequest
```csharp
public MonitorReportRequest ToReportRequest()
```
Converts the configuration to a report request with resolved time ranges.

### ServerConfiguration Class

Server connection settings.

**Properties:**
- `Url`: ArcGIS Monitor base URL
- `Username`: Authentication username
- `Password`: Password (plain text or Base64 encoded)
- `PasswordEncoding`: If true, password is Base64 UTF-8 encoded
- `IgnoreSslErrors`: If true, SSL certificate validation is disabled (default: true)
- `TimeoutSeconds`: HTTP request timeout in seconds (default: 300). Use -1 for infinite timeout

**Methods:**

#### GetPassword
```csharp
public string GetPassword()
```
Returns the decoded password.

**Throws:**
- `InvalidOperationException`: If password encoding is enabled but password is not valid Base64

### ReportConfiguration Class

Report generation settings.

**Properties:**
- `Collection`: Collection name to query
- `Timezone`: Timezone for time range calculations (default: "UTC")
- `EndTime`: End time configuration
- `PastDays`: Number of days backward from end time
- `PastHours`: Number of hours backward from end time
- `Types`: List of component types to include
- `Metrics`: Metric filtering configuration
- `PageSize`: Page size for paginated queries (default: 100)
- `MetricBucket`: Time bucket for aggregation (default: "observed_at:15m")
- `IncludeMetricTimeSeries`: Whether to fetch time series data (default: true)
- `MaxMetricIdsForTimeSeries`: Maximum metrics for time series (default: 5000)

### EndTimeConfiguration Class

End time specification for reports.

**Properties:**
- `Now`: If true, use current time
- `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`: Explicit date/time components

**Methods:**

#### Resolve
```csharp
public DateTimeOffset Resolve(TimeZoneInfo timezone)
```
Resolves the end time to a DateTimeOffset in the specified timezone.

### MetricsConfiguration Class

Metric filtering settings.

**Properties:**
- `AlertingOnOnly`: If true, include only metrics with alerting enabled
- `IncludeOnly`: List of metric name patterns to include (LIKE matching)
- `ExcludeMetrics`: List of metric name patterns to exclude

---

## Namespace: ArcGISMonitorExcelReporterLib.Client

### ArcGisMonitorQueryService Class

High-level service for querying ArcGIS Monitor with pagination support.

**Constructor:**
```csharp
public ArcGisMonitorQueryService(ArcGisMonitorClient client)
```

**Methods:**

#### CountComponentsAsync
```csharp
public async Task<int> CountComponentsAsync(
    string collectionName,
    string componentType,
    DateTimeOffset fromUtc,
    DateTimeOffset toUtc,
    CancellationToken cancellationToken = default)
```
Gets the count of components matching the criteria.

#### GetComponentsAsync
```csharp
public async Task<List<ComponentFeature>> GetComponentsAsync(
    string collectionName,
    string componentType,
    DateTimeOffset fromUtc,
    DateTimeOffset toUtc,
    int pageSize = 100,
    CancellationToken cancellationToken = default)
```
Gets components with pagination, including logs, labels, parents, and agents.

#### GetComponentsWithAllMetricsAsync
```csharp
public async Task<List<ComponentFeature>> GetComponentsWithAllMetricsAsync(
    string collectionName,
    string componentType,
    int pageSize = 100,
    CancellationToken cancellationToken = default)
```
Gets components with all their metrics.

#### GetComponentsWithMetricStatsAsync
```csharp
public async Task<List<ComponentFeature>> GetComponentsWithMetricStatsAsync(
    string collectionName,
    string componentType,
    string metricNameLike,
    DateTimeOffset fromUtc,
    DateTimeOffset toUtc,
    int pageSize = 100,
    CancellationToken cancellationToken = default)
```
Gets components filtered by metric name pattern with aggregated statistics.

#### GetMetricTimeSeriesAsync
```csharp
public async Task<QueryResponse<MetricFeature>> GetMetricTimeSeriesAsync(
    IEnumerable<int> metricIds,
    DateTimeOffset fromUtc,
    DateTimeOffset toUtc,
    string bucket = "observed_at:15m",
    CancellationToken cancellationToken = default)
```
Gets time series data for specified metric IDs with time-based aggregation.

---

## Namespace: ArcGISMonitorExcelReporterLib.Builders

### MonitorQueryBuilders Class

Static helper class for building ArcGIS Monitor query requests.

**Methods:**

#### CollectionComponents
```csharp
public static CollectionQueryRequest CollectionComponents(
    string collectionName,
    string componentType,
    bool returnCountOnly,
    int resultRecordCount = 100,
    int resultOffset = 0,
    DateTimeOffset? fromUtc = null,
    DateTimeOffset? toUtc = null,
    bool includeLogs = true,
    bool includeLabels = true,
    bool includeParents = true,
    bool includeAgents = true,
    bool includeMetricsObserver = true)
```
Builds a query for components with optional related data (logs, labels, etc.).

#### CollectionComponentsWithAllMetrics
```csharp
public static CollectionQueryRequest CollectionComponentsWithAllMetrics(
    string collectionName,
    string componentType,
    bool returnCountOnly,
    int resultRecordCount = 100,
    int resultOffset = 0)
```
Builds a query for components with all their metrics.

#### CollectionComponentsByMetricName
```csharp
public static CollectionQueryRequest CollectionComponentsByMetricName(
    string collectionName,
    string componentType,
    string metricNameLike,
    DateTimeOffset fromUtc,
    DateTimeOffset toUtc,
    bool returnCountOnly,
    int resultRecordCount = 100,
    int resultOffset = 0)
```
Builds a query for components filtered by metric name with aggregated statistics.

#### MetricsTimeSeries
```csharp
public static MetricQueryRequest MetricsTimeSeries(
    IEnumerable<int> metricIds,
    DateTimeOffset fromUtc,
    DateTimeOffset toUtc,
    string bucket = "observed_at:15m")
```
Builds a query for metric time series data with time-based grouping.

#### Helper Methods
- `BetweenTimestamp(string fieldName, DateTimeOffset fromUtc, DateTimeOffset toUtc)`: Creates a SQL BETWEEN clause for timestamps
- `AlertOverlapsWhere(DateTimeOffset fromUtc, DateTimeOffset toUtc)`: Creates a WHERE clause for alerts that overlap the time range
- `FormatMonitorTimestamp(DateTimeOffset value)`: Formats a timestamp for Monitor API queries
- `EscapeSqlLiteral(string value)`: Escapes single quotes in SQL string literals

---

## Namespace: ArcGISMonitorExcelReporterLib.Reporting

### MonitorReportService Class

Service for building monitor reports from query results.

**Constructor:**
```csharp
public MonitorReportService(ArcGisMonitorQueryService queries)
```

**Methods:**

#### BuildReportAsync
```csharp
public async Task<MonitorExcelReport> BuildReportAsync(
    MonitorReportRequest request,
    CancellationToken cancellationToken = default)
```
Builds a complete report by querying ArcGIS Monitor, applying filters, and organizing data.

**Process:**
1. Validates request parameters
2. Queries components and metrics for each collection/type combination
3. Applies metric filters
4. Fetches time series data if requested
5. Returns normalized report data

#### BuildAndSaveExcelAsync
```csharp
public async Task BuildAndSaveExcelAsync(
    MonitorReportRequest request,
    string outputPath,
    CancellationToken cancellationToken = default)
```
Builds a report and saves it directly to Excel.

### MonitorExcelReportWriter Class

Writes MonitorExcelReport data to Excel files using ClosedXML.

**Methods:**

#### Save
```csharp
public void Save(MonitorExcelReport report, string outputPath)
```
Writes the report to an Excel file with multiple sheets.

**Excel Structure:**
- **Summary**: Overview with metadata, counts, and index
- **Collections**: Collection summary table
- **Components**: Component inventory
- **Metrics**: Metrics catalog with alerting configuration
- **Metric_Data**: Time series or aggregated metric statistics
- **Alerts**: Alert history
- **COL_*** sheets: Per-collection detail sheets
- **MET_*** sheets: Per-metric detail sheets

**Features:**
- Automatic sheet name sanitization (31 char limit, invalid characters removed)
- Excel tables with filtering enabled
- Internal hyperlinks between sheets
- Appropriate data type formatting (dates, numbers)
- Row counts logged for each sheet

---

## Models

### Query Models (QueryModels.cs)

#### CollectionQueryRequest / MetricQueryRequest
Request objects for querying collections or metrics.

**Properties:**
- `Where`: SQL WHERE clause
- `Including`: List of included resources with filters

#### CollectionIncludeSpec / MetricIncludeSpec
Specification for including related resources in queries.

**Properties:**
- `Resource`: Resource type (e.g., "components", "metrics", "metrics_data")
- `Where`: Filter clause
- `ReturnCountOnly`: If true, return only count
- `ResultRecordCount`: Page size
- `ResultOffset`: Pagination offset
- `Including`: Nested includes
- `GroupbyFieldsForStatistics` (Collection) / `GroupByFieldsForStatistics` (Metric): Grouping field(s)
- `OutStatistics`: List of statistics to calculate

#### OutStatistic
Statistical aggregation configuration.

**Properties:**
- `StatisticType`: List of statistics ("count", "min", "max", "avg", "stddev", "percentile_95", "sum")
- `OnStatisticField`: Field to aggregate (typically "value")

### Response Models (ResponseModels.cs)

#### QueryResponse<TFeature>
Generic response wrapper from ArcGIS Monitor API.

**Properties:**
- `Features`: List of feature objects
- `ExceededTransferLimit`: Indicates if result set was truncated

#### CollectionFeature
Represents a collection with components.

**Properties:**
- `Attributes`: Collection metadata
- `Components`: Component results (count or list)

#### ComponentFeature
Represents a component with related data.

**Properties:**
- `Attributes`: Component metadata
- `Metrics`: List of metrics
- `Labels`, `Parents`, `Agents`, `ComponentLogs`, `Observers`: Related data

#### MetricFeature
Represents a metric with data and alerts.

**Properties:**
- `Attributes`: Metric metadata
- `MetricsData`: Time series or aggregated data points
- `Alerts`: Associated alerts

#### Attribute Classes
- `CollectionAttributes`: Collection metadata
- `ComponentAttributes`: Component metadata (ID, name, type, version, memory, CPU, etc.)
- `MetricAttributes`: Metric metadata (ID, name, unit, thresholds, alerting config)
- `MetricDataAttributes`: Time series data point (min, max, avg, stddev, percentile_95, sum, count)
- `AlertAttributes`: Alert information (state, status, thresholds, duration, timestamps)
- `LabelAttributes`, `AgentAttributes`: Additional metadata

---

## Usage Examples

### Basic Usage
```csharp
using ArcGISMonitorExcelReporterLib;
using Config = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var config = await Config.LoadAsync("config.json");
var reporter = new ArcGISMonitorExcelReporter();
await reporter.GenerateExcelAsync(config, "report.xlsx");
```

### Programmatic Configuration
```csharp
var config = new Config
{
    Server = new ServerConfiguration
    {
        Url = "https://monitor.example.com:30443/",
        Username = "admin",
        Password = "password",
        IgnoreSslErrors = true
    },
    Report = new ReportConfiguration
    {
        Collection = "Production",
        Timezone = "America/New_York",
        EndTime = new EndTimeConfiguration { Now = true },
        PastDays = 7,
        Types = ["host", "service"],
        Metrics = new MetricsConfiguration
        {
            IncludeOnly = ["CPU", "Memory"],
            AlertingOnOnly = false
        }
    }
};

var reporter = new ArcGISMonitorExcelReporter();
await reporter.GenerateExcelAsync(config, "report.xlsx");
```

### Using the Client Directly
```csharp
using var client = new ArcGisMonitorClient(
    new Uri("https://monitor.example.com:30443/"));
    
await client.AuthenticateAsync("admin", "password");

var queryService = new ArcGisMonitorQueryService(client);
var components = await queryService.GetComponentsWithAllMetricsAsync(
    "Production", "host", pageSize: 100);

foreach (var component in components)
{
    Console.WriteLine($"{component.Attributes.Name}: {component.Metrics?.Count ?? 0} metrics");
}
```

---

## Error Handling

Common exceptions:

- `ArgumentNullException`, `ArgumentException`: Invalid parameters
- `InvalidOperationException`: Configuration errors, authentication failures, expired tokens
- `HttpRequestException`: Network errors, HTTP errors from ArcGIS Monitor
- `JsonException`: Deserialization errors
- `IOException`: File system errors when reading config or writing Excel

Best practices:
```csharp
try
{
    var reporter = new ArcGISMonitorExcelReporter();
    await reporter.GenerateExcelAsync(config, outputPath);
    Log.Information("Report generated successfully");
}
catch (InvalidOperationException ex)
{
    Log.Error(ex, "Configuration or authentication error");
}
catch (HttpRequestException ex)
{
    Log.Error(ex, "Failed to communicate with ArcGIS Monitor");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unexpected error");
    throw;
}
```

---

## Threading and Async

- All I/O operations are async
- Methods accept `CancellationToken` for cancellation support
- The library is thread-safe for read operations
- `ArcGisMonitorClient` instances should not be shared across threads during authentication

---

## Performance Considerations

- **Pagination**: Queries use pagination (default 100 records per page) to handle large datasets
- **Time Series Limit**: By default, time series data is fetched for up to 5000 metrics (configurable)
- **Parallel Queries**: The library queries collections and component types sequentially
- **Memory**: Large reports can consume significant memory; consider filtering metrics if memory is constrained
- **Network**: Minimize network round-trips by including related resources in queries

---

## See Also

- [Configuration Guide](configuration.md)
- [Excel Export Details](excel-export.md)
- [Metric Statistics](metric-statistics.md)
- [Logging Guide](logging.md)
