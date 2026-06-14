# Contributing to ArcGIS Monitor Excel Reporter

First off, thank you for considering contributing to ArcGIS Monitor Excel Reporter! It's people like you that make this tool such a great resource.

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the [issue list](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues) as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

* **Use a clear and descriptive title**
* **Describe the exact steps which reproduce the problem**
* **Provide specific examples to demonstrate the steps**
* **Include screenshots if applicable**
* **Describe the behavior you observed after following the steps**
* **Explain which behavior you expected to see instead and why**
* **Include your environment details:**
  - Operating System and version
  - .NET Runtime version (output of `dotnet --version`)
  - Application version (check the version in your config or output log)

### Suggesting Enhancements

Enhancement suggestions are tracked as [GitHub issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues). When creating an enhancement suggestion, please include:

* **Use a clear and descriptive title**
* **Provide a step-by-step description of the suggested enhancement**
* **Provide specific examples to demonstrate the steps**
* **Describe the current behavior and expected behavior**
* **Explain why this enhancement would be useful**

### Pull Requests

* Follow the [styleguides](#styleguides) below
* Include appropriate test cases if adding new features
* Update documentation accordingly
* End all files with a newline
* Write unit tests for new code

#### Pull Request Process

1. Fork the repository and create your branch from `main`
2. If you've added code that should be tested, add tests
3. Update documentation as needed
4. Ensure the test suite passes (`dotnet test`)
5. Ensure you follow the styleguides
6. Create your PR with a clear description of changes
7. Link any related issues to your PR

## Development Setup

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Git](https://git-scm.com/)
- [Visual Studio](https://visualstudio.microsoft.com/), [Visual Studio Code](https://code.visualstudio.com/), or your preferred C# editor
- PowerShell 5.1 or later

### Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter.git
   cd ArcGISMonitorExcelReporter
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

4. **Run tests:**
   ```bash
   dotnet test
   ```

5. **Run the application locally:**
   ```bash
   dotnet run --project ArcGISMonitorExcelReporter/ArcGISMonitorExcelReporter.csproj
   ```

### Project Structure

```
ArcGISMonitorExcelReporter/
├── ArcGISMonitorExcelReporterLib/          # Main library (NuGet package)
│   ├── ArcGISMonitorExcelReporter.cs       # Main API class
│   ├── Client/                             # ArcGIS Monitor API client
│   ├── Configuration/                      # Configuration models
│   ├── Models/                             # Domain models
│   ├── Reporting/                          # Report generation logic
│   └── Samples/                            # Usage examples
├── ArcGISMonitorExcelReporter/             # Console application
│   ├── Program.cs                          # Entry point
│   ├── VersionInfo.targets                 # MSBuild versioning
│   ├── GenerateVersionFile.ps1             # Version generation script
│   └── config.json                         # Configuration file
└── .github/workflows/                      # CI/CD workflows
	└── release.yml                         # Release workflow
```

## Styleguides

### C# Style Guide

We follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

#### Key Points:

- **Naming Conventions:**
  - `PascalCase` for `public` members, `class` names, and method names
  - `camelCase` for local variables and private fields
  - `_camelCase` for private fields (with underscore prefix)
  - `UPPER_CASE` for constants

- **Formatting:**
  - Use 4 spaces for indentation (not tabs)
  - Opening braces on the same line (Allman style)
  - One class per file
  - Maximum line length: 120 characters (aim for ~100 for readability)

- **Comments:**
  - Use `//` for single-line comments
  - Use `/* */` for multi-line comments
  - Use `///` for XML documentation comments on public members
  - Comments should be clear and concise

- **Example:**
  ```csharp
  /// <summary>
  /// Generates an Excel report from ArcGIS Monitor data.
  /// </summary>
  /// <param name="configuration">The report configuration.</param>
  /// <param name="outputPath">Path where the Excel file will be saved.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
  public async Task GenerateExcelAsync(Configuration configuration, string outputPath)
  {
	  if (configuration == null)
	  {
		  throw new ArgumentNullException(nameof(configuration));
	  }

	  // Implementation
  }
  ```

### Commit Messages

- Use the present tense: "Add feature" not "Added feature"
- Use the imperative mood: "Move cursor to..." not "Moves cursor to..."
- Limit the first line to 72 characters or less
- Reference issues and pull requests liberally after the first line
- Consider starting the commit message with an emoji:
  - 🎨 `:art:` when improving the format/structure of the code
  - ⚡ `:zap:` when improving performance
  - 🐛 `:bug:` when fixing a bug
  - ✨ `:sparkles:` when adding a new feature
  - 📚 `:books:` when writing docs
  - 🚀 `:rocket:` when deploying stuff
  - 🔧 `:wrench:` when updating configurations
  - 🔄 `:repeat:` when refactoring code
  - ✅ `:white_check_mark:` when adding tests
  - 🎯 `:dart:` when targeting a specific fix

### Example Commits

```
✨ Add support for custom report templates

- Implement custom template engine
- Add template validation
- Update documentation
- Add unit tests for template parsing

Closes #42
```

```
🐛 Fix build number duplication in CI

The Windows and Linux builds were not sharing the same build number
in GitHub Actions. Implemented CI marker detection to prevent
incrementation in CI environment.

Fixes #38
```

### XML Documentation

All public types and members should have XML documentation comments:

```csharp
/// <summary>
/// Gets or sets the server URL for ArcGIS Monitor.
/// </summary>
/// <remarks>
/// The URL should be absolute and include the protocol (e.g., https://monitor.example.com:30443/arcgis).
/// </remarks>
public string Url { get; set; }

/// <summary>
/// Validates the configuration.
/// </summary>
/// <returns>A list of validation errors, or an empty list if valid.</returns>
public List<string> Validate()
{
	// Implementation
}
```

## Testing

### Unit Tests

- Write tests for all new features
- Tests should be in a project named `*.Tests` (e.g., `ArcGISMonitorExcelReporter.Tests`)
- Use descriptive test names following the pattern: `MethodName_Condition_ExpectedResult`
- Aim for high coverage but prioritize meaningful tests over coverage percentage

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity detailed

# Run tests with coverage report
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Test Example

```csharp
[TestClass]
public class ServerConfigurationTests
{
	[TestMethod]
	public void Validate_WithValidUrl_ReturnsNoErrors()
	{
		// Arrange
		var config = new ServerConfiguration
		{
			Url = "https://monitor.example.com:30443/arcgis",
			Username = "admin",
			Password = "password"
		};

		// Act
		var errors = config.Validate();

		// Assert
		Assert.AreEqual(0, errors.Count);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void Validate_WithInvalidUrl_ThrowsException()
	{
		// Arrange
		var config = new ServerConfiguration
		{
			Url = "not-a-valid-url",
			Username = "admin",
			Password = "password"
		};

		// Act & Assert
		config.Validate();
	}
}
```

## Documentation

When adding new features or changing existing behavior:

1. **Update README.md** with usage examples
2. **Update CHANGELOG.md** with your changes
3. **Add XML documentation comments** to your code
4. **Update configuration examples** if applicable
5. **Add code samples** to the Samples directory if appropriate

## Build and Release

### Local Build

```bash
# Debug build
dotnet build

# Release build
dotnet build --configuration Release
```

### Publishing

The project uses GitHub Actions for automated publishing. When you create a release:

1. The workflow automatically builds both `win-x64` and `linux-x64` versions
2. Binaries are packaged and uploaded to the GitHub release
3. Version numbers are automatically managed based on the build date

**Note:** The version number is automatically generated in the format `yyyy.MM.dd.BuildNumber`. Do not manually edit version files in pull requests; they will be generated automatically during the build process.

### Build Number Management

The build number system uses:
- `BuildNumber.txt` - Stores the current build number
- `LastDatePrefix.txt` - Stores the last build date
- `BuildNumberFromCI.txt` - Marker file for CI environment detection

These files are **git-ignored** and automatically managed. See [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) for technical details.

## Additional Notes

### Issue and Pull Request Labels

- `bug` - Something isn't working
- `enhancement` - New feature or request
- `documentation` - Improvements or additions to documentation
- `good first issue` - Good for newcomers
- `help wanted` - Extra attention is needed
- `question` - Further information is requested
- `wontfix` - This will not be worked on

### Recognition

Contributors will be recognized in:
- The [CHANGELOG.md](CHANGELOG.md)
- The GitHub repository contributors list
- Project documentation

## Questions?

Don't hesitate to reach out:
- Open an issue with the `question` label
- Start a [GitHub Discussion](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)
- Check our existing [documentation](README.md)

---

**Thank you for contributing to ArcGIS Monitor Excel Reporter!** 🎉
