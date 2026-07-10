# Documentation - ArcGIS Monitor Excel Reporter

## 📚 Documentation Index

This is the central documentation for the **ArcGIS Monitor Excel Reporter** project. Select the document you need based on your role or requirement.

---

## 🚀 Getting Started

### 👤 **End User**
You want to use the application to generate Excel reports

- **Quick Start:** [README.md](README.md#quick-start) - Step-by-step guides
- **Configuration:** [README.md](README.md#configuration-reference) - Complete parameter reference
- **Examples:** [config.json.example](config.json.example) - Sample configuration file
- **Troubleshooting:** [README.md](README.md#troubleshooting) - Common issues and solutions

---

## 💻 For Developers

### 🛠️ **Contributing to the Project**
You want to add features or fix bugs

1. **Read first:** [CONTRIBUTING.md](CONTRIBUTING.md) - Complete contribution guide
2. **Clone the repo:**
   ```bash
   git clone https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter.git
   cd ArcGISMonitorExcelReporter
   ```
3. **Local Setup:**
   - [CONTRIBUTING.md#development-setup](CONTRIBUTING.md#development-setup) - Configure environment
   - [CONTRIBUTING.md#styleguides](CONTRIBUTING.md#styleguides) - Code style
4. **Submit PR:**
   - [CONTRIBUTING.md#pull-requests](CONTRIBUTING.md#pull-requests) - PR process

### 📖 **Technical Documentation**
You need to understand the internal architecture

- **Architecture:** [README.md](README.md#project-structure) - Project structure
- **API Library:** XML documentation in `ArcGISMonitorExcelReporterLib/`
- **Samples:** [ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs](ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs)
- **Versioning:** [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) - Versioning system explanation
- **CI/CD:** [.github/workflows/release.yml](.github/workflows/release.yml) - GitHub Actions workflow

---

## 📋 Quick Document Reference

| Document | Purpose | Audience | Size |
|----------|---------|----------|------|
| **[README.md](README.md)** | Complete official documentation in English | Everyone | 13 KB |
| **[SUMMARY.md](SUMMARY.md)** | Executive summary in Spanish | Managers, Analysts | 12 KB |
| **[CHANGELOG.md](CHANGELOG.md)** | Change history and versions | Developers, Users | 7 KB |
| **[CONTRIBUTING.md](CONTRIBUTING.md)** | Guide for contributors | Developers | 11 KB |
| **[LICENSE](LICENSE)** | MIT License | Legal | 1 KB |
| **[BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md)** | Technical detail of the versioning fix | DevOps, Developers | Variable |
| **[config.json.example](config.json.example)** | Configuration template | Users, Developers | 1 KB |

---

## 🎯 Use Cases by Role

### 👨‍💼 **Project Manager / Stakeholder**
```
I want to read          Read first
   ↓
[SUMMARY.md]           Understand what it is and what it does
   ↓
[README.md] (Features) Learn the main capabilities
   ↓
[CHANGELOG.md]         See project progress
```

### 👨‍💻 **User / Operator**
```
I need to generate reports
   ↓
[README.md] Quick Start        Steps to get started
   ↓
[config.json.example]          Create my configuration
   ↓
[README.md] Troubleshooting    Resolve issues
```

### 👨‍🔬 **Developer / Contributor**
```
I want to contribute
   ↓
[CONTRIBUTING.md]              Development setup
   ↓
[README.md] Project Structure  Understand architecture
   ↓
Code with XML Docs             Read implementation
   ↓
[CONTRIBUTING.md] PR Process   Submit changes
```

### 🚀 **DevOps / Release Manager**
```
I need to understand versioning
   ↓
[BUILD_NUMBER_CI_FIX.md]       Build number system
   ↓
[.github/workflows/release.yml] CI/CD Workflow
   ↓
[CHANGELOG.md]                 Release history
```

---

## 📊 Repository Structure

```
ArcGISMonitorExcelReporter/
├── 📋 Documentation (at root)
│   ├── README.md                      ← READ FIRST (in English)
│   ├── SUMMARY.md                     ← Executive summary (in Spanish)
│   ├── CHANGELOG.md                   ← Version history
│   ├── CONTRIBUTING.md                ← Contribution guide
│   ├── LICENSE                        ← MIT License
│   ├── BUILD_NUMBER_CI_FIX.md         ← Technical detail
│   ├── INDEX.md                       ← This file
│   └── config.json.example            ← Config template
│
├── 📦 Source Code
│   ├── ArcGISMonitorExcelReporterLib/   ← Main library
│   │   ├── ArcGISMonitorExcelReporter.cs
│   │   ├── Client/                      ← API Client
│   │   ├── Configuration/               ← Config models
│   │   ├── Models/                      ← Domain models
│   │   ├── Reporting/                   ← Report generation
│   │   └── Samples/                     ← Examples with XML Docs
│   ├── ArcGISMonitorExcelReporter/      ← Console application
│   │   ├── Program.cs
│   │   ├── VersionInfo.targets          ← MSBuild versioning
│   │   └── GenerateVersionFile.ps1
│   └── *.Tests/                         ← Test projects
│
├── 🔧 Configuration
│   ├── .github/
│   │   └── workflows/
│   │       └── release.yml              ← CI/CD workflow
│   ├── .gitignore                       ← Git ignore rules
│   ├── ArcGISMonitorExcelReporter.slnx ← Solution
│   └── global.json                      ← .NET version
│
└── 📄 Other
	├── BUILD_NUMBER.txt                 ← Auto-generated
	├── LAST_DATE_PREFIX.txt            ← Auto-generated
	└── BuildNumberFromCI.txt            ← Temporary (CI)
```

---

## 🔍 Quick Search

### Question: "How do I configure the application?"
→ See [README.md#quick-start](README.md#quick-start) and [config.json.example](config.json.example)

### Question: "How do I contribute to the project?"
→ See [CONTRIBUTING.md](CONTRIBUTING.md)

### Question: "What's new in the latest version?"
→ See [CHANGELOG.md](CHANGELOG.md)

### Question: "Why doesn't the build number duplicate in CI?"
→ See [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md)

### Question: "Where is the license agreement?"
→ See [LICENSE](LICENSE)

### Question: "How do I troubleshoot issues?"
→ See [README.md#troubleshooting](README.md#troubleshooting)

---

## 🌐 Important Links

### Project Site
- 🐙 [GitHub Repository](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter)
- 📦 [NuGet Package](https://www.nuget.org/) (pending)
- 📖 [Official Documentation](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/docs)

### Communication
- 🐛 [Report Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
- 💬 [Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)
- 📧 [Contact](https://github.com/desarrollosig-net)

### External Resources
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [ArcGIS Monitor Documentation](https://doc.safe.com/arcgis-monitor/)
- [ClosedXML GitHub](https://github.com/ClosedXML/ClosedXML)
- [Serilog GitHub](https://github.com/serilog/serilog)

---

## ✅ Documentation Checklist

- ✓ README.md complete in English
- ✓ CHANGELOG.md with history
- ✓ CONTRIBUTING.md for contributors
- ✓ SUMMARY.md in Spanish
- ✓ MIT LICENSE
- ✓ config.json.example
- ✓ BUILD_NUMBER_CI_FIX.md (technical)
- ✓ INDEX.md (this file)
- ✓ XML Documentation in code
- ✓ Examples in Samples/

---

## 📞 Support

Can't find what you're looking for?

1. **Search** in [GitHub Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
2. **Ask** in [GitHub Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)
3. **Read** the full README.md - it's probably there
4. **Contribute** a documentation improvement

---

## 📅 Document Information

- **Created:** January 27, 2025
- **Last Updated:** January 27, 2025
- **Version:** 1.0
- **Maintainer:** DesarrolloSIG

---

**Thank you for your interest in ArcGIS Monitor Excel Reporter!** 🚀
