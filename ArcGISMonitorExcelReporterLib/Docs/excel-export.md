# Excel Export

The project includes a reporting layer to save ArcGIS Monitor outputs to an `.xlsx` file using ClosedXML.

## Added Classes

- `Reporting/MonitorReportRequest.cs` *(included in `MonitorExcelReportModels.cs`)*: report input parameters.
- `Reporting/MonitorExcelReport`: normalized output container.
- `Reporting/MonitorReportService`: executes structured HTTP calls and builds the tabular model.
- `Reporting/MonitorExcelReportWriter`: writes the physical Excel file.

## Excel Structure

The generated file contains:

- `Summary`: initial sheet with metadata, counts, and index with internal links.
- `Collections`: tabular summary of queried collections.
- `Components`: consolidated component inventory.
- `Metrics`: consolidated metrics catalog.
- `Metric_Data`: aggregated data or time series of metrics (includes min, max, avg, stddev, percentile 95, sum, count).
- `Alerts`: alerts associated with metrics.
- `COL_*`: collection and component type-specific sheets.
- `MET_*`: metric name-specific sheets.

Sheet names are sanitized to comply with Excel restrictions: maximum 31 characters and exclusion of invalid characters such as `[]:*?/\`.

## Output Location

When using the console application, Excel reports are automatically saved to the `reports/` folder relative to the configuration file directory. The naming pattern is `{config-name}_{yyyyMMdd_HHmm}.xlsx`.

Example:
```
D:\ExcelReport\dist\
├── agm2023x.json              (configuration file)
└── reports\
    └── agm2023x_20250108_1015.xlsx
```

When using the library directly, you can specify any output path.

## Example

```csharp
using ArcGISMonitorExcelReporterLib.Client;
using ArcGISMonitorExcelReporterLib.Reporting;

using var client = new ArcGisMonitorClient(new Uri("https://monitor-server:30443/"));
await client.AuthenticateAsync(username, password);

var queries = new ArcGisMonitorQueryService(client);
var reportService = new MonitorReportService(queries);

var request = new MonitorReportRequest
{
    CollectionNames = ["Sample Collection"],
    ComponentTypes = ["host", "arcgis-server", "portal"],
    MetricNameLikes = ["CPU Utilized", "Memory Utilized"],
    FromUtc = DateTimeOffset.UtcNow.AddDays(-5),
    ToUtc = DateTimeOffset.UtcNow,
    IncludeMetricTimeSeries = true
};

await reportService.BuildAndSaveExcelAsync(request, @"C:\Reportes\monitor.xlsx");
```
