# ArcGIS Monitor Excel Reporter - Solution Summary

## 📋 General Description

**ArcGIS Monitor Excel Reporter** is a .NET 8 console application that generates Excel reports with complete data extracted from ArcGIS Monitor, including metrics, components, services, and system health information.

## 🎯 Main Objective

Automate data extraction from ArcGIS Monitor and generate professional Excel reports that facilitate:
- 📊 Metrics and trend analysis
- 🔍 Auditing and regulatory compliance
- 📈 Performance tracking
- 📋 System status documentation

## 🏗️ Architecture

The solution is composed of:

### 1. **ArcGISMonitorExcelReporterLib** (Library)
Main component contains:
- 🔌 **API Client** - Communication with ArcGIS Monitor
- ⚙️ **Configuration** - JSON/programmatic configuration models
- 📦 **Models** - Domain entities (Collections, Components, Metrics, etc.)
- 📝 **Reporting** - Report generation logic
- 📚 **Samples** - Usage examples

### 2. **ArcGISMonitorExcelReporter** (Console Application)
Entry point that:
- Reads configuration from `config.json`
- Validates parameters
- Invokes the library
- Generates the Excel file
- Provides structured logging

## 🔑 Main Features

| Feature | Description |
|---|---|
| **Complete Excel Reports** | Multiple sheets with formatted and structured data |
| **Secure Authentication** | Support for plain-text or Base64 passwords |
| **Timezone Support** | IANA timezone identifiers for precise calculations |
| **Flexible Filtering** | By collections, component types, metrics |
| **Time Series** | Historical data with configurable aggregation |
| **Dual Configuration** | JSON or programmatic configuration |
| **Self-Contained Executables** | Windows and Linux with no external dependencies |
| **Automatic Versioning** | Daily build number system (yyyy.MM.dd.BuildNumber) |
| **CI/CD Ready** | GitHub Actions for automated builds and publishing |

## 📦 Main Dependencies

```json
{
  "ClosedXML": "Excel file generation",
  "Serilog": "Structured logging",
  "System.Text.Json": "JSON serialization",
  ".NET 8.0": "Base runtime"
}
```

## 🚀 Flujo de Uso

```
┌─────────────────────────────────────────────────────────────┐
│  Usuario                                                    │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 1. Crea config.json
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  ArcGISMonitorExcelReporter.exe                             │
├─────────────────────────────────────────────────────────────┤
│  1. Read configuration                                      │
│  2. Validate parameters                                     │
│  3. Authenticate with ArcGIS Monitor                        │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 4. Invoca ArcGISMonitorExcelReporterLib
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  ArcGISMonitorExcelReporterLib                              │
├─────────────────────────────────────────────────────────────┤
│  1. Conecta a ArcGIS Monitor API                            │
│  2. Extrae Collections                                      │
│  3. Extrae Components                                       │
│  4. Extrae Metrics                                          │
│  5. Fetch historical data (time-series)                     │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 6. Construye libro de Excel
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  Report.xlsx                                                │
├─────────────────────────────────────────────────────────────┤
│  • Collections Sheet                                        │
│  • Components Sheet                                         │
│  • Metrics Summary Sheet                                    │
│  • Alerts Sheet                                             │
│  • Time Series Sheet (opcional)                             │
└─────────────────────────────────────────────────────────────┘
```

## ⚙️ Versioning System

### Format
```
yyyy.MM.dd.BuildNumber
```

### Behavior
- **Local Builds**: BuildNumber increments daily (1, 2, 3...) and resets at midnight
- **CI Builds (GitHub Actions)**: BuildNumber tied to `github.run_number` for uniqueness
- **Multi-Platform CI**: Windows (win-x64) and Linux (linux-x64) share the same BuildNumber

### Control Files
- `BuildNumber.txt` - Current number (git-ignored)
- `LastDatePrefix.txt` - Date of the last build (git-ignored)
- `BuildNumberFromCI.txt` - CI detection marker (git-ignored)

**Example**: `2025.01.27.3` = Third build of January 27, 2025

## 🔄 CI/CD with GitHub Actions

### Workflow: `.github/workflows/release.yml`

```
┌─────────────────────────────────┐
│  Trigger: Push release tag      │
└────┬────────────────────────────┘
	 │
	 ├─ Checkout code
	 ├─ Setup .NET 8
	 ├─ Calculate version (yyyy.MM.dd.BuildNumber)
	 ├─ Pre-populate BuildNumber.txt
	 ├─ Pre-populate LastDatePrefix.txt
	 ├─ Create BuildNumberFromCI.txt marker
	 │
	 ├─ Publish win-x64 (self-contained)
	 ├─ Package Windows artifacts
	 │
	 ├─ Restore BuildNumberFromCI.txt marker
	 ├─ Publish linux-x64 (self-contained)
	 ├─ Package Linux artifacts
	 │
	 ├─ Create GitHub release
	 └─ Upload artifacts to release
```

### Purpose of `BuildNumberFromCI.txt`
Prevents `VersionInfo.targets` from incrementing the build number when building multiple platforms (Windows and Linux) within the same workflow run. This ensures that both executables share the same version number.

## 📊 Generated Excel Structure

| Sheet | Content |
|-------|--------|
| **Collections** | List of monitored collections |
| **Components** | Individual components (hosts, databases, etc.) |
| **Metrics Summary** | Metrics summary with statistics (min, max, average) |
| **Alerts** | Active alerts and severity levels |
| **Time Series** | Historical metric data with timestamps |

**Format Features:**
- Color-coded headers
- Frozen panes for easy navigation
- Auto-adjusted column widths
- Appropriate numeric formatting for metrics

## 🔐 Security Configuration

### Authentication
```json
{
	"server": {
	"username": "admin",
	"password": "your_password",
	"password_encoding": false,
	"ignore_ssl_errors": false
	}
}
```

### Options
| Option | Value | Description |
|--------|-------|-------------|
| `password_encoding` | `true` | Password is Base64-encoded |
| `password_encoding` | `false` | Password is plain text |
| `ignore_ssl_errors` | `false` | Validate SSL certificates (RECOMMENDED) |
| `ignore_ssl_errors` | `true` | Ignore SSL errors (NOT RECOMMENDED for production) |

## 📋 System Requirements

| Component | Requirement |
|-----------|------------|
| **.NET Runtime** | 8.0 or later |
| **Platforms** | Windows x64, Linux x64 |
| **ArcGIS Monitor** | 2023.x or later |
| **Excel** | Any application that reads .xlsx (Excel, LibreOffice Calc, Google Sheets, etc.) |

## 📚 Available Documentation

| File | Purpose |
|---------|----------|
| **README.md** | Complete documentation in English |
| **CHANGELOG.md** | Change history and versions |
| **CONTRIBUTING.md** | Guide for contributing to the project |
| **LICENSE** | MIT License |
| **SUMMARY.md** | This file (summary in Spanish) |
| **BUILD_NUMBER_CI_FIX.md** | Technical explanation of the versioning fix |

## 🛠️ Development

### Projects in the Solution
- `ArcGISMonitorExcelReporterLib` - Main library (.NET 8)
- `ArcGISMonitorExcelReporter` - Console application (.NET 8)

### Build Locally
```bash
# Debug
dotnet build

# Release
dotnet build --configuration Release

# Publish for Windows
dotnet publish -c Release -r win-x64 --self-contained

# Publish for Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

### Code Style
- Microsoft C# conventions
- PascalCase for public members
- camelCase for local variables
- XML documentation for public APIs
- 4-space indentation

## 🐛 Common Troubleshooting

| Problem | Cause | Solution |
|---------|-------|----------|
| "Cannot connect to server" | Incorrect URL or unreachable server | Verify URL and network connectivity |
| "Invalid credentials" | Incorrect username/password | Verify credentials and permissions |
| "Timezone not found" | Invalid IANA identifier | Use valid identifiers (UTC, America/New_York, etc.) |
| "Out of memory" | Data set too large | Reduce `page_size`, `past_days`, or disable time-series |

## 📞 Support

- 📖 [Documentation](README.md)
- 🐛 [Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
- 💬 [Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)

## 📝 Quick Changelog

```
v2025.01.27.1
├─ BuildNumber fix in GitHub Actions
├─ Complete XML documentation in English
├─ README.md in English
├─ CHANGELOG.md
├─ CONTRIBUTING.md
├─ LICENSE (MIT)
└─ SUMMARY.md (this file)

v2025.01.20.1
└─ Initial release with full functionality
```

## 🎯 Roadmap

### Q1 2025
- [ ] Performance optimization for large datasets
- [ ] CSV export support
- [ ] Batch report generation

### Q2 2025
- [ ] Web API for remote generation
- [ ] Scheduled report execution
- [ ] Email delivery
- [ ] Caching system

### Q3 2025
- [ ] Custom report templates
- [ ] Azure integration
- [ ] PowerBI connector

### Q4 2025
- [ ] Dashboard generation
- [ ] Real-time monitoring integration
- [ ] Advanced analytics

## 📜 License

Licensed under the MIT License - See [LICENSE](LICENSE)

---

**Last Updated:** June 30, 2026
**Document Version:** 1.0  
**Project Status:** Active Development
