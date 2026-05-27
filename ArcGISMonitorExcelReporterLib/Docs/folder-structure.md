# Folder Structure Changes

## Summary

The ArcGIS Monitor Excel Reporter now creates organized output folders relative to the configuration file location.

## Changes Made

### Output Folders

Two folders are automatically created in the same directory as the configuration JSON file:

1. **`logs/`** - Contains rolling log files
   - Naming: `arcgis-monitor-reporter-{yyyy-MM-dd}.log`
   - One file per day
   - Includes detailed timestamps and log levels

2. **`reports/`** - Contains generated Excel reports
   - Naming: `{config-name}_{yyyyMMdd_HHmm}.xlsx`
   - Timestamp-based naming prevents file overwrites
   - Easy to identify when each report was generated

### Example Directory Structure

```
D:\ExcelReport\dist\
├── agm2023x.json                              (configuration file)
├── logs\
│   ├── arcgis-monitor-reporter-20250108.log  (today's logs)
│   └── arcgis-monitor-reporter-20250109.log  (tomorrow's logs)
└── reports\
    ├── agm2023x_20250108_1015.xlsx           (generated at 10:15)
    └── agm2023x_20250108_1430.xlsx           (generated at 14:30)
```

## Benefits

1. **Clean Organization**: All outputs are contained in predictable locations
2. **Relative Paths**: Works regardless of where the configuration file is located
3. **No Clutter**: Config directory stays clean with outputs in dedicated folders
4. **Easy Cleanup**: Simple to delete old reports or logs
5. **Multiple Configs**: Each config file gets its own logs and reports folders
6. **Version Control**: Config files can be versioned without worrying about outputs

## Implementation Details

### Program.cs
- Extracts config file directory path
- Creates `logs/` and `reports/` folders automatically
- Configures Serilog to write to the logs folder
- Generates Excel reports in the reports folder

### Logging Configuration
```csharp
var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configFilePath)) 
    ?? Directory.GetCurrentDirectory();

var logsFolder = Path.Combine(configDirectory, "logs");
Directory.CreateDirectory(logsFolder);

var reportsFolder = Path.Combine(configDirectory, "reports");
Directory.CreateDirectory(reportsFolder);
```

### Log Output Example
```
[10:15:32 INF] === ArcGIS Monitor Excel Reporter Started ===
[10:15:32 INF] Configuration file: D:\ExcelReport\dist\agm2023x.json
[10:15:32 INF] Reports folder: D:\ExcelReport\dist\reports
[10:15:32 INF] Logs folder: D:\ExcelReport\dist\logs
[10:15:32 INF] Output Excel file: D:\ExcelReport\dist\reports\agm2023x_20250108_1015.xlsx
```

## Updated Documentation

The following documentation files have been updated:
- `README.md` - Updated with folder structure examples
- `Docs/configuration.md` - Added output directories section
- `Docs/excel-export.md` - Added output location details
- `Docs/logging.md` - Updated with relative path configuration
- `Samples/LoggingExample.cs` - Updated examples to use relative paths

## Migration Notes

If you have existing configurations:

1. **Old behavior**: Files were created in the config directory or current working directory
2. **New behavior**: Files are created in `reports/` and `logs/` subdirectories

No action required - the folders are created automatically on first run.

## Testing

To verify the changes work correctly:

1. Place your config JSON file anywhere (e.g., `D:\ExcelReport\dist\agm2023x.json`)
2. Run the application
3. Check for:
   - `D:\ExcelReport\dist\logs\arcgis-monitor-reporter-{date}.log`
   - `D:\ExcelReport\dist\reports\agm2023x_{timestamp}.xlsx`
