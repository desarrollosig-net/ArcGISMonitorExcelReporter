# ArcGIS Monitor Excel Reporter - Solution Summary

## 📋 General Description

**ArcGIS Monitor Excel Reporter** is a .NET 8 console application and reusable library that
generates comprehensive Excel reports from ArcGIS Monitor data, including metrics, components,
services, alerts, and time-series health information.

## 🎯 Main Objective

Automate data extraction from ArcGIS Monitor and produce professional, multi-sheet Excel workbooks
that facilitate:
- 📊 Metrics and trend analysis
- 🔍 Auditing and regulatory compliance
- 📈 Performance tracking over configurable time windows
- 📋 System status documentation for stakeholders

---

## 🏗️ Architecture

The solution is composed of two projects:

### 1. **ArcGISMonitorExcelReporterLib** (Library)
Reusable .NET 8 class library. Main namespaces:
- 🔌 **Client** — HTTP communication with the ArcGIS Monitor REST API
- ⚙️ **Configuration** — JSON-serializable and programmatic configuration models
- 📦 **Models** — Domain entities (Collections, Components, Metrics, Alerts, etc.)
- 📝 **Reporting** — Report orchestration, data aggregation, and Excel rendering
- 📚 **Samples** — `ExampleUsage` class with ready-to-run demonstrations

### 2. **ArcGISMonitorExcelReporter** (Console Application)
Thin entry point that:
- Reads `config.json` (or a path passed as argument)
- Validates parameters via the library
- Invokes `ArcGISMonitorExcelReporter.GenerateExcelAsync()`
- Writes the `.xlsx` file and exits with a structured Serilog log

---

## 🔑 Main Features

| Feature | Description |
|---|---|
| **Complete Excel Reports** | Collections, Components, Metrics Summary, Alerts, and Time Series sheets |
| **Secure Authentication** | Plain-text or Base64-encoded passwords (`password_encoding`) |
| **Timezone Support** | IANA timezone identifiers for precise time window calculations |
| **Flexible Filtering** | By collection, component types, metric names (include/exclude lists) |
| **Time Series** | Historical data with configurable bucket (`metric_bucket`), top-N column selection by P95/Max |
| **Dual Configuration** | JSON file (`config.json`) or fully programmatic C# API |
| **Self-Contained Executables** | Single-file binaries for Windows (win-x64) and Linux (linux-x64) |
| **Automatic Versioning** | Daily build number system (`yyyy.MM.dd.BuildNumber`) |
| **CI/CD Ready** | GitHub Actions workflow for consistent multi-platform builds and publishing |

---

## 📦 Main Dependencies

```json
{
	"ClosedXML":        "Excel file generation (.xlsx)",
	"Serilog":          "Structured console logging",
	"System.Text.Json": "JSON configuration parsing",
	".NET 8.0":         "Base runtime and BCL"
}
```

---

## 🚀 Usage Flow

```
┌─────────────────────────────────────────────────────────────┐
│  User                                                       │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 1. Create config.json
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  ArcGISMonitorExcelReporter.exe                             │
├─────────────────────────────────────────────────────────────┤
│  1. Read configuration                                      │
│  2. Validate parameters                                     │
│  3. Authenticate with ArcGIS Monitor                        │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 4. Invoke ArcGISMonitorExcelReporterLib
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  ArcGISMonitorExcelReporterLib                              │
├─────────────────────────────────────────────────────────────┤
│  1. Connect to ArcGIS Monitor API                           │
│  2. Extract Collections                                     │
│  3. Extract Components                                      │
│  4. Extract Metrics                                         │
│  5. Fetch historical data (time-series)                     │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 6. Build Excel workbook
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  Report.xlsx                                                │
├─────────────────────────────────────────────────────────────┤
│  • Collections Sheet                                        │
│  • Components Sheet                                         │
│  • Metrics Summary Sheet                                    │
│  • Alerts Sheet                                             │
│  • Time Series Sheet (optional)                             │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Versioning System

### Format
```
yyyy.MM.dd.BuildNumber
```

### Behavior
- **Local Builds**: BuildNumber increments daily (1, 2, 3 …) and resets at midnight
- **CI Builds (GitHub Actions)**: BuildNumber tied to `github.run_number` for uniqueness
- **Multi-Platform CI**: Windows (win-x64) and Linux (linux-x64) share the same BuildNumber

### Control Files *(git-ignored)*
- `BuildNumber.txt` — current number
- `LastDatePrefix.txt` — date of the last build
- `BuildNumberFromCI.txt` — CI detection marker (prevents double-increment)

**Example**: `2026.06.30.13` = 13th build of June 30, 2026

---

## 🔄 CI/CD with GitHub Actions

### Workflow: `.github/workflows/release.yml`

```
┌────────────────────────────────┐
│  Trigger: Push release tag     │
└────┬───────────────────────────┘
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
Prevents `VersionInfo.targets` from incrementing the build number when building multiple platforms
(Windows and Linux) within the same workflow run, ensuring both executables share an identical
version string. See [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) for full details.

---