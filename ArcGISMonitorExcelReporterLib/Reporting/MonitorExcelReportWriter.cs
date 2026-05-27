using ClosedXML.Excel;
using System.Reflection;
using Serilog;

namespace ArcGISMonitorExcelReporterLib.Reporting;

/// <summary>
/// Excel report generator for ArcGIS Monitor data.
/// Creates structured Excel files with multiple sheets including summary, components, alerts, and metric data.
/// </summary>
/// <remarks>
/// <para>
/// This class transforms <see cref="MonitorExcelReport"/> objects into well-formatted Excel (.xlsx) files using ClosedXML.
/// </para>
/// <para>
/// Generated Excel file structure:
/// <list type="bullet">
/// <item><description><b>Inputs:</b> Report parameters (dates, counts)</description></item>
/// <item><description><b>Summary:</b> Summary by component type with alerts and sheet index</description></item>
/// <item><description><b>Components:</b> Complete component inventory</description></item>
/// <item><description><b>Alerts:</b> All alerts for the period</description></item>
/// <item><description><b>Metric sheets:</b> One per component type + metric, with aggregated data from ArcGIS Monitor</description></item>
/// </list>
/// </para>
/// <para>
/// Special features:
/// <list type="bullet">
/// <item><description>Host process metrics (Process CPU*, Process Instances*, Process Memory*) are grouped with wildcards</description></item>
/// <item><description>Sheet names sanitized and truncated to 31 characters (Excel limit)</description></item>
/// <item><description>Hyperlinks between sheets for easy navigation</description></item>
/// <item><description>Excel tables with automatic formatting and frozen headers</description></item>
/// <item><description>Percentile 95 calculation using formula: avg + 1.645 * stddev (when count ≥ 30)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class MonitorExcelReportWriter
{
    /// <summary>
    /// Saves an ArcGIS Monitor report to an Excel (.xlsx) file.
    /// </summary>
    /// <param name="report">The report with all component, metric, and alert data.</param>
    /// <param name="outputPath">Full path to the Excel file to create. Directory will be created if it doesn't exist.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="report"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="outputPath"/> is empty or null.</exception>
    /// <remarks>
    /// <para>
    /// This method creates a complete Excel file with the following sheets:
    /// <list type="number">
    /// <item><description>Inputs - Report parameters</description></item>
    /// <item><description>Summary - Summary by component type with navigable index</description></item>
    /// <item><description>Components - Table of all components</description></item>
    /// <item><description>Alerts - Table of all alerts</description></item>
    /// <item><description>Metric sheets - One per component type + metric combination</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The file is saved using ClosedXML and all processing is logged with Serilog.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var report = new MonitorExcelReport 
    /// { 
    ///     Components = components, 
    ///     Metrics = metrics,
    ///     MetricData = metricData 
    /// };
    /// var writer = new MonitorExcelReportWriter();
    /// writer.Save(report, @"C:\Reports\monitor_report_20250127.xlsx");
    /// </code>
    /// </example>
    public static void Save(MonitorExcelReport report, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(report);
        if(string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Excel file path is required.", nameof(outputPath));
        }

        Log.Information("Creating Excel workbook...");

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            Log.Debug("Output directory: {Directory}", directory);
        }

        using var workbook = new XLWorkbook();
        var sheetRegistry = new SheetRegistry(workbook);

        Log.Debug("Writing Inputs sheet...");
        WriteInputs(workbook, sheetRegistry, report);

        Log.Debug("Writing Summary sheet...");
        WriteSummary(workbook, sheetRegistry, report);

        Log.Debug("Writing Components sheet ({Count} rows)...", report.Components.Count);
        WriteTableSheet(workbook, sheetRegistry, "Components", report.Components);

        Log.Debug("Writing Alerts sheet ({Count} rows)...", report.Alerts.Count);
        WriteTableSheet(workbook, sheetRegistry, "Alerts", report.Alerts);

        Log.Debug("Writing component-metric sheets...");
        WriteComponentMetricSheets(workbook, sheetRegistry, report);

        Log.Information("Saving Excel file to: {OutputPath}", outputPath);
        workbook.SaveAs(outputPath);

        var fileInfo = new FileInfo(outputPath);
        Log.Information("Excel file saved successfully. Size: {Size:N0} bytes", fileInfo.Length);
    }

    /// <summary>
    /// Writes the "Inputs" sheet with report parameters and metadata.
    /// </summary>
    /// <param name="workbook">The Excel workbook where the sheet will be added.</param>
    /// <param name="sheetRegistry">Sheet name registry to avoid duplicates.</param>
    /// <param name="report">The report with data to display.</param>
    /// <remarks>
    /// Displays information such as generation dates, time range, and totals of components/metrics/alerts.
    /// </remarks>
    private static void WriteInputs(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
    {
        var ws = workbook.Worksheets.Add("Inputs");
        sheetRegistry.Register("Inputs", "Inputs");

        ws.Cell(1, 1).Value = "Report Parameters";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        var metadata = new (string Label, object? Value)[]
        {
            ("Generated UTC", report.GeneratedAtUtc),
            ("From UTC", report.FromUtc),
            ("To UTC", report.ToUtc),
            ("Total Collections", report.Collections.Count),
            ("Total Components", report.Components.Count),
            ("Total Metrics", report.Metrics.Count),
            ("Total Metric Data Points", report.MetricData.Count),
            ("Total Alerts", report.Alerts.Count)
        };

        var row = 3;
        foreach (var item in metadata)
        {
            ws.Cell(row, 1).Value = item.Label;
            ws.Cell(row, 1).Style.Font.Bold = true;
            SetCellValue(ws.Cell(row, 2), item.Value);
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    /// <summary>
    /// Writes the "Summary" sheet with executive summary and navigable index of all sheets.
    /// </summary>
    /// <param name="workbook">The Excel workbook where the sheet will be added.</param>
    /// <param name="sheetRegistry">Sheet name registry to create correct hyperlinks.</param>
    /// <param name="report">The report with data to summarize.</param>
    /// <remarks>
    /// <para>
    /// Contains two main sections:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <b>Summary table:</b> Groups components by Type and Subtype, showing:
    /// - Number of components
    /// - Critical, warning, and info alerts
    /// </description></item>
    /// <item><description>
    /// <b>Sheet index:</b> Clickable links to all report sheets, including
    /// grouped metric sheets (with wildcards for Process CPU*, Process Instances*, Process Memory*)
    /// </description></item>
    /// </list>
    /// </remarks>
    private static void WriteSummary(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
    {
        var ws = workbook.Worksheets.Add("Summary");
        sheetRegistry.Register("Summary", "Summary");

        ws.Cell(1, 1).Value = "ArcGIS Monitor - Summary Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        // Group components by type and category
        var summaryData = report.Components
            .GroupBy(c => new { c.Type, c.Subtype })
            .Select(g =>
            {
                var componentIds = g.Select(c => c.ComponentId).ToList();
                var componentMetrics = report.Metrics.Where(m => componentIds.Contains(m.ComponentId)).ToList();
                var metricIds = componentMetrics.Select(m => m.MetricId).ToList();
                var componentAlerts = report.Alerts.Where(a => componentIds.Contains(a.ComponentId ?? 0)).ToList();

                return new
                {
                    ComponentType = g.Key.Type ?? "Unknown",
                    Category = g.Key.Subtype ?? "General",
                    ComponentCount = g.Count(),
                    CriticalAlerts = componentAlerts.Count(a => a.CriticalThreshold.HasValue && a.Status == 2),
                    WarningAlerts = componentAlerts.Count(a => a.WarningThreshold.HasValue && a.Status == 1),
                    InfoAlerts = componentAlerts.Count(a => a.InfoThreshold.HasValue && a.Status == 0)
                };
            })
            .OrderBy(x => x.ComponentType)
            .ThenBy(x => x.Category)
            .ToList();

        // Write summary table
        var row = 3;
        ws.Cell(row, 1).Value = "Component Type";
        ws.Cell(row, 2).Value = "Category";
        ws.Cell(row, 3).Value = "Number of Components";
        ws.Cell(row, 4).Value = "Critical Alerts";
        ws.Cell(row, 5).Value = "Warning Alerts";
        ws.Cell(row, 6).Value = "Info Alerts";
        ws.Range(row, 1, row, 6).Style.Font.Bold = true;
        ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
        row++;

        foreach (var item in summaryData)
        {
            ws.Cell(row, 1).Value = item.ComponentType;
            ws.Cell(row, 2).Value = item.Category;
            ws.Cell(row, 3).Value = item.ComponentCount;
            ws.Cell(row, 4).Value = item.CriticalAlerts;
            ws.Cell(row, 5).Value = item.WarningAlerts;
            ws.Cell(row, 6).Value = item.InfoAlerts;
            row++;
        }

        // Add index section
        row += 2;
        ws.Cell(row, 1).Value = "Sheet Index";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Inputs"), "Inputs", "Report parameters and metadata");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Components"), "Components", "Complete component inventory");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Alerts"), "Alerts", "All alerts across components");

        // Add links to component-metric sheets
        row += 1;
        ws.Cell(row, 1).Value = "Component Type Metrics (grouped by type and metric)";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        // Group by ComponentType + MetricName (with special grouping for host process metrics)
        var metricsByTypeGroups = report.Metrics
            .GroupBy(m =>
            {
                var componentType = report.Components.FirstOrDefault(c => c.ComponentId == m.ComponentId)?.Type ?? "Unknown";
                var metricName = m.MetricName ?? $"Metric_{m.MetricId}";

                // For host components, group process metrics by wildcard
                if (componentType.Equals("host", StringComparison.OrdinalIgnoreCase))
                {
                    if (metricName.StartsWith("Process CPU", StringComparison.OrdinalIgnoreCase))
                        return new { ComponentType = componentType, MetricName = "Process CPU*" };
                    if (metricName.StartsWith("Process Instances", StringComparison.OrdinalIgnoreCase))
                        return new { ComponentType = componentType, MetricName = "Process Instances*" };
                    if (metricName.StartsWith("Process Memory", StringComparison.OrdinalIgnoreCase))
                        return new { ComponentType = componentType, MetricName = "Process Memory*" };
                }

                return new { ComponentType = componentType, MetricName = metricName };
            })
            .OrderBy(g => g.Key.ComponentType)
            .ThenBy(g => g.Key.MetricName);

        foreach (var group in metricsByTypeGroups)
        {
            var logicalName = SheetRegistry.BuildMetricByTypeSheetName(group.Key.ComponentType, group.Key.MetricName);
            var label = $"{group.Key.ComponentType} - {group.Key.MetricName}";
            var description = group.Key.MetricName.EndsWith("*") 
                ? $"All metrics starting with '{group.Key.MetricName.TrimEnd('*')}' for {group.Key.ComponentType} components"
                : $"All {group.Key.MetricName} data for {group.Key.ComponentType} components";
            WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName(logicalName), label, description);
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(3);
    }

    /// <summary>
    /// Writes metric sheets grouped by component type and metric name.
    /// </summary>
    /// <param name="workbook">The Excel workbook where sheets will be added.</param>
    /// <param name="sheetRegistry">Sheet name registry to manage duplicates and truncation.</param>
    /// <param name="report">The report with metrics and data to write.</param>
    /// <remarks>
    /// <para>
    /// Creates one sheet per unique ComponentType + MetricName combination.
    /// </para>
    /// <para>
    /// <b>Special grouping for host:</b> Process metrics are grouped with wildcards:
    /// <list type="bullet">
    /// <item><description>Process CPU* - All "Process CPU - [process]" metrics</description></item>
    /// <item><description>Process Instances* - All "Process Instances - [process]" metrics</description></item>
    /// <item><description>Process Memory* - All "Process Memory - [process]" metrics</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Each sheet contains:
    /// <list type="number">
    /// <item><description>Header: Component Type, Metric Name, Unit</description></item>
    /// <item><description>Data table: All metric_data rows from ArcGIS Monitor ordered by ComponentName → MetricName → ObservedAt</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Sheet name: {ComponentType}{MetricName} without spaces (e.g., "hostCPUUtilized", "hostProcessCPU_")
    /// </para>
    /// </remarks>
    private static void WriteComponentMetricSheets(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
    {
        // Group by ComponentType + MetricName (with special grouping for host process metrics)
        var metricsByTypeGroups = report.Metrics
            .GroupBy(m =>
            {
                var componentType = report.Components.FirstOrDefault(c => c.ComponentId == m.ComponentId)?.Type ?? "Unknown";
                var metricName = m.MetricName ?? $"Metric_{m.MetricId}";
                var unit = m.Unit;

                // For host components, group process metrics by wildcard
                if (componentType.Equals("host", StringComparison.OrdinalIgnoreCase))
                {
                    if(metricName.StartsWith("Process CPU", StringComparison.OrdinalIgnoreCase))
                        return new { ComponentType = componentType, MetricName = "Process CPU*", Unit = "varies" };
                    if (metricName.StartsWith("Process Instances", StringComparison.OrdinalIgnoreCase))
                        return new { ComponentType = componentType, MetricName = "Process Instances*", Unit = "varies" };
                    if (metricName.StartsWith("Process Memory", StringComparison.OrdinalIgnoreCase))
                        return new { ComponentType = componentType, MetricName = "Process Memory*", Unit = "varies" };
                }

                return new { ComponentType = componentType, MetricName = metricName, Unit = unit ?? string.Empty };
            })
            .OrderBy(g => g.Key.ComponentType)
            .ThenBy(g => g.Key.MetricName);

        foreach (var group in metricsByTypeGroups)
        {
            var logicalName = SheetRegistry.BuildMetricByTypeSheetName(group.Key.ComponentType, group.Key.MetricName);
            var physicalName = sheetRegistry.GetOrCreatePhysicalName(logicalName);
            var ws = workbook.Worksheets.Add(physicalName);
            WriteBackToIndex(ws);

            // Header with component type and metric information
            ws.Cell(2, 1).Value = "Component Type";
            ws.Cell(2, 2).Value = group.Key.ComponentType;
            ws.Cell(2, 1).Style.Font.Bold = true;

            ws.Cell(3, 1).Value = "Metric";
            ws.Cell(3, 2).Value = group.Key.MetricName;
            ws.Cell(3, 1).Style.Font.Bold = true;

            ws.Cell(4, 1).Value = "Unit";
            ws.Cell(4, 2).Value = group.Key.Unit;
            ws.Cell(4, 1).Style.Font.Bold = true;

            // Collect all metric data for all metrics in this group
            var allMetricData = new List<MetricDataReportRow>();
            foreach (var metric in group)
            {
                var metricData = report.MetricData
                    .Where(md => md.MetricId == metric.MetricId)
                    .ToList();
                allMetricData.AddRange(metricData);
            }

            // Sort by component name, metric name (important for wildcard groups), and then by observed time
            allMetricData = [.. allMetricData
                .OrderBy(md => md.ComponentName)
                .ThenBy(md => md.MetricName)
                .ThenBy(md => md.ObservedAt)];

            // Write metric data table
            if (allMetricData.Any())
            {
                WriteRows(ws, 6, allMetricData);
            }
            else
            {
                ws.Cell(6, 1).Value = "No metric data available";
            }
        }
    }

    /// <summary>
    /// Writes a generic sheet with a typed data table.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection (must have public properties).</typeparam>
    /// <param name="workbook">The Excel workbook where the sheet will be added.</param>
    /// <param name="sheetRegistry">Sheet name registry to avoid duplicates.</param>
    /// <param name="logicalName">Logical sheet name (will be sanitized and truncated as needed).</param>
    /// <param name="rows">Collection of objects to write as table rows.</param>
    /// <remarks>
    /// Uses reflection to get public properties of type T and create columns automatically.
    /// The table is created at row 3 with bold headers and Excel table formatting.
    /// </remarks>
    private static void WriteTableSheet<T>(XLWorkbook workbook, SheetRegistry sheetRegistry, string logicalName, IReadOnlyCollection<T> rows)
    {
        var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName(logicalName));
        WriteBackToIndex(ws);
        WriteRows(ws, 3, rows);
    }

    /// <summary>
    /// Writes generic data rows to a sheet, using reflection to get columns.
    /// </summary>
    /// <typeparam name="T">The type of objects (must have public instance properties).</typeparam>
    /// <param name="ws">The sheet where data will be written.</param>
    /// <param name="startRow">The row where to start writing (1-based).</param>
    /// <param name="rows">Collection of objects to write.</param>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="number">
    /// <item><description>Gets public properties of type T using reflection</description></item>
    /// <item><description>Writes headers at <paramref name="startRow"/> with bold format</description></item>
    /// <item><description>Writes each object as a row, using <see cref="SetCellValue"/> for appropriate formatting</description></item>
    /// <item><description>Creates an Excel table over the used range</description></item>
    /// <item><description>Adjusts column widths to content</description></item>
    /// <item><description>Freezes the header row</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static void WriteRows<T>(IXLWorksheet ws, int startRow, IReadOnlyCollection<T> rows)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (properties.Length == 0)
        {
            ws.Cell(startRow, 1).Value = "No columns.";
            return;
        }

        var headerRow = startRow;
        for (var col = 0; col < properties.Length; col++)
        {
            ws.Cell(headerRow, col + 1).Value = properties[col].Name;
            ws.Cell(headerRow, col + 1).Style.Font.Bold = true;
        }

        var row = headerRow + 1;
        foreach (var item in rows)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                SetCellValue(ws.Cell(row, col + 1), value);
            }
            row++;
        }

        var usedRange = ws.Range(headerRow, 1, Math.Max(row - 1, headerRow), properties.Length);
        usedRange.CreateTable();
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(headerRow);
    }

    /// <summary>
    /// Writes a "Back to index" hyperlink in cell A1 of a sheet.
    /// </summary>
    /// <param name="ws">The sheet where the link will be written.</param>
    /// <remarks>
    /// Creates a clickable link to cell A1 of the "Summary" sheet for easy navigation.
    /// </remarks>
    private static void WriteBackToIndex(IXLWorksheet ws)
    {
        ws.Cell(1, 1).FormulaA1 = HyperlinkFormula("Summary", "Back to index");
        ws.Cell(1, 1).Style.Font.Bold = true;
    }

    /// <summary>
    /// Writes a hyperlink to another sheet with a label and description.
    /// </summary>
    /// <param name="ws">The sheet where the link will be written.</param>
    /// <param name="row">The row where to write (1-based).</param>
    /// <param name="targetSheet">Physical name of the target sheet.</param>
    /// <param name="label">Text to display in the hyperlink.</param>
    /// <param name="description">Description to display in column B.</param>
    /// <remarks>
    /// Creates a hyperlink in column A and puts the description in column B of the same row.
    /// </remarks>
    private static void WriteIndexLink(IXLWorksheet ws, int row, string targetSheet, string label, string description)
    {
        ws.Cell(row, 1).FormulaA1 = HyperlinkFormula(targetSheet, label);
        ws.Cell(row, 2).Value = description;
    }

    /// <summary>
    /// Creates an Excel HYPERLINK formula to navigate to another sheet.
    /// </summary>
    /// <param name="sheetName">Target sheet name (will be escaped appropriately).</param>
    /// <param name="text">Text to display in the link (will be escaped appropriately).</param>
    /// <returns>A string with the HYPERLINK formula ready to use in Excel.</returns>
    /// <remarks>
    /// Escapes single quotes in sheet name and double quotes in text according to Excel rules.
    /// The link always points to cell A1 of the target sheet.
    /// </remarks>
    /// <example>
    /// <code>
    /// var formula = HyperlinkFormula("Summary", "Go to summary");
    /// // Returns: HYPERLINK("#'Summary'!A1","Go to summary")
    /// </code>
    /// </example>
    private static string HyperlinkFormula(string sheetName, string text)
    {
        var escapedSheet = sheetName.Replace("'", "''");
        var escapedText = text.Replace("\"", "\"\"");
        return $"HYPERLINK(\"#'{escapedSheet}'!A1\",\"{escapedText}\")";
    }

    /// <summary>
    /// Sets the value of an Excel cell with appropriate formatting based on data type.
    /// </summary>
    /// <param name="cell">The cell where the value will be set.</param>
    /// <param name="value">The value to write (can be null).</param>
    /// <remarks>
    /// <para>
    /// Handles the following types with special formatting:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>null</c> → Empty string</description></item>
    /// <item><description><see cref="DateTimeOffset"/> and <see cref="DateTime"/> → Format "yyyy-mm-dd hh:mm:ss"</description></item>
    /// <item><description>Numeric types (<c>int</c>, <c>long</c>, <c>double</c>, <c>decimal</c>, <c>float</c>) → Direct numeric value</description></item>
    /// <item><description><c>bool</c> → Boolean value</description></item>
    /// <item><description>Others → String conversion with <c>ToString()</c></description></item>
    /// </list>
    /// </remarks>
    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.UtcDateTime;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case bool boolean:
                cell.Value = boolean;
                break;
            case int integer:
                cell.Value = integer;
                break;
            case long longValue:
                cell.Value = longValue;
                break;
            case double doubleValue:
                cell.Value = doubleValue;
                break;
            case decimal decimalValue:
                cell.Value = decimalValue;
                break;
            case float floatValue:
                cell.Value = floatValue;
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }

    /// <summary>
    /// Sheet name registry to manage duplicates and name truncation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Excel has a 31-character limit for sheet names and doesn't allow duplicates.
    /// This class manages the conversion of logical names (can be long and descriptive)
    /// to physical names (sanitized, truncated, and unique).
    /// </para>
    /// <para>
    /// Features:
    /// <list type="bullet">
    /// <item><description>Sanitizes invalid characters (/, \, ?, *, [, ], :)</description></item>
    /// <item><description>Truncates names to 31 characters</description></item>
    /// <item><description>Adds suffixes _1, _2, etc. to avoid duplicates</description></item>
    /// <item><description>Maintains mapping between logical and physical names</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private sealed class SheetRegistry
    {
        private readonly XLWorkbook _workbook;
        private readonly Dictionary<string, string> _logicalToPhysical = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _usedPhysicalNames = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the sheet registry.
        /// </summary>
        /// <param name="workbook">The Excel workbook to verify existing names.</param>
        public SheetRegistry(XLWorkbook workbook)
        {
            _workbook = workbook;
        }

        /// <summary>
        /// Registers an explicit mapping between logical and physical name.
        /// </summary>
        /// <param name="logicalName">Logical/descriptive sheet name.</param>
        /// <param name="physicalName">Physical name that will appear in Excel.</param>
        /// <remarks>
        /// Useful for standard sheets like "Summary", "Inputs", etc. that always have the same name.
        /// </remarks>
        public void Register(string logicalName, string physicalName)
        {
            _logicalToPhysical[logicalName] = physicalName;
            _usedPhysicalNames.Add(physicalName);
        }

        /// <summary>
        /// Gets or creates a unique physical name for a given logical name.
        /// </summary>
        /// <param name="logicalName">Logical/descriptive sheet name.</param>
        /// <returns>Sanitized, truncated, and guaranteed unique physical name.</returns>
        /// <remarks>
        /// <para>
        /// If the logical name already has a mapping, returns it.
        /// If not, generates a new physical name:
        /// <list type="number">
        /// <item><description>Sanitizes invalid characters</description></item>
        /// <item><description>Truncates to 31 characters</description></item>
        /// <item><description>If conflict exists, adds _1, _2, etc.</description></item>
        /// <item><description>Saves the mapping for future queries</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string GetOrCreatePhysicalName(string logicalName)
        {
            if (_logicalToPhysical.TryGetValue(logicalName, out var existing))
                return existing;

            var baseName = SanitizeSheetName(logicalName);
            var candidate = baseName;
            var index = 1;
            while (_usedPhysicalNames.Contains(candidate) || _workbook.Worksheets.Any(w => w.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
            {
                var suffix = $"_{index++}";
                var prefixLength = Math.Min(baseName.Length, 31 - suffix.Length);
                candidate = baseName[..prefixLength] + suffix;
            }

            _logicalToPhysical[logicalName] = candidate;
            _usedPhysicalNames.Add(candidate);
            return candidate;
        }

        /// <summary>
        /// Builds a sheet name for collections (format: COL_{collection}_{type}).
        /// </summary>
        /// <param name="collectionName">Collection name.</param>
        /// <param name="componentType">Component type.</param>
        /// <returns>Logical name for the collection sheet.</returns>
        /// <remarks>
        /// Legacy method kept for compatibility. No longer used in current structure.
        /// </remarks>
        public static string BuildCollectionSheetName(string collectionName, string componentType)
            => $"COL_{collectionName}_{componentType}";

        /// <summary>
        /// Builds a sheet name for individual metrics (format: MET_{metric}).
        /// </summary>
        /// <param name="metricName">Metric name.</param>
        /// <returns>Logical name for the metric sheet.</returns>
        /// <remarks>
        /// Legacy method kept for compatibility. No longer used in current structure.
        /// </remarks>
        public static string BuildMetricSheetName(string metricName)
            => $"MET_{metricName}";

        /// <summary>
        /// Builds a sheet name for component-metric (format: {component}_{metric}).
        /// </summary>
        /// <param name="componentName">Component name.</param>
        /// <param name="metricName">Metric name.</param>
        /// <returns>Sanitized logical name for the sheet (not truncated).</returns>
        /// <remarks>
        /// Legacy method kept for compatibility. No longer used in current structure.
        /// </remarks>
        public static string BuildComponentMetricSheetName(string componentName, string metricName)
        {
            // Don't truncate here - let GetOrCreatePhysicalName handle truncation AND deduplication
            var sanitizedComponent = SanitizeSheetNameStatic(componentName);
            var sanitizedMetric = SanitizeSheetNameStatic(metricName);
            return $"{sanitizedComponent}_{sanitizedMetric}";
        }

        /// <summary>
        /// Builds a sheet name for metrics grouped by component type.
        /// Format: {ComponentType}{MetricName} without spaces.
        /// </summary>
        /// <param name="componentType">Component type (host, service, database, etc.).</param>
        /// <param name="metricName">Metric name (can include * for wildcards).</param>
        /// <returns>Sanitized logical name without spaces (not truncated).</returns>
        /// <remarks>
        /// <para>
        /// This is the method currently used to generate metric sheet names.
        /// </para>
        /// <para>
        /// Examples:
        /// <list type="bullet">
        /// <item><description>host + "CPU Utilized" → "hostCPUUtilized"</description></item>
        /// <item><description>host + "Process CPU*" → "hostProcessCPU_"</description></item>
        /// <item><description>service + "Request Rate" → "serviceRequestRate"</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Spaces are completely removed. Invalid characters are replaced with '_'.
        /// Truncation to 31 characters is done later in <see cref="GetOrCreatePhysicalName"/>.
        /// </para>
        /// </remarks>
        public static string BuildMetricByTypeSheetName(string componentType, string metricName)
        {
            // Remove spaces and sanitize - ComponentType + MetricName without spaces
            var sanitizedType = SanitizeSheetNameStatic(componentType).Replace(" ", "");
            var sanitizedMetric = SanitizeSheetNameStatic(metricName).Replace(" ", "");
            return $"{sanitizedType}{sanitizedMetric}";
        }

        /// <summary>
        /// Wrapper for <see cref="SanitizeSheetNameStatic"/> for instance use.
        /// </summary>
        /// <param name="value">String to sanitize.</param>
        /// <returns>Sanitized and truncated string to 31 characters.</returns>
        private static string SanitizeSheetName(string value)
        {
            return SanitizeSheetNameStatic(value);
        }

        /// <summary>
        /// Sanitizes and truncates a sheet name according to Excel rules.
        /// </summary>
        /// <param name="value">String to sanitize.</param>
        /// <returns>Valid string for Excel sheet name (maximum 31 characters).</returns>
        /// <remarks>
        /// <para>
        /// Rules applied:
        /// <list type="number">
        /// <item><description>Replaces invalid file and Excel characters (/, \, ?, *, [, ], :) with '_'</description></item>
        /// <item><description>Removes leading and trailing whitespace</description></item>
        /// <item><description>If empty, uses "Sheet" as default name</description></item>
        /// <item><description>Truncates to 31 characters (Excel limit)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Invalid characters in Excel:
        /// <c>[ ] * ? / \ :</c> and all invalid characters in operating system file names.
        /// </para>
        /// </remarks>
        private static string SanitizeSheetNameStatic(string value)
        {
            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars().Concat(['[', ']', '*', '?', '/', '\\', ':']));
            var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
            var sanitized = new string(chars).Trim();

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "Sheet";

            return sanitized.Length <= 31 ? sanitized : sanitized[..31];
        }
    }
}
