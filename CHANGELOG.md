# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Support for additional export formats (CSV, JSON)
- Batch report generation
- Scheduled report execution
- Custom report templates
- Email delivery of reports
- Report caching for improved performance

## [2025.01.27.1] - 2025-01-27

### Added
- ✨ **BuildNumber Persistence Fix** - Resolved issue where build numbers would duplicate when compiling multiple platforms (Windows/Linux) in the same GitHub Actions workflow
  - Implemented CI marker file detection (`BuildNumberFromCI.txt`)
  - Added conditional logic in MSBuild targets to prevent increment during CI builds
  - Ensured version consistency across all platform builds
  - See [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) for technical details

- 📖 **Comprehensive Documentation**
  - Complete XML documentation comments for all public classes and methods in English
  - Added `ExampleUsage` class with two demonstration methods:
	- `GenerateFromJsonAsync()` - JSON configuration based reporting
	- `GenerateFromConfigurationObjectAsync()` - Programmatic configuration
  - Detailed <remarks>, <param>, <exception>, and <example> tags for better IntelliSense

- 📚 **Enhanced Configuration Classes**
  - Added thorough documentation to `ServerConfiguration` 
  - Added thorough documentation to `ReportConfiguration`
  - Added thorough documentation to `EndTimeConfiguration`
  - Added thorough documentation to `MetricsConfiguration`
  - Documented all properties with usage examples and default values

- 📋 **Project Documentation**
  - Created comprehensive `README.md` with full feature set description
  - Created `README.md` covering installation, usage, configuration, troubleshooting
  - Added system requirements and prerequisites
  - Added quick start guides (JSON and programmatic)

### Changed
- 🔧 Updated `VersionInfo.targets` with CI-aware build logic
  - Detects GitHub Actions environment via marker file
  - Prevents build number increment when building multiple platforms sequentially
  - Maintains backward compatibility with local development

- 🔄 Updated `.github/workflows/release.yml` to support consistent versioning
  - Pre-populates `BuildNumber.txt` and `LastDatePrefix.txt` from `github.run_number`
  - Creates `BuildNumberFromCI.txt` marker for build detection
  - Restores marker before each platform-specific build

### Fixed
- 🐛 **BuildNumber Duplication** - Fixed issue where Linux build would increment build number unnecessarily
- 🐛 **Version Inconsistency** - Ensured both Windows and Linux releases use identical version numbers

### Technical Details

#### Version Management System
- **Format:** `yyyy.MM.dd.BuildNumber`
- **Local Builds:** Increments daily (1, 2, 3... resets at midnight)
- **CI Builds:** Uses `github.run_number` for uniqueness
- **Multi-Platform CI:** Implements sequential build with marker restoration to prevent duplication

#### Build Files
- `BuildNumber.txt` - Current build number (git-ignored)
- `LastDatePrefix.txt` - Last build date prefix (git-ignored)
- `BuildNumberFromCI.txt` - CI marker file (temporary, git-ignored)

## [2025.01.20.5] - 2025-01-20

### Added
- Initial public release
- Core functionality for generating Excel reports from ArcGIS Monitor
- Support for Windows (win-x64) and Linux (linux-x64) platforms
- JSON-based configuration system
- Programmatic configuration API
- Comprehensive error handling
- Structured logging with Serilog
- Self-contained executable distribution

### Features
- 📊 Complete data extraction from ArcGIS Monitor
- 🏗️ Well-formatted Excel workbooks with multiple sheets
- 🔐 Secure authentication (plain text and Base64-encoded passwords)
- 🌍 Full timezone support with IANA timezone identifiers
- 🎯 Flexible filtering by collections, component types, and metrics
- 📈 Optional time-series data with configurable aggregation
- ⚙️ Multiple configuration methods (JSON files or programmatic)
- 🚀 GitHub Actions CI/CD workflow for automated builds
- 📦 Single-file executable publishing for ease of distribution

### Documentation
- `VERSION.md` - Versioning system documentation
- `ArcGISMonitorExcelReporterLib/Docs/` - Comprehensive API documentation
- Quick start examples and configuration samples

---

## Version Format Legend

```
2025.01.27.1
└─┬─ Year (2025)
  └─┬─ Month (01)
	└─┬─ Day (27)
	  └─ Build Number (1)
```

- **Resets daily** - Build number resets to 1 each day
- **GitHub Actions** - Uses `github.run_number` for unique, consistent versioning
- **Release naming** - GitHub tags follow format `v2025.01.27.1`

## Release Timeline

- **2025-01-20** - Initial public release (v2025.01.20.1+)
- **2025-01-27** - BuildNumber persistence fix and documentation enhancement
- **Future** - Additional features and platform support planned

## Compatibility

### .NET Runtime
- ✅ .NET 8.0
- ✅ .NET 9.0 (tested)
- ⚠️ .NET 7.0 and earlier (not supported)

### Operating Systems
- ✅ Windows 10+ (x64)
- ✅ Windows Server 2019+ (x64)
- ✅ Ubuntu 20.04+ (x64)
- ✅ CentOS 8+ (x64)
- ✅ Red Hat Enterprise Linux 8+ (x64)
- ⚠️ Other Linux distributions (not tested, but likely compatible)

### ArcGIS Monitor
- ✅ 2023.x
- ✅ 2024.x
- ⚠️ Earlier versions (compatibility not verified)

### Excel Compatibility
- ✅ Microsoft Excel 2016+
- ✅ Microsoft Excel 365
- ✅ LibreOffice Calc 7.0+
- ✅ Google Sheets (via import)
- ⚠️ Older Excel versions may have limited support

## Known Issues

None currently known. Please report issues on [GitHub Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues).

## Roadmap

### Q1 2025
- [ ] Performance optimization for large datasets
- [ ] Support for CSV export format
- [ ] Batch report generation capabilities

### Q2 2025
- [ ] Web API for remote report generation
- [ ] Scheduled report execution
- [ ] Email delivery of reports
- [ ] Report caching system

### Q3 2025
- [ ] Custom report templates
- [ ] Azure integration
- [ ] PowerBI connector

### Q4 2025
- [ ] Dashboard generation
- [ ] Real-time monitoring integration
- [ ] Advanced analytics

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support, please:
1. Check the [Troubleshooting section](README.md#troubleshooting) in README.md
2. Review existing [GitHub Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
3. Create a new issue with detailed information
4. Join [GitHub Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)

---

**Last Updated:** January 27, 2025  
**Project Status:** Active Development  
**Latest Version:** 2025.01.27.1
