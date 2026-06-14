using ArcGISMonitorExcelReporterLib.Configuration;

using ClosedXML.Excel;

using System.Reflection;

using Serilog;

namespace ArcGISMonitorExcelReporterLib.Reporting
{
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
    /// <item><description><b>Components sheets:</b> One per component type (host, service, database, etc.)</description></item>
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
    /// <item><description>Percentile 95 calculation using exact z-score (1.6448536269514722) constrained by max observed value</description></item>
    /// <item><description>Metric sheets show consolidated statistics for the entire query period, not time series data</description></item>
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
        /// This method creates a complete Excel file with the following structure:
        /// <list type="number">
        /// <item><description><b>Inputs</b> - Report parameters and metadata</description></item>
        /// <item><description><b>Summary</b> - Executive summary with navigable index</description></item>
        /// <item><description><b>For each component type (host, service, database, etc.):</b>
        ///   <list type="bullet">
        ///   <item><description>Component summary sheet: All components of that type with non-null attributes</description></item>
        ///   <item><description>For each metric of that component type:
        ///     <list type="bullet">
        ///     <item><description>Metric summary sheet: Characteristics + aggregated data by component (Count, Min, Max, Avg, StdDev, P95) + alert counts</description></item>
        ///     <item><description>Time series sheet: Max values every 15 minutes by component with chart placeholder and instructions</description></item>
        ///     </list>
        ///   </description></item>
        ///   </list>
        /// </description></item>
        /// <item><description><b>Alerts</b> - Table of all alerts</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Sheet ordering: Inputs → Summary → (ComponentType1 → Metric1 Summary → Metric1 TimeSeries → Metric2 Summary → Metric2 TimeSeries → ...) → ComponentType2 → ... → Alerts
        /// </para>
        /// <para>
        /// Charts: ClosedXML does not support automated chart creation. Each time series sheet includes formatted instructions
        /// for users to manually create line charts from the data tables.
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
        /// MonitorExcelReportWriter.Save(report, @"C:\Reports\monitor_report_20250127.xlsx");
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
            if(!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                Log.Debug("Output directory: {Directory}", directory);
            }

            using var workbook = new XLWorkbook();
            var sheetRegistry = new SheetRegistry(workbook);

            try
            {
                Log.Debug("Writing Inputs sheet...");
                WriteInputs(workbook, sheetRegistry, report);

                Log.Debug("Writing Summary sheet...");
                WriteSummary(workbook, sheetRegistry, report);

                Log.Debug("Writing Components sheets by type...");
                WriteComponentsWithMetricsSheets(workbook, sheetRegistry, report);

                Log.Debug("Writing Alerts sheet ({Count} rows)...", report.Alerts.Count);
                WriteAlertsSheet(workbook, sheetRegistry, report.Alerts);

                Log.Information("Saving Excel file to: {OutputPath}", outputPath);
                workbook.SaveAs(outputPath);

                var fileInfo = new FileInfo(outputPath);
                Log.Information("Excel file saved successfully. Size: {Size:N0} bytes", fileInfo.Length);
            }
            catch(ArgumentException ex) when(ex.Message.Contains("more than one field name"))
            {
                Log.Error(ex, "Duplicate column name detected in Excel table. This usually means there are duplicate property names in the data model.");
                Log.Error("Attempting to identify duplicate columns...");

                LogDuplicateColumns(workbook);

                throw new InvalidOperationException(
                    "Failed to create Excel file due to duplicate column names. " +
                    "This is usually caused by duplicate property names in the component data. " +
                    "Check the logs above for details about which columns are duplicated.", ex);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Unexpected error while creating Excel workbook");
                throw;
            }
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

            var tz = TimeZoneInfoResolver.Resolve(report.Timezone);

            var metadata = new (string Label, object? Value)[]
            {
                ("Server URL",              report.ServerUrl),
                ("Collection",              !string.IsNullOrWhiteSpace(report.CollectionName) && report.CollectionName.Trim() != "*"
                                                ? report.CollectionName
                                                : null),
                ("Generated",               FormatLocalDate(report.GeneratedAtUtc, tz)),
                ("From",                    FormatLocalDate(report.FromUtc, tz)),
                ("To",                      FormatLocalDate(report.ToUtc, tz)),
                ("Past Days",               (object)report.PastDays),
                ("Past Hours",              (object)report.PastHours),
                ("Timezone",                report.Timezone),
                ("Total Components Types",  report.Collections.Count),
                ("Total Components",        report.Components.Count),
                ("Total Metrics",           report.Metrics.Count),
                ("Total Metric Data",       report.MetricData.Count),
                ("Total Time Series Data",  report.TimeSeriesMetricData.Count),
                ("Total Alerts",            report.Alerts.Count)
            };

            var row = 3;
            foreach(var (Label, Value) in metadata)
            {
                if(Value is null)
                {
                    continue;
                }

                ws.Cell(row, 1).Value = Label;
                ws.Cell(row, 1).Style.Font.Bold = true;
                SetCellValue(ws.Cell(row, 2), Value);
                row++;
            }

            ws.Columns().AdjustToContents();
        }

        /// <summary>
        /// Converts a UTC <see cref="DateTimeOffset"/> to the given timezone and returns it
        /// as a formatted string (yyyy-MM-dd HH:mm zzz).
        /// </summary>
        private static string FormatLocalDate(DateTimeOffset utc, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTime(utc, tz);
            return local.ToString("yyyy-MM-dd HH:mm zzz");
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

                        CriticalAlerts = componentAlerts.Count(a => a.CriticalThreshold.HasValue && a.Status == 3),
                        WarningAlerts  = componentAlerts.Count(a => a.WarningThreshold.HasValue && a.Status == 2),
                        InfoAlerts     = componentAlerts.Count(a => a.InfoThreshold.HasValue && a.Status == 1)
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

            foreach(var item in summaryData)
            {
                ws.Cell(row, 1).Value = item.ComponentType;
                ws.Cell(row, 2).Value = item.Category;
                ws.Cell(row, 3).Value = item.ComponentCount;
                ws.Cell(row, 4).Value = item.CriticalAlerts;
                ws.Cell(row, 5).Value = item.WarningAlerts;
                ws.Cell(row, 6).Value = item.InfoAlerts;
                ApplyAlertColor(ws.Cell(row, 4), item.CriticalAlerts, AlertSeverity.Critical);
                ApplyAlertColor(ws.Cell(row, 5), item.WarningAlerts, AlertSeverity.Warning);
                ApplyAlertColor(ws.Cell(row, 6), item.InfoAlerts, AlertSeverity.Info);
                row++;
            }

            var summaryTableLastRow = row - 1;

            // Add index section
            row += 2;
            ws.Cell(row, 1).Value = "Sheet Index";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Inputs"), "Inputs", "Report parameters and metadata");

            // Add links to component sheets grouped by type with their metrics
            row += 1;
            ws.Cell(row, 1).Value = "Components by Type (with Metrics)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            var componentsByType = report.Components
                .GroupBy(c => c.Type ?? "Unknown")
                .OrderBy(g => g.Key);

            foreach(var group in componentsByType)
            {
                var componentType = group.Key;
                var componentIds = group.Select(c => c.ComponentId).ToHashSet();

                // Link to component summary
                var logicalName = SheetRegistry.BuildComponentTypeSheetName(componentType);
                var label = $"{componentType} Components";
                var description = $"{group.Count()} {componentType} component(s)";
                WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName(logicalName), label, description);

                // Links to metrics for this component type (normalized for grouping)
                var metricsForType = report.Metrics
                    .Where(m => componentIds.Contains(m.ComponentId))
                    .Select(m => NormalizeMetricName(m.MetricName ?? "Unknown"))
                    .Distinct()
                    .OrderBy(m => m);

                foreach(var metricName in metricsForType)
                {
                    // Build sheet name using BuildMetricByTypeSheetName (consistent with other parts)
                    var logicalSheetBase = SheetRegistry.BuildMetricByTypeSheetName(componentType, metricName);
                    var summarySheet = sheetRegistry.GetOrCreatePhysicalName(logicalSheetBase);
                    var timeSeriesSheet = sheetRegistry.GetOrCreatePhysicalName($"TS{logicalSheetBase}");

                    // Display with spaces in the index
                    WriteIndexLink(ws, row++, summarySheet, $"  • {metricName} - Summary", "Aggregated data and alerts");
                    WriteIndexLink(ws, row++, timeSeriesSheet, $"  • {metricName} - Time Series", "Max every 15 min with chart");
                }

                row++; // Blank line between component types
            }

            row += 1;
            WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Alerts"), "Alerts", "All alerts across components");

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(summaryTableLastRow);
        }

        /// <summary>
        /// Normalizes metric names by removing specific suffixes for grouping and consolidation.
        /// </summary>
        /// <param name="metricName">The metric name to normalize.</param>
        /// <returns>The normalized metric name if it matches a known pattern, otherwise the original name.</returns>
        /// <remarks>
        /// <para>
        /// This method detects and normalizes metrics that follow patterns with dynamic suffixes:
        /// <c>"[Prefix] - [Dynamic Suffix]"</c>
        /// </para>
        /// <para>
        /// Supported metric patterns for consolidation:
        /// <list type="bullet">
        /// <item><description><c>"Process CPU Used - processname.exe"</c> → <c>"Process CPU Used"</c></description></item>
        /// <item><description><c>"Process CPU Utilized - processname.exe"</c> → <c>"Process CPU Utilized"</c></description></item>
        /// <item><description><c>"Process Instances - processname.exe"</c> → <c>"Process Instances"</c></description></item>
        /// <item><description><c>"Process Memory Used - processname.exe"</c> → <c>"Process Memory Used"</c></description></item>
        /// <item><description><c>"Process Memory Utilized - processname.exe"</c> → <c>"Process Memory Utilized"</c></description></item>
        /// <item><description><c>"Database Connections - dbname"</c> → <c>"Database Connections"</c></description></item>
        /// <item><description><c>"Groups Access - groupname"</c> → <c>"Groups Access"</c></description></item>
        /// <item><description><c>"Health Check Status - checkname"</c> → <c>"Health Check Status"</c></description></item>
        /// <item><description><c>"Items Access - itemname"</c> → <c>"Items Access"</c></description></item>
        /// <item><description><c>"Items Active - typename"</c> → <c>"Items Active"</c></description></item>
        /// <item><description><c>"Items Type - typename"</c> → <c>"Items Type"</c></description></item>
        /// <item><description><c>"Licenses Type Available - licensename"</c> → <c>"Licenses Type Available"</c></description></item>
        /// <item><description><c>"Licenses Type Used - licensename"</c> → <c>"Licenses Type Used"</c></description></item>
        /// <item><description><c>"Licenses Type Utilized - licensename"</c> → <c>"Licenses Type Utilized"</c></description></item>
        /// <item><description><c>"Users Active - username"</c> → <c>"Users Active"</c></description></item>
        /// <item><description><c>"Users Active Type - typename"</c> → <c>"Users Active Type"</c></description></item>
        /// <item><description><c>"GDB Connections - dbname"</c> → <c>"GDB Connections"</c></description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The normalization process:
        /// <list type="number">
        /// <item><description>Detects if metric starts with a known prefix pattern</description></item>
        /// <item><description>Removes the dynamic suffix (everything after the hyphen)</description></item>
        /// <item><description>Trims trailing spaces and hyphens</description></item>
        /// <item><description>Non-matching metrics return their original name</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// NormalizeMetricName("Process CPU Used - chrome.exe"); // Returns: "Process CPU Used"
        /// NormalizeMetricName("Database Connections - mydb"); // Returns: "Database Connections"
        /// NormalizeMetricName("CPU Utilized"); // Returns: "CPU Utilized"
        /// </code>
        /// </example>
        private static string NormalizeMetricName(string metricName)
        {
            if(string.IsNullOrWhiteSpace(metricName))
            {
                return metricName;
            }

            // Metric prefixes to detect and consolidate (case-insensitive)
            var consolidationPrefixes = new[]
            {
                "Process CPU Used -",
                "Process CPU Utilized -",
                "Process Instances -",
                "Process Memory Used -",
                "Process Memory Utilized -",
                "Database Connections -",
                "Groups Access -",
                "Health Check Status -",
                "Items Access -",
                "Items Active -",
                "Items Type -",
                "Licenses Type Available -",
                "Licenses Type Used -",
                "Licenses Type Utilized -",
                "Users Active -",
                "Users Active Type -",
                "GDB Connections -"
            };

            // Check if metric matches any consolidation prefix
            foreach(var prefix in consolidationPrefixes)
            {
                if(metricName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the dynamic suffix (trim trailing spaces and hyphen)
                    var normalized = prefix.TrimEnd(' ', '-');
                    Log.Debug("Normalized metric for consolidation: '{Original}' → '{Normalized}'", metricName, normalized);
                    return normalized;
                }
            }

            // Not a consolidation metric, return original
            return metricName;
        }

        /// <summary>
        /// Writes component sheets with their associated metrics intercalated.
        /// For each component type: creates summary sheet, then all metric sheets for that type.
        /// </summary>
        /// <param name="workbook">The Excel workbook where sheets will be added.</param>
        /// <param name="sheetRegistry">Sheet name registry to manage duplicates and truncation.</param>
        /// <param name="report">The report with components to write.</param>
        /// <remarks>
        /// <para>
        /// Structure: ComponentType1 → Metric1 (Summary + TS) → Metric2 (Summary + TS) → ComponentType2 → ...
        /// This creates a logical grouping where all metrics for a component type appear immediately after that component.
        /// </para>
        /// <para>
        /// Metrics with dynamic suffixes are automatically consolidated:
        /// <list type="bullet">
        /// <item><description>"Process CPU Used - chrome.exe", "Process CPU Used - firefox.exe" → Single "Process CPU Used" sheet</description></item>
        /// <item><description>"Database Connections - db1", "Database Connections - db2" → Single "Database Connections" sheet</description></item>
        /// <item><description>"Licenses Type Used - license1", "Licenses Type Used - license2" → Single "Licenses Type Used" sheet</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        private static void WriteComponentsWithMetricsSheets(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
        {
            var componentsByType = report.Components
                .GroupBy(c => c.Type ?? "Unknown")
                .OrderBy(g => g.Key);

            foreach(var group in componentsByType)
            {
                var componentType = group.Key;

                try
                {
                    var componentsList = group.OrderBy(c => c.Name).ToList();
                    var componentIds = componentsList.Select(c => c.ComponentId).ToHashSet();

                    Log.Information("Writing sheets for component type '{ComponentType}' with {Count} components",
                        componentType, componentsList.Count);

                    // 1. Create component summary sheet
                    var logicalName = SheetRegistry.BuildComponentTypeSheetName(componentType);
                    var physicalName = sheetRegistry.GetOrCreatePhysicalName(logicalName);
                    var ws = workbook.Worksheets.Add(physicalName);
                    WriteBackToIndex(ws);

                    // Title
                    ws.Cell(2, 1).Value = $"{componentType} Components";
                    ws.Cell(2, 1).Style.Font.Bold = true;
                    ws.Cell(2, 1).Style.Font.FontSize = 14;
                    ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196); // Blue
                    ws.Cell(2, 1).Style.Font.FontColor = XLColor.White;

                    // Write components table with non-empty columns only
                    WriteComponentsSummaryTable(ws, 4, componentsList);

                    ws.Columns().AdjustToContents();

                    Log.Information("Successfully created component summary sheet for type '{ComponentType}'", componentType);

                    // 2. Create metric sheets for this component type (grouped by normalized name)
                    var metricsForType = report.Metrics
                        .Where(m => componentIds.Contains(m.ComponentId))
                        .GroupBy(m => NormalizeMetricName(m.MetricName ?? "Unknown"))
                        .OrderBy(g => g.Key);

                    foreach(var metricGroup in metricsForType)
                    {
                        var metricName = metricGroup.Key;
                        var metricsForName = metricGroup.ToList();

                        try
                        {
                            Log.Debug("Processing metric '{MetricName}' for component type '{ComponentType}' ({Count} instances)",
                                metricName, componentType, metricsForName.Count);

                            // Sheet 1: Metric Summary
                            WriteMetricSummarySheet(workbook, sheetRegistry, componentType, metricName, metricsForName, report);

                            // Sheet 2: Time Series
                            WriteMetricTimeSeriesSheet(workbook, sheetRegistry, componentType, metricName, metricsForName, report);
                        }
                        catch(Exception ex)
                        {
                            Log.Error(ex, "Failed to create sheets for metric '{MetricName}' in component type '{ComponentType}'",
                                metricName, componentType);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Failed to create sheets for component type '{ComponentType}'", componentType);

                    // Create error sheet
                    try
                    {
                        var errorSheetName = sheetRegistry.GetOrCreatePhysicalName($"ERROR_{componentType}");
                        var errorWs = workbook.Worksheets.Add(errorSheetName);
                        errorWs.Cell(1, 1).Value = $"Error creating sheet for {componentType}";
                        errorWs.Cell(2, 1).Value = ex.Message;
                        errorWs.Cell(3, 1).Value = ex.StackTrace;
                    }
                    catch
                    {
                        Log.Error("Failed to create error sheet for component type '{ComponentType}'", componentType);
                    }
                }
            }
        }

        /// <summary>
        /// Writes a metric summary sheet with characteristics, aggregated data, and alerts.
        /// </summary>
        private static void WriteMetricSummarySheet(
            XLWorkbook workbook,
            SheetRegistry sheetRegistry,
            string componentType,
            string metricName,
            List<MetricReportRow> metrics,
            MonitorExcelReport report)
        {
            // Use the same method as in WriteSummary to ensure sheet names match links
            var logicalName = SheetRegistry.BuildMetricByTypeSheetName(componentType, metricName);
            var physicalName = sheetRegistry.GetOrCreatePhysicalName(logicalName);
            var ws = workbook.Worksheets.Add(physicalName);
            WriteBackToIndex(ws);

            try
            {
                var row = 2;

                // Title (metricName is already normalized for process metrics, keep spaces)
                ws.Cell(row, 1).Value = $"Metric: {metricName} ({componentType})";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 16;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
                row += 2;

                WriteMetricComponentDataTable(ws, row, metrics, report.MetricData, report.Alerts);

                ws.Columns().AdjustToContents();
                ws.SheetView.FreezeRows(4);
                ws.SheetView.FreezeColumns(1);
                Log.Debug("Created metric summary sheet for '{MetricName}' in '{ComponentType}'", metricName, componentType);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to write metric summary sheet for '{MetricName}' in '{ComponentType}'", metricName, componentType);
                ws.Cell(2, 1).Value = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Writes a table with component-level data for a metric including aggregates and alerts.
        /// </summary>
        private static void WriteMetricComponentDataTable(
            IXLWorksheet ws,
            int startRow,
            List<MetricReportRow> metrics,
            List<MetricDataReportRow> metricData,
            List<AlertReportRow> alerts)
        {
            // Deduplicate metrics by MetricId and order by ComponentId, then MetricId
            var uniqueMetrics = metrics
                .GroupBy(m => m.MetricId)
                .Select(g => g.First())
                .OrderBy(m => m.ComponentId)
                .ThenBy(m => m.MetricId)
                .ToList();

            if(uniqueMetrics.Count != metrics.Count)
            {
                Log.Warning("Removed {Count} duplicate metric(s) in metric component data table. Original: {Original}, Unique: {Unique}",
                    metrics.Count - uniqueMetrics.Count, metrics.Count, uniqueMetrics.Count);
            }

            // Check if this is a consolidated metric (multiple unique metric names exist)
            var uniqueMetricNames = uniqueMetrics
                .Select(m => m.MetricName ?? "Unknown")
                .Distinct()
                .Count();
            var isConsolidatedMetric = uniqueMetricNames > 1;

            if(isConsolidatedMetric)
            {
                Log.Debug("Detected consolidated metric sheet with {Count} unique metric names. Will filter out metrics with all null aggregations.",
                    uniqueMetricNames);
            }

            var headers = new[]
            {
                "Component Id", "Component Name", "Metric Name", "Metric Id", "Component Type", "Component SubType",
                "Unit",
                "Count", "Min", "Max", "Avg", "StdDev", "P95",
                "Critical Alerts", "Warning Alerts", "Info Alerts",
                "Alerting Enabled", "Aggregation", "Operator",
                "Critical Threshold", "Warning Threshold", "Info Threshold", "Samples"
            };

            // Write headers
            for(var i = 0; i < headers.Length; i++)
            {
                ws.Cell(startRow, i + 1).Value = headers[i];
                ws.Cell(startRow, i + 1).Style.Font.Bold = true;
                ws.Cell(startRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Build data rows (using deduplicated metrics)
            var currentRow = startRow + 1;
            var excludedCount = 0;

            foreach(var metric in uniqueMetrics)
            {
                // La API devuelve un registro agregado por métrica con groupbyFieldsForStatistics
                var aggregatedData = metricData.FirstOrDefault(md => md.MetricId == metric.MetricId);

                // Usar directamente los valores agregados de la API
                var count = aggregatedData?.CountValue ?? 0;
                var minValue = aggregatedData?.MinValue;
                var maxValue = aggregatedData?.MaxValue;
                var avgValue = aggregatedData?.AvgValue;
                var stdDevValue = aggregatedData?.StdDevValue;

                // For consolidated metrics, skip rows where all aggregations are null
                if(isConsolidatedMetric)
                {
                    var hasAnyData = count > 0 ||
                                    minValue.HasValue ||
                                    maxValue.HasValue ||
                                    avgValue.HasValue ||
                                    stdDevValue.HasValue;

                    if(!hasAnyData)
                    {
                        Log.Debug("Excluding metric '{MetricName}' (ID: {MetricId}) from consolidated sheet: all aggregations are null",
                            metric.MetricName, metric.MetricId);
                        excludedCount++;
                        continue; // Skip this metric
                    }
                }

                // P95 calculation using exact z-score for normal distribution with max constraint
                var p95Value = StatisticsCalculator.CalculatePercentile95(avgValue, stdDevValue, maxValue);

                // Count alerts for this component
                var componentAlerts = alerts.Where(a => a.ComponentId == metric.ComponentId && a.MetricId == metric.MetricId).ToList();
                var infoAlerts     = componentAlerts.Count(a => a.InfoThreshold.HasValue && a.Status == 1);
                var warningAlerts = componentAlerts.Count(a => a.WarningThreshold.HasValue && a.Status == 2);
                var criticalAlerts = componentAlerts.Count(a => a.CriticalThreshold.HasValue && a.Status == 3);

                // Write row - Identification columns first
                ws.Cell(currentRow, 1).Value = metric.ComponentId;
                ws.Cell(currentRow, 2).Value = metric.ComponentName ?? "Unknown";
                ws.Cell(currentRow, 3).Value = metric.MetricName ?? "Unknown";
                ws.Cell(currentRow, 4).Value = metric.MetricId;
                ws.Cell(currentRow, 5).Value = metric.ComponentType ?? "Unknown";
                ws.Cell(currentRow, 6).Value = metric.ComponentSubtype ?? string.Empty;

                // Unit
                ws.Cell(currentRow, 7).Value = metric.Unit ?? string.Empty;

                // Statistical data
                ws.Cell(currentRow, 8).Value = count;
                SetCellValue(ws.Cell(currentRow, 9), minValue);
                SetCellValue(ws.Cell(currentRow, 10), maxValue);
                SetCellValue(ws.Cell(currentRow, 11), avgValue);
                SetCellValue(ws.Cell(currentRow, 12), stdDevValue);
                SetCellValue(ws.Cell(currentRow, 13), p95Value);

                // Alert counts
                ws.Cell(currentRow, 14).Value = criticalAlerts;
                ws.Cell(currentRow, 15).Value = warningAlerts;
                ws.Cell(currentRow, 16).Value = infoAlerts;
                ApplyAlertColor(ws.Cell(currentRow, 14), criticalAlerts, AlertSeverity.Critical);
                ApplyAlertColor(ws.Cell(currentRow, 15), warningAlerts, AlertSeverity.Warning);
                ApplyAlertColor(ws.Cell(currentRow, 16), infoAlerts, AlertSeverity.Info);

                // Alerting configuration
                ws.Cell(currentRow, 17).Value = metric.IsAlertingEnabled ?? false;
                ws.Cell(currentRow, 18).Value = metric.Aggregation ?? string.Empty;
                ws.Cell(currentRow, 19).Value = metric.Operator ?? string.Empty;

                // Thresholds and samples
                SetCellValue(ws.Cell(currentRow, 20), metric.CriticalThreshold);
                SetCellValue(ws.Cell(currentRow, 21), metric.WarningThreshold);
                SetCellValue(ws.Cell(currentRow, 22), metric.InfoThreshold);
                SetCellValue(ws.Cell(currentRow, 23), metric.Samples);

                // Format numeric cells (statistical data and thresholds)
                for(var col = 8; col <= 13; col++)
                {
                    if(ws.Cell(currentRow, col).Value.IsNumber)
                    {
                        ws.Cell(currentRow, col).Style.NumberFormat.Format = "#,##0.00";
                    }
                }

                for(var col = 20; col <= 22; col++)
                {
                    if(ws.Cell(currentRow, col).Value.IsNumber)
                    {
                        ws.Cell(currentRow, col).Style.NumberFormat.Format = "#,##0.00";
                    }
                }

                currentRow++;
            }

            if(excludedCount > 0)
            {
                Log.Information("Excluded {Count} metric(s) with all null aggregations from consolidated metric sheet", excludedCount);
            }

            // Create table
            if(currentRow > startRow + 1)
            {
                var tableRange = ws.Range(startRow, 1, currentRow - 1, headers.Length);
                var table = tableRange.CreateTable();
                table.Theme = XLTableTheme.TableStyleMedium9;
            }
        }

        /// <summary>
        /// Writes a metric time series sheet with max values every 15 minutes by component.
        /// </summary>
        private static void WriteMetricTimeSeriesSheet(
            XLWorkbook workbook,
            SheetRegistry sheetRegistry,
            string componentType,
            string metricName,
            List<MetricReportRow> metrics,
            MonitorExcelReport report)
        {
            // Use the same method as in WriteSummary with "TS" prefix to ensure sheet names match links
            var baseLogicalName = SheetRegistry.BuildMetricByTypeSheetName(componentType, metricName);
            var logicalName = $"TS{baseLogicalName}";
            var physicalName = sheetRegistry.GetOrCreatePhysicalName(logicalName);
            var ws = workbook.Worksheets.Add(physicalName);
            WriteBackToIndex(ws);

            try
            {
                var row = 2;

                // Title (metricName is already normalized for process metrics, keep spaces)
                ws.Cell(row, 1).Value = $"Time Series: {metricName} ({componentType}) - Max every {report.MetricDataBucket}";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 16;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
                row += 2;

                // Get time series data for this metric
                var metricIds = metrics.Select(m => m.MetricId).ToHashSet();
                var timeSeriesData = report.TimeSeriesMetricData
                    .Where(md => metricIds.Contains(md.MetricId) && md.ObservedAt.HasValue && md.MaxValue.HasValue)
                    .OrderBy(md => md.ObservedAt)
                    .ToList();

                if(timeSeriesData.Count == 0)
                {
                    ws.Cell(row, 1).Value = "No time series data available";
                    ws.SheetView.FreezeRows(4);
                    ws.SheetView.FreezeColumns(1);
                    Log.Warning("No time series data found for metric '{MetricName}' in '{ComponentType}'", metricName, componentType);
                    return;
                }

                // Deduplicate time series data by MetricId + ObservedAt (keep first occurrence)
                var uniqueTimeSeriesData = timeSeriesData
                    .GroupBy(md => new { md.MetricId, md.ObservedAt })
                    .Select(g => g.First())
                    .OrderBy(md => md.ObservedAt)
                    .ToList();

                if(uniqueTimeSeriesData.Count != timeSeriesData.Count)
                {
                    Log.Warning("Removed {Count} duplicate time series data point(s) for metric '{MetricName}' in '{ComponentType}'. Original: {Original}, Unique: {Unique}",
                        timeSeriesData.Count - uniqueTimeSeriesData.Count, metricName, componentType, timeSeriesData.Count, uniqueTimeSeriesData.Count);
                    timeSeriesData = uniqueTimeSeriesData;
                }

                // Pivot: rows = timestamps, columns = components
                var timestamps = timeSeriesData.Select(md => md.ObservedAt!.Value).Distinct().OrderBy(t => t).ToList();

                // Deduplicate metrics by MetricId and order by ComponentId, then MetricId
                var uniqueMetrics = metrics
                    .GroupBy(m => m.MetricId)
                    .Select(g => g.First())
                    .OrderBy(m => m.ComponentId)
                    .ThenBy(m => m.MetricId)
                    .ToList();

                if(uniqueMetrics.Count != metrics.Count)
                {
                    Log.Warning("Removed {Count} duplicate metric(s) in time series for '{MetricName}' in '{ComponentType}'. Original: {Original}, Unique: {Unique}",
                        metrics.Count - uniqueMetrics.Count, metricName, componentType, metrics.Count, uniqueMetrics.Count);
                }

                var components = uniqueMetrics;

                // Deduplicate column names for components
                var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var finalComponentColumns = new List<(MetricReportRow Component, string ColumnName)>();

                // Check if this is a grouped/consolidated metric (normalized name differs from original)
                var isGroupedMetric = components.Any(c =>
                    NormalizeMetricName(c.MetricName ?? "") != (c.MetricName ?? ""));

                foreach(var component in components)
                {
                    string baseName;

                    if(isGroupedMetric)
                    {
                        // For grouped/consolidated metrics, use: ComponentName - MetricName
                        baseName = $"{component.ComponentName ?? $"Component_{component.ComponentId}"} - {component.MetricName ?? "Unknown"}";
                    }
                    else
                    {
                        // For regular metrics, use just ComponentName
                        baseName = component.ComponentName ?? $"Component_{component.ComponentId}";
                    }

                    if(columnNames.TryGetValue(baseName, out var value))
                    {
                        columnNames[baseName] = ++value;
                        var uniqueName = $"{baseName}_{value}";
                        finalComponentColumns.Add((component, uniqueName));
                        Log.Warning("Duplicate component name '{ComponentName}' in time series for metric '{MetricName}', renamed to '{UniqueName}' in sheet '{SheetName}'",
                            baseName, metricName, uniqueName, ws.Name);
                    }
                    else
                    {
                        columnNames[baseName] = 0;
                        finalComponentColumns.Add((component, baseName));
                    }
                }

                // Detect if this is a consolidated metric sheet (multiple unique metric names)
                var uniqueMetricNames = uniqueMetrics
                    .Select(m => m.MetricName ?? "Unknown")
                    .Distinct()
                    .Count();
                var isConsolidatedMetric = uniqueMetricNames > 1;

                // Build data matrix and track columns with all null values
                var dataMatrix = new Dictionary<DateTimeOffset, Dictionary<int, double?>>();
                var columnHasData = new Dictionary<int, bool>();

                // Initialize column tracking
                for(var i = 0; i < finalComponentColumns.Count; i++)
                {
                    columnHasData[i] = false;
                }

                // Populate data matrix and detect non-null columns
                foreach(var timestamp in timestamps)
                {
                    var rowData = new Dictionary<int, double?>();

                    for(var i = 0; i < finalComponentColumns.Count; i++)
                    {
                        var (component, _) = finalComponentColumns[i];
                        var value = timeSeriesData
                            .FirstOrDefault(md => md.MetricId == component.MetricId && md.ObservedAt == timestamp)
                            ?.MaxValue;

                        rowData[i] = value;

                        if(value.HasValue)
                        {
                            columnHasData[i] = true;
                        }
                    }

                    dataMatrix[timestamp] = rowData;
                }

                // Filter columns: in consolidated sheets, exclude columns with all null values
                var columnsToInclude = new List<(int OriginalIndex, MetricReportRow Component, string ColumnName)>();
                var excludedColumnsCount = 0;

                for(var i = 0; i < finalComponentColumns.Count; i++)
                {
                    var (component, columnName) = finalComponentColumns[i];

                    if(isConsolidatedMetric && !columnHasData[i])
                    {
                        Log.Debug("Excluding column '{ColumnName}' (Metric ID: {MetricId}): all time series values are null",
                            columnName, component.MetricId);
                        excludedColumnsCount++;
                        continue;
                    }

                    columnsToInclude.Add((i, component, columnName));
                }

                if(excludedColumnsCount > 0)
                {
                    Log.Information("Excluded {Count} column(s) with all null values from consolidated time series sheet", excludedColumnsCount);
                }

                // Write headers using filtered columns
                ws.Cell(row, 1).Value = "ObservedAt";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

                var col = 2;
                foreach(var (_, _, columnName) in columnsToInclude)
                {
                    ws.Cell(row, col).Value = columnName;
                    ws.Cell(row, col).Style.Font.Bold = true;
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                    col++;
                }

                // Write data rows using filtered columns
                var currentRow = row + 1;
                foreach(var timestamp in timestamps)
                {
                    ws.Cell(currentRow, 1).Value = timestamp.UtcDateTime;
                    ws.Cell(currentRow, 1).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";

                    col = 2;
                    foreach(var (originalIndex, component, _) in columnsToInclude)
                    {
                        var value = dataMatrix[timestamp][originalIndex];

                        if(value.HasValue)
                        {
                            ws.Cell(currentRow, col).Value = value.Value;
                            ws.Cell(currentRow, col).Style.NumberFormat.Format = "#,##0.00";
                        }
                        col++;
                    }
                    currentRow++;
                }

                // Create table with filtered columns
                if(currentRow > row + 1 && columnsToInclude.Count > 0)
                {
                    var tableRange = ws.Range(row, 1, currentRow - 1, columnsToInclude.Count + 1);
                    var table = tableRange.CreateTable();
                    table.Theme = XLTableTheme.TableStyleMedium9;

                    // Add chart placeholder note
                    var chartRow = currentRow + 2;
                    AddChartPlaceholder(ws, chartRow, componentType, metricName, table);
                }

                ws.Columns().AdjustToContents();
                ws.SheetView.FreezeRows(4);
                ws.SheetView.FreezeColumns(1);
                Log.Debug("Created time series sheet for '{MetricName}' in '{ComponentType}'", metricName, componentType);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to write time series sheet for '{MetricName}' in '{ComponentType}'", metricName, componentType);
                ws.Cell(2, 1).Value = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds a chart placeholder with instructions for creating a line chart.
        /// </summary>
        /// <param name="ws">The worksheet</param>
        /// <param name="chartRow">Row where the chart note will be placed</param>
        /// <param name="componentType">Component type</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="table">The data table</param>
        /// <remarks>
        /// ClosedXML does not support native chart creation. This method adds formatted instructions
        /// for users to manually create a line chart. For automated chart creation, consider using
        /// DocumentFormat.OpenXml directly or EPPlus library as a post-processing step after saving
        /// the file with ClosedXML.
        /// </remarks>
        private static void AddChartPlaceholder(IXLWorksheet ws, int chartRow, string componentType, string metricName, IXLTable table)
        {
            try
            {
                Log.Debug("Adding chart placeholder for '{MetricName}' in '{ComponentType}'", metricName, componentType);

                // Title for chart section
                ws.Cell(chartRow, 1).Value = "📊 Chart Instructions";
                ws.Cell(chartRow, 1).Style.Font.Bold = true;
                ws.Cell(chartRow, 1).Style.Font.FontSize = 12;
                ws.Cell(chartRow, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
                chartRow++;

                // Instructions
                ws.Cell(chartRow, 1).Value = "To create a line chart for this time series data:";
                ws.Cell(chartRow, 1).Style.Font.Bold = true;
                chartRow++;

                var instructions = new[]
                {
                            $"1. Select the table '{table.Name}' above",
                            "2. Go to Insert > Charts > Line Chart",
                            "3. Excel will automatically use the first column (ObservedAt) as X-axis",
                            "4. All component columns will be plotted as separate lines",
                            "5. Optionally, format the chart title, axis labels, and legend",
                            "",
                            "Alternative: Use a pivot chart for interactive filtering by component"
                        };

                foreach(var instruction in instructions)
                {
                    if(string.IsNullOrEmpty(instruction))
                    {
                        chartRow++;
                        continue;
                    }

                    ws.Cell(chartRow, 1).Value = instruction;
                    ws.Cell(chartRow, 1).Style.Font.FontSize = 10;

                    if(instruction.StartsWith("Alternative:"))
                    {
                        ws.Cell(chartRow, 1).Style.Font.Italic = true;
                        ws.Cell(chartRow, 1).Style.Font.FontColor = XLColor.DarkBlue;
                    }

                    chartRow++;
                }

                // Technical note
                chartRow++;
                ws.Cell(chartRow, 1).Value = "Note: Automated chart creation requires post-processing with DocumentFormat.OpenXml or EPPlus.";
                ws.Cell(chartRow, 1).Style.Font.FontSize = 9;
                ws.Cell(chartRow, 1).Style.Font.Italic = true;
                ws.Cell(chartRow, 1).Style.Font.FontColor = XLColor.Gray;

                Log.Debug("Successfully added chart placeholder for '{MetricName}' in '{ComponentType}'", metricName, componentType);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to add chart placeholder for '{MetricName}' in '{ComponentType}'", metricName, componentType);
            }
        }

        /// <summary>
        /// Writes the components summary table with only non-empty columns.
        /// </summary>
        private static int WriteComponentsSummaryTable(IXLWorksheet ws, int startRow, List<ComponentReportRow> components)
        {
            if(components.Count == 0)
            {
                ws.Cell(startRow, 1).Value = "No components found";
                return startRow + 1;
            }

            try
            {
                // Deduplicate components by ComponentId
                var uniqueComponents = components
                    .GroupBy(c => c.ComponentId)
                    .Select(g => g.First())
                    .OrderBy(c => c.Name)
                    .ToList();

                if(uniqueComponents.Count != components.Count)
                {
                    Log.Warning("Removed {Count} duplicate component(s) in sheet '{SheetName}'. Original: {Original}, Unique: {Unique}",
                        components.Count - uniqueComponents.Count, ws.Name, components.Count, uniqueComponents.Count);
                }

                // Get all properties (only declared in this class, not inherited)
                var properties = typeof(ComponentReportRow).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                Log.Debug("Found {Count} properties in ComponentReportRow: {Properties}",
                    properties.Length, string.Join(", ", properties.Select(p => p.Name)));

                // Filter properties that have at least one non-null, non-empty value
                var nonEmptyProperties = properties
                    .Where(p => uniqueComponents.Any(c =>
                    {
                        var value = p.GetValue(c);
                        return value != null &&
                               !(value is string str && string.IsNullOrWhiteSpace(str)) &&
                               !(value is int intVal && intVal == 0 && p.Name.EndsWith("Count"));
                    }))
                    .ToList();

                Log.Debug("Filtered to {Count} non-empty properties: {Properties}",
                    nonEmptyProperties.Count, string.Join(", ", nonEmptyProperties.Select(p => p.Name)));

                // Create unique column names (in case of duplicates)
                var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var finalColumnNames = new List<(PropertyInfo Property, string ColumnName)>();

                foreach(var prop in nonEmptyProperties)
                {
                    var baseName = prop.Name;

                    if(columnNames.TryGetValue(baseName, out var value))
                    {
                        columnNames[baseName] = ++value;
                        var uniqueName = $"{baseName}_{value}";
                        finalColumnNames.Add((prop, uniqueName));
                        Log.Warning("Duplicate column name detected: '{ColumnName}', renamed to '{UniqueName}' in sheet '{SheetName}'",
                            baseName, uniqueName, ws.Name);
                    }
                    else
                    {
                        columnNames[baseName] = 0;
                        finalColumnNames.Add((prop, baseName));
                    }
                }

                if(finalColumnNames.Count == 0)
                {
                    ws.Cell(startRow, 1).Value = "No displayable columns found";
                    Log.Warning("No displayable columns found for components in sheet '{SheetName}'", ws.Name);
                    return startRow + 1;
                }

                Log.Debug("Final column names for sheet '{SheetName}': {Columns}",
                    ws.Name, string.Join(", ", finalColumnNames.Select(c => c.ColumnName)));

                // Write headers
                var col = 1;
                foreach(var (_, columnName) in finalColumnNames)
                {
                    ws.Cell(startRow, col).Value = columnName;
                    ws.Cell(startRow, col).Style.Font.Bold = true;
                    ws.Cell(startRow, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                    col++;
                }

                // Write data rows (using deduplicated components)
                var currentRow = startRow + 1;
                foreach(var component in uniqueComponents)
                {
                    col = 1;
                    foreach(var (property, _) in finalColumnNames)
                    {
                        try
                        {
                            var value = property.GetValue(component);
                            SetCellValue(ws.Cell(currentRow, col), value);
                        }
                        catch(Exception ex)
                        {
                            Log.Warning(ex, "Failed to get value for property '{Property}' on component '{Component}'",
                                property.Name, component.Name ?? component.ComponentId.ToString());
                            ws.Cell(currentRow, col).Value = "#ERROR#";
                        }
                        col++;
                    }
                    currentRow++;
                }

                // Create table
                if(currentRow > startRow + 1 && finalColumnNames.Count > 0)
                {
                    try
                    {
                        var tableRange = ws.Range(startRow, 1, currentRow - 1, finalColumnNames.Count);
                        var table = tableRange.CreateTable();
                        table.Theme = XLTableTheme.TableStyleMedium9;

                        Log.Debug("Created table in sheet '{SheetName}' with {Rows} rows and {Columns} columns",
                            ws.Name, currentRow - startRow - 1, finalColumnNames.Count);
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex, "Failed to create table in sheet '{SheetName}' with range ({StartRow},{StartCol}) to ({EndRow},{EndCol})",
                            ws.Name, startRow, 1, currentRow - 1, finalColumnNames.Count);
                        throw;
                    }
                }

                ws.SheetView.FreezeRows(startRow);
                ws.SheetView.FreezeColumns(1);

                return currentRow;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to write components summary table in sheet '{SheetName}'", ws.Name);
                ws.Cell(startRow, 1).Value = $"Error creating table: {ex.Message}";
                return startRow + 2;
            }
        }

        /// <summary>
        /// Writes the Alerts sheet with severity labels and color coding.
        /// Replaces the numeric Status code (1=info, 2=warning, 3=critical) with a labeled, colored cell.
        /// </summary>
        private static void WriteAlertsSheet(XLWorkbook workbook, SheetRegistry sheetRegistry, List<AlertReportRow> alerts)
        {
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName("Alerts"));
            WriteBackToIndex(ws);

            if(alerts.Count == 0)
            {
                ws.Cell(3, 1).Value = "No alerts.";
                return;
            }

            // Deduplicate by AlertId
            var rows = alerts
                .GroupBy(a => a.AlertId)
                .Select(g => g.First())
                .ToList();

            const int headerRow = 3;

            string[] headers =
            [
                "Collection", "AlertId", "MetricId", "MetricName",
                "ComponentId", "ComponentName", "State", "Severity",
                "OpenedAt", "ClosedAt", "Operator",
                "InfoThreshold", "WarningThreshold", "CriticalThreshold", "Duration"
            ];

            for(var col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(headerRow, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            var row = headerRow + 1;
            foreach(var alert in rows)
            {
                var severity = ResolveSeverity(alert);

                ws.Cell(row, 1).Value = alert.CollectionName;
                ws.Cell(row, 2).Value = alert.AlertId;
                SetCellValue(ws.Cell(row, 3), alert.MetricId);
                ws.Cell(row, 4).Value = alert.MetricName ?? string.Empty;
                SetCellValue(ws.Cell(row, 5), alert.ComponentId);
                ws.Cell(row, 6).Value = alert.ComponentName ?? string.Empty;
                ws.Cell(row, 7).Value = alert.State ?? string.Empty;

                // Severity column: label with color instead of raw Status int
                var severityCell = ws.Cell(row, 8);
                severityCell.Value = severity ?? string.Empty;
                if(severity != null)
                {
                    ApplySeverityColor(severityCell, severity);
                }

                SetCellValue(ws.Cell(row, 9), alert.OpenedAt);
                SetCellValue(ws.Cell(row, 10), alert.ClosedAt);
                ws.Cell(row, 11).Value = alert.Operator ?? string.Empty;
                SetCellValue(ws.Cell(row, 12), alert.InfoThreshold);
                SetCellValue(ws.Cell(row, 13), alert.WarningThreshold);
                SetCellValue(ws.Cell(row, 14), alert.CriticalThreshold);
                SetCellValue(ws.Cell(row, 15), alert.Duration);

                row++;
            }

            var usedRange = ws.Range(headerRow, 1, Math.Max(row - 1, headerRow), headers.Length);
            usedRange.CreateTable();
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(headerRow);
            ws.SheetView.FreezeColumns(1);
        }

        /// <summary>
        /// Resolves the severity label for an alert.
        /// Prefers the <see cref="AlertReportRow.State"/> string; falls back to the numeric
        /// <see cref="AlertReportRow.Status"/> code (1=info, 2=warning, 3=critical).
        /// </summary>
        private static string? ResolveSeverity(AlertReportRow alert)
        {
            if(!string.IsNullOrWhiteSpace(alert.State))
            {
                var lower = alert.State.ToLowerInvariant();
                if(lower is "info" or "warning" or "critical")
                {
                    return lower;
                }
            }

            return alert.Status switch
            {
                1 => "info",
                2 => "warning",
                3 => "critical",
                _ => null
            };
        }

        /// <summary>
        /// Applies severity background color to a cell: red for critical, dark yellow for warning, dark blue for info.
        /// </summary>
        private static void ApplySeverityColor(IXLCell cell, string severity)
        {
            var color = severity switch
            {
                "critical" => XLColor.FromArgb(255, 0, 0),
                "warning"  => XLColor.FromArgb(191, 144, 0),
                "info"     => XLColor.FromArgb(31, 73, 125),
                _          => XLColor.NoColor
            };

            if(color == XLColor.NoColor)
            {
                return;
            }

            cell.Style.Font.FontColor = color;
            cell.Style.Font.Bold = true;
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
        /// Automatically deduplicates rows based on unique key properties (Id, AlertId, etc.).
        /// </remarks>
        private static void WriteTableSheet<T>(XLWorkbook workbook, SheetRegistry sheetRegistry, string logicalName, IReadOnlyCollection<T> rows)
        {
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName(logicalName));
            WriteBackToIndex(ws);

            // Deduplicate rows based on available unique key properties
            var uniqueRows = DeduplicateRows(rows, logicalName);
            WriteRows(ws, 3, uniqueRows);
            ws.SheetView.FreezeColumns(1);
        }

        /// <summary>
        /// Deduplicates a collection of rows based on available unique key properties.
        /// </summary>
        /// <typeparam name="T">The type of objects in the collection.</typeparam>
        /// <param name="rows">Collection of objects to deduplicate.</param>
        /// <param name="contextName">Context name for logging (e.g., sheet name).</param>
        /// <returns>Deduplicated collection of rows.</returns>
        private static IReadOnlyCollection<T> DeduplicateRows<T>(IReadOnlyCollection<T> rows, string contextName)
        {
            if(rows.Count == 0)
            {
                return rows;
            }

            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Find the first available unique key property
            var keyProperty = properties.FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals($"{type.Name}Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("AlertId", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("MetricId", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("ComponentId", StringComparison.OrdinalIgnoreCase));

            if(keyProperty == null)
            {
                // No unique key found, return all rows with a warning
                Log.Debug("No unique key property found for type '{TypeName}' in '{ContextName}'. Skipping deduplication.",
                    type.Name, contextName);
                return rows;
            }

            // Deduplicate by the key property
            var uniqueRows = rows
                .GroupBy(r => keyProperty.GetValue(r))
                .Select(g => g.First())
                .ToList();

            if(uniqueRows.Count != rows.Count)
            {
                Log.Warning("Removed {Count} duplicate row(s) in '{ContextName}' based on property '{KeyProperty}'. Original: {Original}, Unique: {Unique}",
                    rows.Count - uniqueRows.Count, contextName, keyProperty.Name, rows.Count, uniqueRows.Count);
            }

            return uniqueRows;
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

            if(properties.Length == 0)
            {
                ws.Cell(startRow, 1).Value = "No columns.";
                return;
            }

            var headerRow = startRow;
            for(var col = 0; col < properties.Length; col++)
            {
                ws.Cell(headerRow, col + 1).Value = properties[col].Name;
                ws.Cell(headerRow, col + 1).Style.Font.Bold = true;
            }

            var row = headerRow + 1;
            foreach(var item in rows)
            {
                for(var col = 0; col < properties.Length; col++)
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
        /// Applies alert count color to a cell: red for critical, dark yellow for warning, dark blue for info.
        /// Only applies color when the count is non-null, non-empty and greater than zero.
        /// </summary>
        private static void ApplyAlertColor(IXLCell cell, int count, AlertSeverity severity)
        {
            if(count <= 0)
            {
                return;
            }

            var color = severity switch
            {
                AlertSeverity.Critical => XLColor.FromArgb(255, 0, 0),       // Red
                AlertSeverity.Warning  => XLColor.FromArgb(191, 144, 0),     // Burnt yellow / dark gold
                AlertSeverity.Info     => XLColor.FromArgb(31, 73, 125),     // Dark navy blue
                _                     => XLColor.NoColor
            };

            cell.Style.Font.FontColor = color;
            cell.Style.Font.Bold = true;
        }

        private enum AlertSeverity { Info, Warning, Critical }

        /// <summary>
        /// Sets the value of an Excel cell with appropriate formatting based on data type.
        /// </summary>
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
            switch(value)
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
        /// Logs information about duplicate columns found in workbook sheets.
        /// </summary>
        /// <param name="workbook">The Excel workbook to inspect for duplicate columns.</param>
        /// <remarks>
        /// Iterates through all worksheets and attempts to identify duplicate column names in tables.
        /// Useful for debugging table creation errors related to duplicate field names.
        /// </remarks>
        private static void LogDuplicateColumns(XLWorkbook workbook)
        {
            try
            {
                foreach(var worksheet in workbook.Worksheets)
                {
                    try
                    {
                        Log.Debug("Checking worksheet '{SheetName}' for duplicate columns...", worksheet.Name);

                        CheckTablesForDuplicates(worksheet);
                        CheckHeaderRowForDuplicates(worksheet);
                    }
                    catch(Exception ex)
                    {
                        Log.Warning(ex, "Failed to check worksheet '{SheetName}' for duplicate columns", worksheet.Name);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Warning(ex, "Failed to log duplicate columns information");
            }
        }

        /// <summary>
        /// Checks all tables in a worksheet for duplicate column names.
        /// </summary>
        /// <param name="worksheet">The worksheet to check.</param>
        private static void CheckTablesForDuplicates(IXLWorksheet worksheet)
        {
            foreach(var table in worksheet.Tables)
            {
                var columnNames = table.Fields.Select(f => f.Name).ToList();
                var duplicates = FindDuplicateNames(columnNames);

                if(duplicates.Count > 0)
                {
                    Log.Error("Found {Count} duplicate column(s) in table '{TableName}' on sheet '{SheetName}': {Columns}",
                        duplicates.Count, table.Name, worksheet.Name,
                        string.Join(", ", duplicates.Select(d => $"{d.Name} (x{d.Count})")));
                }
                else
                {
                    Log.Debug("No duplicate columns found in table '{TableName}' on sheet '{SheetName}'",
                        table.Name, worksheet.Name);
                }
            }
        }

        /// <summary>
        /// Checks the first row of a worksheet for duplicate header names if no tables exist.
        /// </summary>
        /// <param name="worksheet">The worksheet to check.</param>
        private static void CheckHeaderRowForDuplicates(IXLWorksheet worksheet)
        {
            if(worksheet.Tables.Any())
            {
                return;
            }

            var headers = ExtractHeadersFromFirstRow(worksheet);

            if(headers.Count == 0)
            {
                return;
            }

            var duplicates = FindDuplicateNames(headers);

            if(duplicates.Count > 0)
            {
                Log.Error("Found {Count} duplicate column(s) in header row of sheet '{SheetName}': {Columns}",
                    duplicates.Count, worksheet.Name,
                    string.Join(", ", duplicates.Select(d => $"{d.Name} (x{d.Count})")));
            }
        }

        /// <summary>
        /// Extracts header names from the first row of a worksheet.
        /// </summary>
        /// <param name="worksheet">The worksheet to extract headers from.</param>
        /// <returns>List of header names from the first row.</returns>
        private static List<string> ExtractHeadersFromFirstRow(IXLWorksheet worksheet)
        {
            var firstRow = worksheet.Row(1);
            var headers = new List<string>();
            var col = 1;
            const int maxColumns = 100; // Safety limit

            while(!firstRow.Cell(col).IsEmpty() && col <= maxColumns)
            {
                headers.Add(firstRow.Cell(col).GetString());
                col++;
            }

            return headers;
        }

        /// <summary>
        /// Finds duplicate names in a collection of strings.
        /// </summary>
        /// <param name="names">Collection of names to check for duplicates.</param>
        /// <returns>List of duplicate names with their counts.</returns>
        private static List<(string Name, int Count)> FindDuplicateNames(List<string> names) => [.. names
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => (Name: g.Key, Count: g.Count()))];

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
        /// <remarks>
        /// Initializes a new instance of the sheet registry.
        /// </remarks>
        /// <param name="workbook">The Excel workbook to verify existing names.</param>
        private sealed class SheetRegistry(XLWorkbook workbook)
        {
            private readonly XLWorkbook _workbook = workbook;
            private readonly Dictionary<string, string> _logicalToPhysical = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _usedPhysicalNames = new(StringComparer.OrdinalIgnoreCase);

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
                if(_logicalToPhysical.TryGetValue(logicalName, out var existing))
                {
                    return existing;
                }

                var baseName = SanitizeSheetName(logicalName);
                var candidate = baseName;
                var index = 1;
                while(_usedPhysicalNames.Contains(candidate) || _workbook.Worksheets.Any(w => w.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
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
            /// <returns>Sanitized logical name for the sheet with underscores replacing spaces (not truncated).</returns>
            /// <remarks>
            /// <para>
            /// Legacy method kept for compatibility. No longer used in current structure.
            /// </para>
            /// <para>
            /// Spaces in component and metric names are replaced with underscores.
            /// </para>
            /// </remarks>
            public static string BuildComponentMetricSheetName(string componentName, string metricName)
            {
                // Don't truncate here - let GetOrCreatePhysicalName handle truncation AND deduplication
                var sanitizedComponent = SanitizeSheetNameStatic(componentName);
                var sanitizedMetric = SanitizeSheetNameStatic(metricName);
                return $"{sanitizedComponent}_{sanitizedMetric}";
            }

            /// <summary>
            /// Builds a sheet name for components grouped by type.
            /// Format: {ComponentType} (without prefix)
            /// </summary>
            /// <param name="componentType">Component type (host, service, database, etc.).</param>
            /// <returns>Sanitized logical name for the component type sheet.</returns>
            /// <remarks>
            /// <para>
            /// Examples:
            /// <list type="bullet">
            /// <item><description>"host" → "host"</description></item>
            /// <item><description>"service" → "service"</description></item>
            /// <item><description>"database" → "database"</description></item>
            /// <item><description>"my type" → "my_type"</description></item>
            /// </list>
            /// </para>
            /// <para>
            /// Spaces in component type are replaced with underscores.
            /// </para>
            /// </remarks>
            public static string BuildComponentTypeSheetName(string componentType)
            {
                var sanitizedType = SanitizeSheetNameStatic(componentType);
                return sanitizedType;
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
            /// <item><description>host + "Process CPU Used" → "hostProcessCPUUsed"</description></item>
            /// <item><description>service + "Request Rate" → "serviceRequestRate"</description></item>
            /// </list>
            /// </para>
            /// <para>
            /// Spaces are removed completely. Invalid characters are replaced with ''.
            /// Truncation to 31 characters is done later in <see cref="GetOrCreatePhysicalName"/>.
            /// </para>
            /// </remarks>
            public static string BuildMetricByTypeSheetName(string componentType, string metricName)
            {
                // Remove all spaces from component type and metric name
                var sanitizedType = componentType.Replace(" ", "");
                var sanitizedMetric = metricName.Replace(" ", "");
                return $"{sanitizedType}{sanitizedMetric}";
            }

            /// <summary>
            /// Wrapper for <see cref="SanitizeSheetNameStatic"/> for instance use.
            /// </summary>
            /// <param name="value">String to sanitize.</param>
            /// <returns>Sanitized and truncated string to 31 characters.</returns>
            private static string SanitizeSheetName(string value) => SanitizeSheetNameStatic(value);

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
            /// <item><description>Replaces spaces with underscores ('_')</description></item>
            /// <item><description>Removes leading and trailing whitespace before processing</description></item>
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
                var chars = value.Trim().Select(ch => invalid.Contains(ch) || ch == ' ' ? '_' : ch).ToArray();
                var sanitized = new string(chars);

                if(string.IsNullOrWhiteSpace(sanitized))
                {
                    sanitized = "Sheet";
                }

                return sanitized.Length <= 31 ? sanitized : sanitized[..31];
            }
        }
    }
}
