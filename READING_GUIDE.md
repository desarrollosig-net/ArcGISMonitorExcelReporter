# 📖 Recommended Reading Guide

Welcome to **ArcGIS Monitor Excel Reporter**. This guide will help you navigate the documentation based on your role and needs.

## 🎯 What is your role?

### 👤 I'm a User

**Goal:** Use the application to generate Excel reports

#### Recommended reading (45 minutes):
1. **[README.md](README.md)** - Introduction (5 min)
   - Read the "Features" section to understand what it does
   - Read "Quick Start - Method 1: JSON Configuration File"

2. **[config.json.example](config.json.example)** - See example (5 min)
   - Use this as a template for your configuration

3. **[README.md](README.md#configuration-reference)** - Reference (15 min)
   - Read the complete "Configuration Reference"
   - Customize parameters as needed

4. **[README.md](README.md#troubleshooting)** - Troubleshooting (10 min)
   - Save this section for future reference

5. **[README.md](README.md#output-excel-structure)** - Excel Format (10 min)
   - Understand what sheets your report will have

---

### 👨‍💼 I'm a Project Manager / Stakeholder

**Goal:** Understand what the project is and what it can do

#### Recommended reading (20 minutes):
1. **[SUMMARY.md](SUMMARY.md)** - Overview (10 min)
   - Read all of it - designed for executives

2. **[README.md](README.md#features)** - Features (5 min)
   - Understand the main capabilities

3. **[CHANGELOG.md](CHANGELOG.md)** - Evolution (5 min)
   - See the progress and future roadmap

---

### 👨‍💻 I'm a Developer

**Goal:** Modify, extend, or contribute code

#### Recommended reading (2-3 hours):

##### Phase 1: Setup (30 minutes)
1. **[CONTRIBUTING.md](CONTRIBUTING.md#development-setup)** - Local Setup
   - Install dependencies
   - Clone repository
   - Run `dotnet build`

2. **[README.md](README.md#project-structure)** - Structure
   - Understand the main directories

##### Phase 2: Understanding the Code (45 minutes)
3. **[README.md](README.md#building-from-source)** - Build
   - How to compile and run

4. **Code Navigation:**
   - Open `ArcGISMonitorExcelReporterLib/ArcGISMonitorExcelReporter.cs`
   - Read the XML comments (press Ctrl+K, Ctrl+I in VS)
   - Search for the `Configuration` class to see models

5. **[ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs](ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs)**
   - Read usage examples
   - Understand the two main methods

##### Phase 3: Standards Guide (30 minutes)
6. **[CONTRIBUTING.md](CONTRIBUTING.md#styleguides)** - Code Style
   - Read the "C# Style Guide" section
   - Read "Commit Messages"

7. **[CONTRIBUTING.md](CONTRIBUTING.md#testing)** - Testing
   - How to write tests
   - How to run tests

##### Phase 4: Contributing (45 minutes+)
8. **[CONTRIBUTING.md](CONTRIBUTING.md#pull-requests)** - PR Process
   - Follow the Pull Request workflow

---

### 🚀 I'm a DevOps / Release Manager

**Goal:** Build, publish, and maintain the infrastructure

#### Recommended reading (1.5 hours):

1. **[README.md](README.md#versioning--build-number-management)** - Versioning (15 min)
   - Understand the versioning system
   - Read how it is managed in CI/CD

2. **[BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md)** - Technical Fix (30 min)
   - Complete system detail
   - Reason for the CI marker

3. **[.github/workflows/release.yml](.github/workflows/release.yml)** - Workflow (20 min)
   - Read the annotated workflow
   - Understand each step

4. **[CONTRIBUTING.md](CONTRIBUTING.md#build-and-release)** - Local Build (20 min)
   - "Local Build" section
   - "Publishing" section

5. **Manual Build Test (15 min):**
   ```bash
   cd ArcGISMonitorExcelReporter
   dotnet publish -c Release -r win-x64 --self-contained
   dotnet publish -c Release -r linux-x64 --self-contained
   ```

---

## 📚 Full Reading by Topic

### If you want to...

#### 🔧 **Set up your first instance**
→ [README.md#quick-start](README.md#quick-start) + [config.json.example](config.json.example)
**Time:** 30 minutes

#### 🐛 **Fix a problem**
→ [README.md#troubleshooting](README.md#troubleshooting)
**Time:** 5-15 minutes

#### ✨ **Pitch the product**
→ [SUMMARY.md](SUMMARY.md)
**Time:** 20 minutes

#### 🤝 **Make your first pull request**
→ [CONTRIBUTING.md](CONTRIBUTING.md)
**Time:** 2-3 hours (includes setup)

#### 📊 **Understand the architecture**
→ [README.md#project-structure](README.md#project-structure) + [SUMMARY.md](SUMMARY.md#🏗️-arquitectura)
**Time:** 45 minutes

#### 🚀 **Make a full release**
→ [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) + [.github/workflows/release.yml](.github/workflows/release.yml)
**Time:** 1.5 hours

#### 📖 **Write documentation**
→ [CONTRIBUTING.md#documentation](CONTRIBUTING.md#documentation)
**Time:** Depends on the type

---

## 🗂️ Document Mind Map

```
						  ┌─ README.md ◄──── INICIO
						  │    (Oficial)
						  │
		  ┌───────────────┼───────────────┐
		  │               │               │
	  Usuario         Developer      DevOps/RM
		  │               │               │
		  ▼               ▼               ▼
	config.json.      CONTRIBUTING.   BUILD_NUMBER
	example           md + Code        _CI_FIX.md
		  │               │               │
		  │        ┌──────┼──────┐        │
		  │        │      │      │        │
		  │        ▼      ▼      ▼        │
	Troubleshooting  Tests  Styleguides  │
					 Writing            │
		  │          Code               │
		  │               │              │
		  └──────┬────────┴──────┬───────┘
				 │               │
				 ▼               ▼
		  CHANGELOG.md    LICENSE
				 │               │
				 └───────┬───────┘
						 │
						 ▼
				  SUMMARY.md (ES)
				  INDEX.md
```

---

## ⏱️ Total Reading Time

| Profile | Minimum | Recommended | Complete |
|---------|---------|-------------|----------|
| **User** | 30 min | 1 hour | 2 hours |
| **PM/Stakeholder** | 15 min | 30 min | 1 hour |
| **Developer** | 1.5 hours | 2-3 hours | 4+ hours |
| **DevOps/RM** | 45 min | 1.5 hours | 3 hours |

---

## ✅ Checklist by User Type

### ✓ User
- [ ] Read README intro
- [ ] Reviewed config.json.example
- [ ] Understand Configuration Reference
- [ ] Created my config.json
- [ ] Ran the app successfully
- [ ] Viewing the Excel report

### ✓ PM/Stakeholder
- [ ] Read SUMMARY.md in full
- [ ] Understand what the product does
- [ ] Know the roadmap
- [ ] Know who to contact for support

### ✓ Developer
- [ ] Set up my local environment
- [ ] Was able to build the solution
- [ ] Read CONTRIBUTING.md
- [ ] Understand the code structure
- [ ] Ran tests successfully
- [ ] Created a branch for changes
- [ ] Ready to open a PR

### ✓ DevOps/RM
- [ ] Understand the versioning system
- [ ] Read BUILD_NUMBER_CI_FIX.md
- [ ] Reviewed the GitHub Actions workflow
- [ ] Ran local test builds
- [ ] Understand how to make a release
- [ ] Know where to look when issues arise

---

## 🆘 If You're Stuck

### "I don't know where to start"
→ Read [INDEX.md](INDEX.md) - That's exactly what it's for

### "I can't find the answer"
→ Search in README.md with Ctrl+F for the keyword

### "I have a specific error"
→ Search in [README.md#troubleshooting](README.md#troubleshooting)

### "I want to contribute but don't know how"
→ Read [CONTRIBUTING.md#pull-requests](CONTRIBUTING.md#pull-requests)

### "I need to understand a technical concept"
→ Search in [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) or [README.md#project-structure](README.md#project-structure)

### "I still can't resolve it"
→ Open a [GitHub Issue](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)

---

## 🎓 Supplementary Learning Material

If you want to learn more about the technologies used:

- **[.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)** - Runtime
- **[C# 12 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)** - Language
- **[ArcGIS Monitor Documentation](https://doc.safe.com/arcgis-monitor/)** - API
- **[ClosedXML Wiki](https://github.com/ClosedXML/ClosedXML/wiki)** - Excel generation
- **[Serilog Documentation](https://github.com/serilog/serilog/wiki)** - Logging
- **[GitHub Actions Documentation](https://docs.github.com/en/actions)** - CI/CD

---

## 📅 Suggested Reading Schedule

### Day 1 (Start)
- Morning: Read README intro + Quick Start
- Afternoon: Create your first config

### Day 2 (Deep Dive)
- Morning: Read the complete Configuration Reference
- Afternoon: Run the app, adjust parameters

### Day 3 (Mastery)
- Morning: Read SUMMARY.md (understand the architecture)
- Afternoon: Review CHANGELOG (see evolution)

### If you go further (Development)
- Week 2: Dev setup, read the code
- Week 3: Contribute your first PR

---

## 💡 Final Tips

1. **Use the indexes:** Ctrl+F is your friend
2. **Follow the links:** Navigate between documents
3. **Check examples:** Search for `Example` in code
4. **Don't memorize:** Bookmark this guide for future reference
5. **Ask questions:** GitHub Discussions exist for that

---

**Welcome to ArcGIS Monitor Excel Reporter!** 🚀

We hope you find this documentation useful. If you have suggestions to improve it, please open an issue!

Last Updated: January 27, 2025
