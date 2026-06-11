# Automatic Version System

This project uses an automatic versioning system that generates version numbers in the format: **yyyy.MM.dd.BuildNumber**

## Version Format

```
2025.01.27.5
│    │  │  │
│    │  │  └─── Build number (auto-incremented per day)
│    │  └────── Day
│    └───────── Month
└────────────── Year
```

## How It Works

### 1. Build-Time Generation

- Every time you build the project, the version is automatically generated
- The version format is based on the current date plus an incremental build number
- Build number resets to 1 each day

### 2. Files Generated

The following files are automatically generated and should NOT be committed to git:

- **`BuildNumber.txt`**: Contains the current build number for the day
- **`LastDatePrefix.txt`**: Stores the last build date to detect date changes
- **`obj/Debug|Release/VersionInfo.g.cs`**: Auto-generated C# class with version constants

These files are already included in `.gitignore`.

### 3. Version Display

The version is displayed in:

1. **Application startup logs**:
   ```
   =============================================================
   === ArcGIS Monitor Excel Reporter v2025.01.27.5 ===
   === Build: 2025-01-27 14:30:45 ===
   =============================================================
   ```

2. **Help output** (`-h` or `--help`):
   ```
   ArcGIS Monitor Excel Reporter v2025.01.27.5
   Build: 2025-01-27 14:30:45
   ========================================================
   ```

3. **Debug mode output**:
   ```
   [DEBUG] ArcGIS Monitor Excel Reporter v2025.01.27.5
   [DEBUG] Build: 2025-01-27 14:30:45
   ```

4. **Final completion message**:
   ```
   =============================================================
   === Report generated successfully ===
   === Output: C:\Reports\report.xlsx ===
   === Version: 2025.01.27.5 ===
   =============================================================
   ```

### 4. Accessing Version Information in Code

The auto-generated `VersionInfo` class provides the following constants:

```csharp
// Full version string
VersionInfo.Version         // "2025.01.27.5"

// Date portion only
VersionInfo.DateVersion     // "2025.01.27"

// Build number only
VersionInfo.BuildNumber     // "5"

// Build timestamp
VersionInfo.BuildTimestamp  // "2025-01-27 14:30:45"
```

Example usage:
```csharp
Log.Information("Starting application v{Version}", VersionInfo.Version);
Console.WriteLine($"Build: {VersionInfo.BuildTimestamp}");
```

## Build Number Behavior

### Daily Reset
- Build number resets to **1** at the start of each new day
- Example:
  - 2025.01.27.1 (first build on Jan 27)
  - 2025.01.27.2 (second build on Jan 27)
  - 2025.01.27.3 (third build on Jan 27)
  - 2025.01.28.1 (first build on Jan 28 - reset!)

### Incrementation
- Each successful build increments the build number by 1
- Build number is stored in `BuildNumber.txt`
- Date prefix is stored in `LastDatePrefix.txt`

## Clean Build

If you want to reset the build number manually:

```powershell
# Windows PowerShell
Remove-Item BuildNumber.txt, LastDatePrefix.txt -ErrorAction SilentlyContinue
```

```bash
# Linux/Mac
rm -f BuildNumber.txt LastDatePrefix.txt
```

The next build will start at build number 1.

## Assembly Version Properties

The version is also set in the assembly metadata:

- **AssemblyVersion**: 2025.01.27.5
- **FileVersion**: 2025.01.27.5
- **InformationalVersion**: 2025.01.27.5

You can view these in the compiled executable properties.

## CI/CD Integration

For continuous integration pipelines:

1. **Clean builds**: Remove `BuildNumber.txt` and `LastDatePrefix.txt` before building to start fresh
2. **Preserve between builds**: Keep these files to maintain incrementing build numbers
3. **Artifacts**: The generated `VersionInfo.g.cs` is in the `obj/` directory and doesn't need to be archived

Example GitHub Actions:
```yaml
- name: Build
  run: dotnet build --configuration Release
  
- name: Display version
  run: |
    $version = (Get-Content ArcGISMonitorExcelReporter/obj/Release/net8.0/VersionInfo.g.cs | Select-String 'Version = ').ToString().Split('"')[1]
    echo "Built version: $version"
```

## Files Modified

The automatic versioning system consists of:

1. **`VersionInfo.targets`**: MSBuild targets that increment version and generate code
2. **`ArcGISMonitorExcelReporter.csproj`**: Imports the targets file
3. **`Program.cs`**: Uses `VersionInfo` class to display version
4. **`.gitignore`**: Excludes auto-generated files

## Troubleshooting

### Version not updating
- Ensure you're doing a **full build** (not just run)
- Check that `VersionInfo.targets` is in the project directory
- Verify the `.csproj` file imports the targets file

### Build errors
- Clean the solution: `dotnet clean`
- Delete `obj/` and `bin/` folders
- Rebuild: `dotnet build`

### Version shows as old date
- The version is generated at **build time**, not runtime
- If you don't rebuild, the version will show the last build date
- Always rebuild before distribution

## Benefits

✅ **Automatic**: No manual version updates needed  
✅ **Traceable**: Easy to identify when a build was created  
✅ **Simple**: Date-based versioning is intuitive  
✅ **Incremental**: Build number tracks iterations per day  
✅ **Visible**: Version shown in logs and help output  
✅ **Embedded**: Version is part of the compiled assembly
