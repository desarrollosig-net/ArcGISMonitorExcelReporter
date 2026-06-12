# Error Handling Improvements - MonitorExcelReportWriter

## Overview

Added comprehensive error handling throughout the Excel generation process to diagnose and prevent issues like duplicate column names.

---

## Key Improvements

### 1. **Main Save Method**
**Location**: `Save(MonitorExcelReport report, string outputPath)`

**Features**:
- ✅ Try-catch wrapper around entire workbook creation
- ✅ Specific handling for duplicate column name errors
- ✅ Automatic detection and logging of duplicate columns in all sheets
- ✅ Detailed error messages with context

**Error Detection**:
```csharp
catch (ArgumentException ex) when (ex.Message.Contains("more than one field name"))
{
    Log.Error(ex, "Duplicate column name detected in Excel table...");
    
    // Scan all worksheets for duplicate columns
    foreach (var ws in workbook.Worksheets)
    {
        var tables = ws.Tables.ToList();
        foreach (var table in tables)
        {
            var duplicates = table.Fields
                .Select(f => f.Name)
                .GroupBy(n => n)
                .Where(g => g.Count() > 1);
                
            if (duplicates.Any())
            {
                Log.Error("Sheet '{SheetName}' has duplicate column names: {Duplicates}");
            }
        }
    }
}
```

---

### 2. **Component Summary Table**
**Location**: `WriteComponentsSummaryTable()`

**Features**:
- ✅ Duplicate column name detection and auto-renaming
- ✅ Case-insensitive duplicate checking
- ✅ Detailed logging of property names and filtering
- ✅ Validation of final column list before table creation
- ✅ Error recovery with informative messages

**Deduplication Logic**:
```csharp
var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

foreach (var prop in nonEmptyProperties)
{
    if (columnNames.ContainsKey(prop.Name))
    {
        columnNames[prop.Name]++;
        var uniqueName = $"{prop.Name}_{columnNames[prop.Name]}";
        Log.Warning("Duplicate column '{Name}', renamed to '{UniqueName}'");
    }
    else
    {
        columnNames[prop.Name] = 0;
    }
}
```

**Logging**:
- Property discovery: `"Found {Count} properties: {Names}"`
- Non-empty filtering: `"Filtered to {Count} non-empty properties: {Names}"`
- Final columns: `"Final column names: {Columns}"`
- Warnings for duplicates with sheet name and column details

---

### 3. **Aggregated Metrics Table**
**Location**: `WriteAggregatedMetricsTable()`

**Features**:
- ✅ Try-catch around entire method
- ✅ Logging of metric aggregation process
- ✅ Error recovery with partial data display
- ✅ Table creation validation

**Error Recovery**:
```csharp
catch (Exception ex)
{
    Log.Error(ex, "Failed to write aggregated metrics table in sheet '{SheetName}'");
    ws.Cell(startRow, 1).Value = $"Error creating metrics table: {ex.Message}";
    return startRow + 2;
}
```

---

### 4. **Time Series with Chart**
**Location**: `WriteTimeSeriesWithChart()`

**Features**:
- ✅ Duplicate metric name detection and renaming
- ✅ Comprehensive logging at each stage
- ✅ Graceful degradation if no data available
- ✅ Error recovery with informative messages

**Metric Name Deduplication**:
```csharp
var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

foreach (var metric in top20Metrics)
{
    if (columnNames.ContainsKey(metric.MetricName))
    {
        var uniqueName = $"{metric.MetricName}_{++columnNames[metric.MetricName]}";
        Log.Warning("Duplicate metric name '{Name}' in time series, renamed to '{UniqueName}'");
    }
}
```

---

### 5. **Component Sheets Writer**
**Location**: `WriteComponentSheets()`

**Features**:
- ✅ Per-section try-catch blocks
- ✅ Continues processing even if one section fails
- ✅ Creates error sheets for completely failed component types
- ✅ Detailed logging of progress and errors

**Section-Level Recovery**:
```csharp
// SECTION 1: COMPONENT SUMMARY
try { /* ... */ }
catch (Exception ex)
{
    Log.Error(ex, "Failed to write component summary for '{ComponentType}'");
    ws.Cell(currentRow, 1).Value = $"Error in component summary: {ex.Message}";
    currentRow += 3; // Continue to next section
}

// SECTION 2: AGGREGATED METRICS
try { /* ... */ }
catch (Exception ex) { /* similar recovery */ }

// SECTION 3: TIME SERIES
try { /* ... */ }
catch (Exception ex) { /* similar recovery */ }
```

**Complete Failure Recovery**:
```csharp
catch (Exception ex)
{
    Log.Error(ex, "Failed to create sheet for '{ComponentType}'");
    
    // Create error sheet with details
    var errorSheetName = $"ERROR_{componentType}";
    var errorWs = workbook.Worksheets.Add(errorSheetName);
    errorWs.Cell(1, 1).Value = $"Error creating sheet for {componentType}";
    errorWs.Cell(2, 1).Value = ex.Message;
    errorWs.Cell(3, 1).Value = ex.StackTrace;
}
```

---

## Logging Enhancements

### Property Discovery
```
[DEBUG] Found 12 properties in ComponentReportRow: CollectionName, ComponentId, Name, Type, ...
[DEBUG] Filtered to 8 non-empty properties: ComponentId, Name, Type, AddressInternal, ...
[DEBUG] Final column names for sheet 'host': ComponentId, Name, Type, AddressInternal, ...
```

### Duplicate Detection
```
[WARNING] Duplicate column name 'Connectivity', renamed to 'Connectivity_1' in sheet 'host'
[WARNING] Duplicate metric name 'CPU Utilized' in time series, renamed to 'CPU Utilized_1' in sheet 'host'
```

### Progress Tracking
```
[INFO] Writing sheet for component type 'host' with 25 components
[DEBUG] Found 150 metrics for component type 'host'
[DEBUG] Aggregated 45 metrics in sheet 'host'
[DEBUG] Found 3600 time series data points for component type 'host'
[DEBUG] Selected Top 20 metrics for time series chart in sheet 'host'
[INFO] Successfully created sheet for component type 'host'
```

### Error Details
```
[ERROR] Failed to write component summary section for type 'host'
System.ArgumentException: Duplicate column name 'Connectivity'
   at WriteComponentsSummaryTable(...)
   
[ERROR] Sheet 'host' has duplicate column names: Connectivity
```

---

## Error Recovery Strategy

| Error Level | Strategy | Result |
|-------------|----------|--------|
| **Section fails** | Continue to next section, display error message | Partial sheet with other sections intact |
| **Sheet fails** | Create error sheet with details | Error sheet with stack trace |
| **Save fails** | Scan all sheets for duplicates, log details | Detailed error report in logs |
| **Duplicate columns** | Auto-rename with suffix (_1, _2, etc.) | Table created successfully with unique names |

---

## Benefits

1. **Diagnostic Power**
   - Identifies exact sheet and column causing issues
   - Logs all property names before and after filtering
   - Traces data flow through each section

2. **Fault Tolerance**
   - One section failure doesn't break entire sheet
   - One sheet failure doesn't break entire workbook
   - Partial data is better than no data

3. **Debugging Efficiency**
   - Detailed logs pinpoint exact issue
   - No need to re-run with debugger
   - Stack traces preserved in error sheets

4. **User Experience**
   - Clear error messages in Excel cells
   - Partial results still useful
   - Error sheets guide troubleshooting

---

## Testing Scenarios

### Scenario 1: Duplicate Property Names
**Before**: ArgumentException, no Excel file
**After**: Auto-renamed columns (Connectivity_1), warning in logs, Excel created

### Scenario 2: Missing Metric Data
**Before**: Crash during aggregation
**After**: Empty table with "No metrics found" message

### Scenario 3: Time Series Failure
**Before**: Entire sheet fails
**After**: Component summary and metrics sections intact, time series shows error

### Scenario 4: Complete Sheet Failure
**Before**: Report generation stops
**After**: Error sheet created, other component types processed successfully

---

## Monitoring in Production

Check logs for these patterns:

**Warning Signs**:
```
[WARNING] Duplicate column name detected
[WARNING] No displayable columns found
```

**Critical Issues**:
```
[ERROR] Failed to create table
[ERROR] Sheet has duplicate column names
[ERROR] Failed to create sheet for component type
```

**Success Indicators**:
```
[INFO] Successfully created sheet for component type
[INFO] Excel file saved successfully. Size: X bytes
```

---

## Future Enhancements

1. **Pre-validation**: Check for duplicates before creating workbook
2. **Auto-fix**: Automatically handle known data model issues
3. **Metrics**: Track error rates by component type
4. **Notifications**: Alert on critical errors via external systems

---

## Related Files

- `MonitorExcelReportWriter.cs` - Main implementation
- `MonitorExcelReportModels.cs` - Data models (check for duplicate properties)
- `MonitorReportMapper.cs` - Data mapping logic

---

## Summary

The error handling improvements make the Excel generation process:
- ✅ **Robust**: Continues despite errors
- ✅ **Diagnostic**: Detailed logging and error messages
- ✅ **User-friendly**: Partial results and clear error indicators
- ✅ **Maintainable**: Easy to debug and enhance

The original error "The header row contains more than one field name 'Connectivity'" will now be:
1. Detected and logged with sheet name
2. Auto-renamed to 'Connectivity_1'
3. Or, if critical, displayed with full context in logs
