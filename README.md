# ArcGIS Monitor Excel Reporter

A powerful .NET 8 console application that generates comprehensive Excel reports from ArcGIS Monitor data, including metrics, components, services, and system health information.

## Features

- 📊 **Comprehensive Excel Reports** - Generates detailed reports with collections, components, metrics, and time-series data
- 🔐 **Secure Authentication** - Supports both plain text and Base64-encoded passwords
- 🌍 **Timezone Support** - Full timezone-aware date/time calculations (IANA timezone identifiers)
- 🎯 **Flexible Filtering** - Query specific collections or all collections, filter by component types, include/exclude metrics
- 📈 **Time-Series Data** - Optional time-series metrics with configurable aggregation buckets
- ⚙️ **Dual Configuration** - Load from JSON files or configure programmatically
- 🏗️ **Self-Contained Publishing** - Single-file executables for Windows (win-x64) and Linux (linux-x64)
- 🚀 **Automatic Versioning** - Semantic versioning with automatic build number management (yyyy.MM.dd.BuildNumber)
- 🔄 **CI/CD Ready** - GitHub Actions workflow for automated building and publishing

## System Requirements

- **.NET Runtime:** .NET 8.0 or later
- **Platform:** Windows (x64) or Linux (x64)
- **ArcGIS Monitor:** 2023.x or later
- **Excel Support:** Any application that reads .xlsx files (Microsoft Excel, LibreOffice Calc, etc.)

## Installation

### From Release
Download the latest release from the [GitHub Releases](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/releases) page:
- `ArcGISMonitorExcelReporter-{version}-win-x64.zip` (Windows)
- `ArcGISMonitorExcelReporter-{version}-linux-x64.zip` (Linux)

Extract and run:
```bash
# Windows
ArcGISMonitorExcelReporter.exe

# Linux
./ArcGISMonitorExcelReporter
```

### From Source
```bash
git clone https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter.git
cd ArcGISMonitorExcelReporter
dotnet build --configuration Release
```

## Quick Start

### Method 1: JSON Configuration File

Create a configuration file `config.json`:

```json
{
  "server": {
	"url": "https://monitor.example.com:30443/arcgis",
	"username": "admin",
	"password": "your-password",
	"password_encoding": false,
	"timeout_seconds": 300
  },
  "report": {
	"collection": "Production",
	"timezone": "America/New_York",
	"end_time": { "now": true },
	"past_days": 7,
	"past_hours": 0,
	"types": ["host", "storage", "service", "database"],
	"metrics": {
	  "alerting_on_only": false,
	  "include_only": [],
	  "exclude_metrics": []
	}
  }
}
```

Run the application:
```bash
ArcGISMonitorExcelReporter
```

### Method 2: Programmatic Configuration

```csharp
using ArcGISMonitorExcelReporterLib;
using ArcGISMonitorExcelReporterLib.Configuration;

var configuration = new Configuration
{
	Server = new ServerConfiguration
	{
		Url = "https://monitor.example.com:30443/arcgis",
		Username = "admin",
		Password = "your-password",
		TimeoutSeconds = 300
	},
	Report = new ReportConfiguration
	{
		Collection = "Production",
		Timezone = "America/New_York",
		EndTime = new EndTimeConfiguration { Now = true },
		PastDays = 7,
		PastHours = 0,
		Types = new List<string> { "host", "storage", "service", "database" }
	}
};

var reporter = new ArcGISMonitorExcelReporter();
await reporter.GenerateExcelAsync(configuration, "Report.xlsx");
```

## Configuration Reference

### Server Section

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `url` | string | ✅ Yes | - | ArcGIS Monitor server URL (absolute URL with protocol) |
| `username` | string | ✅ Yes | - | Authentication username |
| `password` | string | ✅ Yes | - | Authentication password |
| `password_encoding` | bool | ❌ No | false | If true, password is Base64-encoded and will be decoded |
| `ignore_ssl_errors` | bool | ❌ No | true | Ignore SSL certificate errors (not recommended for production) |
| `timeout_seconds` | int | ❌ No | 300 | HTTP request timeout in seconds |

### Report Section

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `collection` | string | ❌ No | "" | Collection name (empty, "*", or null = all collections) |
| `timezone` | string | ❌ No | "UTC" | IANA timezone identifier (e.g., "America/New_York", "Europe/London") |
| `end_time` | object | ❌ No | {now: true} | Report end time (see EndTime section) |
| `past_days` | int | ❌ No | 0 | Number of days to include in report |
| `past_hours` | int | ❌ No | 0 | Additional hours to include (in addition to days) |
| `types` | array | ✅ Yes | - | Component types to include (e.g., "host", "storage", "service", "database") |
| `metrics` | object | ❌ No | - | Metrics filtering configuration (see Metrics section) |
| `page_size` | int | ❌ No | 100 | Pagination size for API requests |
| `metric_bucket` | string | ❌ No | "observed_at:15m" | Time-series aggregation bucket (e.g., "observed_at:1h") |
| `include_metric_time_series` | bool | ❌ No | true | Include metric time-series data in report |
| `max_metric_ids_for_time_series` | int | ❌ No | 5000 | Maximum metric IDs for time-series queries |

### EndTime Configuration

Specify when the report period ends:

```json
{
  "end_time": {
	"now": true
  }
}
```

Or with explicit date/time:

```json
{
  "end_time": {
	"now": false,
	"year": 2024,
	"month": 12,
	"day": 31,
	"hour": 23,
	"minute": 59,
	"second": 0
  }
}
```

### Metrics Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `alerting_on_only` | bool | false | Include only metrics with alerting rules enabled |
| `include_only` | array | [] | If non-empty, only these metrics are included (case-insensitive) |
| `exclude_metrics` | array | [] | Exclude these metrics from the report (case-insensitive) |

## Collection Filtering

The `collection` property supports three modes:

```json
// Mode 1: Query all collections (default)
"collection": ""

// Mode 2: Query all collections (explicit)
"collection": "*"

// Mode 3: Query specific collection
"collection": "Production"
```

## Versioning & Build Number Management

This project uses semantic versioning with automatic build number management:

**Version Format:** `yyyy.MM.dd.BuildNumber`

### Local Development
- Build number increments daily
- Resets to 1 at midnight (local timezone)
- Subsequent builds on the same day increment: 1, 2, 3, etc.

### GitHub Actions (CI/CD)
- Build number tied to `github.run_number` for uniqueness
- **Multiple builds in same workflow maintain consistency**
- Windows (win-x64) and Linux (linux-x64) builds use same version
- Pre-populated files prevent accidental increments between platform builds

**Example Versions:**
```
2025.01.27.1    - First build on January 27, 2025
2025.01.27.2    - Second build on same day
2025.01.28.1    - First build on January 28, 2025
```

### Build Files (Ignored)
These files are automatically generated and ignored by Git:
- `BuildNumber.txt` - Current build number
- `LastDatePrefix.txt` - Last build date
- `BuildNumberFromCI.txt` - Marker indicating GitHub Actions build (temporary)

To reset version numbering:
```bash
rm BuildNumber.txt LastDatePrefix.txt BuildNumberFromCI.txt
```

## Output Excel Structure

The generated Excel report contains multiple sheets:

| Sheet | Description |
|-------|-------------|
| **Collections** | List of all monitored collections |
| **Components** | Individual components (hosts, databases, etc.) with health status |
| **Metrics Summary** | Aggregated metrics with statistics (min, max, avg) |
| **Alerts** | Current alerts and their severity levels |
| **Time Series** | Historical metric data with timestamps (if enabled) |

Each sheet is formatted with:
- Header rows with background colors
- Frozen panes for easy navigation
- Auto-fitted column widths
- Number formatting for metrics

## Logging

The application uses [Serilog](https://serilog.net/) for structured logging:

```csharp
// Logs are written to console by default
// Configure custom logging in appsettings.json for file output
```

Example log output:
```
[12:34:56 INF] Validating configuration...
[12:34:56 INF] Configuration validated successfully
[12:34:56 INF] Creating ArcGIS Monitor client for URL: https://monitor.example.com:30443/arcgis
[12:34:57 INF] Authenticating with ArcGIS Monitor as user: admin
[12:34:57 INF] Authentication successful
[12:34:58 INF] Building report from 2025-01-20 12:34:56 to 2025-01-27 12:34:56 UTC
[12:35:02 INF] Report built successfully: 2 collections, 15 components, 450 metrics
[12:35:03 INF] Writing Excel file...
[12:35:04 INF] Excel file written successfully: Report.xlsx
```

## Error Handling

The application handles various error scenarios:

```csharp
// Configuration validation errors
- Missing required fields
- Invalid URLs
- Negative time values
- Invalid timezone identifiers

// Authentication errors
- Invalid credentials
- User account locked
- Missing permissions

// Network errors
- Connection timeouts
- SSL certificate errors
- Server not reachable

// File errors
- Excel file already in use
- Insufficient disk space
- Permission denied
```

## Project Structure

```
ArcGISMonitorExcelReporter/
├── ArcGISMonitorExcelReporterLib/          # Main library (NuGet package)
│   ├── ArcGISMonitorExcelReporter.cs       # Main entry point
│   ├── Client/                              # ArcGIS Monitor API client
│   ├── Configuration/                       # Configuration classes
│   ├── Models/                              # Data models
│   ├── Reporting/                           # Report generation
│   └── Samples/                             # Usage examples
├── ArcGISMonitorExcelReporter/              # Console application
│   ├── Program.cs                           # Entry point
│   ├── VersionInfo.targets                  # MSBuild versioning
│   ├── GenerateVersionFile.ps1              # Version file generation
│   └── config.json.sample                   # Configuration template
└── .github/
	└── workflows/
		└── release.yml                      # CI/CD pipeline
```

## Building from Source

### Prerequisites
- .NET 8.0 SDK or later
- PowerShell 5.1 or later (for version generation)

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build --configuration Release

# Publish for Windows
dotnet publish --configuration Release --runtime win-x64 --self-contained

# Publish for Linux
dotnet publish --configuration Release --runtime linux-x64 --self-contained

# Run locally
dotnet run --project ArcGISMonitorExcelReporter/ArcGISMonitorExcelReporter.csproj
```

## Development

### Adding New Features

1. **Library Changes** - Modify classes in `ArcGISMonitorExcelReporterLib/`
2. **Console App** - Update `ArcGISMonitorExcelReporter/Program.cs`
3. **Tests** - Add unit tests to `*.Tests` projects
4. **Version** - Automatically incremented on build

### Code Style

This project follows Microsoft C# coding conventions:
- PascalCase for class and method names
- camelCase for private fields
- Async methods suffixed with `Async`
- XML documentation comments for public APIs

### NuGet Dependencies

Key dependencies:
- **ClosedXML** - Excel file generation
- **Serilog** - Structured logging
- **System.Text.Json** - JSON serialization

## Troubleshooting

### "Unable to connect to server"
- Verify the server URL is correct and accessible
- Check network connectivity
- If using `ignore_ssl_errors: false`, ensure certificate is valid

### "Invalid credentials"
- Verify username and password
- If using `password_encoding: true`, ensure password is valid Base64
- Check user account permissions

### "Timezone not found"
- Use valid IANA timezone identifiers
- Common values: "UTC", "America/New_York", "Europe/London", "Asia/Tokyo"
- Platform-specific differences may apply (especially Windows)

### "Out of memory"
- Reduce `page_size` in configuration
- Reduce `past_days` to query less historical data
- Disable `include_metric_time_series` if not needed

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📖 [Documentation](ArcGISMonitorExcelReporterLib/Docs/)
- 🐛 [Issue Tracker](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
- 💬 [Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history and version notes.

## Credits

**ArcGIS Monitor Excel Reporter** is developed and maintained by [DesarrolloSIG](https://github.com/desarrollosig-net).

---

**Version:** 2026.06.14.3  
**Last Updated:** June 14, 2026
**Status:** Active Development
