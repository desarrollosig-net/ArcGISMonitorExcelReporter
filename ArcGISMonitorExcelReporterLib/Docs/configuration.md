# Configuration

The library uses the `ArcGISMonitorExcelReporterLib.Configuration.Configuration` class as the primary input contract. This contract corresponds to the structure of the `agm2023x.json` file.

## Output Directories

The application automatically creates two folders relative to the configuration file location:

- **`reports/`**: Contains generated Excel reports with naming pattern `{config-name}_{yyyyMMdd_HHmm}.xlsx`
- **`logs/`**: Contains rolling log files with naming pattern `arcgis-monitor-reporter-{yyyy-MM-dd}.log`

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

## Loading

```csharp
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json");
```

## Execution

```csharp
var reporter = new ArcGISMonitorExcelReporter();
await reporter.GenerateExcelAsync(configuration, "ArcGISMonitorReport.xlsx");
```

## URL Normalization

If `server.url` ends with `/arcgis`, the library internally removes that suffix because client calls already use relative paths like `arcgis/auth/token`, `arcgis/monitoring/collections/query`, and `arcgis/monitoring/metrics/query`.

## Security

The actual configuration file with credentials should not be stored in a repository. For source control, `Samples/agm2023x.sample.json` should be used with substitute values.
