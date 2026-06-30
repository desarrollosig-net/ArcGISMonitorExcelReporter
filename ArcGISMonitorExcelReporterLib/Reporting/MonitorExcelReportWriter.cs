using ArcGISMonitorExcelReporterLib.Configuration;
using ArcGISMonitorExcelReporterLib.Models;

using ClosedXML.Excel;

using System.Drawing;
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
#pragma warning disable S1192
        private const string UnknownValue = "Unknown";
#pragma warning restore S1192

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
                WriteInputs(workbook, sheetRegistry, report, "Inputs");

                Log.Debug("Writing Summary sheet...");
                WriteSummary(workbook, sheetRegistry, report, "Summary", "Agents", "Labels");

                Log.Debug("Writing Components sheets by type...");
                WriteComponentsWithMetricsSheets(workbook, sheetRegistry, report);

                Log.Debug("Writing Agents, Labels & Metrics sheet...");
                WriteAgentsLabelsMetricsSheet(workbook, sheetRegistry, report);

                Log.Debug("Writing Agents sheet ({Count} rows)...", report.Agents.Count);
                WriteAgentsSheet(workbook, sheetRegistry, report.Agents, report);

                Log.Debug("Writing Alerts sheet ({Count} rows)...", report.Alerts.Count);
                WriteAlertsSheet(workbook, sheetRegistry, report.Alerts, report);

                Log.Debug("Writing Labels sheet ({Count} rows)...", report.Labels.Count);
                WriteLabelsSheet(workbook, sheetRegistry, report.Labels, report);

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
        private static void WriteInputs(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report, string sheetName)
        {
            var ws = workbook.Worksheets.Add(sheetName);
            sheetRegistry.Register(sheetName, sheetName);

            ws.Cell(1, 1).Value = "Report Parameters";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;

            var tz = TimeZoneInfoResolver.Resolve(report.Timezone);

            var metadata = new (string Label, object? Value)[]
            {
                ("Server URL",              report.ServerUrl),
                ("ArcGIS Monitor Version",  report.MonitoringInfo?.Version),
                ("Collection",              !string.IsNullOrWhiteSpace(report.CollectionName) && report.CollectionName.Trim() != "*"
                                                ? report.CollectionName
                                                : null),
                ("Generated",               FormatLocalDate(report.GeneratedAtUtc, tz)),
                ("From",                    FormatLocalDate(report.FromUtc, tz)),
                ("To",                      FormatLocalDate(report.ToUtc, tz)),
                ("Past Days",               (object)report.PastDays),
                ("Past Hours",              (object)report.PastHours),
                ("Timezone",                report.Timezone),
                ("Execution Time",          report.ExecutionTime.ToString("hh\\:mm\\:ss")),
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
        private static void WriteSummary(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report, string sheetName, string agentsSheetName, string labelsSheetName)
        {
            var ws = workbook.Worksheets.Add(sheetName);
            sheetRegistry.Register(sheetName, sheetName);

            ws.Cell(1, 1).Value = "ArcGIS Monitor - Summary Report";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;

            // Group components by type and category
            var summaryData = report.Components
                .GroupBy(c => new { c.Type, c.Subtype })
                .Select(g =>
                {
                    var componentIds = g.Select(c => c.ComponentId).ToList();
                    var componentAlerts = report.Alerts.Where(a => componentIds.Contains(a.ComponentId ?? 0)).ToList();

                    return new
                    {
                        ComponentType = g.Key.Type ?? UnknownValue,
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
                .GroupBy(c => c.Type ?? UnknownValue)
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
                    .Select(m => NormalizeMetricName(m.MetricName ?? UnknownValue))
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
                    WriteIndexLink(ws, row++, timeSeriesSheet, $"  • {metricName} - Time Series", $"Max every {FormatBucketLabel(report.MetricDataBucket)} with chart");
                }

                row++; // Blank line between component types
            }

            row += 1;
            WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Alerts"), "Alerts", "All alerts across components");

            row += 1;
            WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName(agentsSheetName), "Agents", "All agents configured in ArcGIS Monitor");

            row += 1;
            WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName(labelsSheetName), "Labels", "All labels available in ArcGIS Monitor");

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
                    Log.Debug("Normalized metric for consolidation: '{Original}' -> '{Normalized}'", metricName, normalized);
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

                    // Get available fields for this component type from metadata
                    var availableFieldsForType = GetComponentTypeFields(report.ComponentTypes, componentType);

                    // Write components table with type-specific fields
                    WriteComponentsSummaryTable(ws, 4, componentsList, report, availableFieldsForType);

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
        /// Gets available field names for a specific component type from the component types metadata.
        /// </summary>
        private static List<string>? GetComponentTypeFields(ComponentTypesInfo? componentTypes, string componentType)
        {
            if(componentTypes?.Types == null || componentTypes.Types.Count == 0)
            {
                return null;
            }

            var typeDefinition = componentTypes.Types.FirstOrDefault(t =>
                t.Name?.Equals(componentType, StringComparison.OrdinalIgnoreCase) ?? false);

            if(typeDefinition?.Names != null && typeDefinition.Names.Count > 0)
            {
                Log.Debug("Found {Count} fields for component type '{ComponentType}': {Fields}",
                    typeDefinition.Names.Count, componentType, string.Join(", ", typeDefinition.Names));
                return typeDefinition.Names;
            }

            return null;
        }

        /// <summary>
        /// Converts snake_case API field names to PascalCase C# property names.
        /// Examples: "address_internal" -> "AddressInternal", "cpu_cores_physical" -> "CpuCoresPhysical"
        /// </summary>
        private static string ConvertSnakeCaseToPascalCase(string snakeCaseField)
        {
            if(string.IsNullOrWhiteSpace(snakeCaseField))
            {
                return snakeCaseField;
            }

            var parts = snakeCaseField.Split('_');
            var pascalCase = string.Concat(parts.Select(part =>
                char.ToUpperInvariant(part[0]) + (part.Length > 1 ? part.Substring(1) : "")));

            return pascalCase;
        }

        /// <summary>
        /// Maps API field names (snake_case) to ComponentReportRow property names.
        /// Returns a HashSet of property names that match the API field names.
        /// </summary>
        private static HashSet<string> MapApiFieldsToProperties(List<string>? apiFields)
        {
            var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if(apiFields == null || apiFields.Count == 0)
            {
                return propertyNames;
            }

            foreach(var apiField in apiFields)
            {
                // Convert snake_case to PascalCase
                var propertyName = ConvertSnakeCaseToPascalCase(apiField);
                propertyNames.Add(propertyName);
            }

            return propertyNames;
        }

        /// <summary>
        /// Builds a dictionary mapping property names to their display aliases from ResourceFields.
        /// Falls back to property name if alias is not available.
        /// </summary>
        private static Dictionary<string, string> BuildPropertyAliasMapping(
            Dictionary<string, ResourceFieldInfo>? resourceFields)
        {
            var aliasMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if(resourceFields == null)
            {
                Log.Debug("No resource fields available. Using property names as headers.");
                return aliasMapping;
            }

            // Try to map from components, alerts, or other resource types
            var resourcesToTry = new[] { "components", "alerts", "metrics", "agents" };
            var allFields = new List<FieldDefinition>();

            foreach(var resourceName in resourcesToTry)
            {
                if(!resourceFields.ContainsKey(resourceName))
                {
                    continue;
                }

                var resourceInfo = resourceFields[resourceName];
                if(resourceInfo?.Fields == null || resourceInfo.Fields.Count == 0)
                {
                    continue;
                }

                Log.Debug("Found {Count} field definitions for resource '{Resource}'", 
                    resourceInfo.Fields.Count, resourceName);
                allFields.AddRange(resourceInfo.Fields);
            }

            if(allFields.Count == 0)
            {
                Log.Debug("No field definitions available. Using property names as headers.");
                return aliasMapping;
            }

            foreach(var field in allFields)
            {
                if(string.IsNullOrWhiteSpace(field.Name))
                {
                    continue;
                }

                // Convert API field name (snake_case) to property name (PascalCase)
                var propertyName = ConvertSnakeCaseToPascalCase(field.Name);

                // Skip if already mapped (first occurrence wins)
                if(aliasMapping.ContainsKey(propertyName))
                {
                    continue;
                }

                // Use alias if available, otherwise use the field name
                var displayName = !string.IsNullOrWhiteSpace(field.Alias) ? field.Alias : field.Name;

                aliasMapping[propertyName] = displayName;

                Log.Debug("Mapped property '{PropertyName}' to alias '{DisplayName}'", propertyName, displayName);
            }

            Log.Information("Built alias mapping for {Count} total fields from multiple resources", aliasMapping.Count);
            return aliasMapping;
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
                Log.Debug("Removed {Count} duplicate metric(s) in metric component data table. Original: {Original}, Unique: {Unique}",
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
                ws.Cell(currentRow, 2).Value = metric.ComponentName ?? UnknownValue;
                ws.Cell(currentRow, 3).Value = metric.MetricName ?? UnknownValue;
                ws.Cell(currentRow, 4).Value = metric.MetricId;
                ws.Cell(currentRow, 5).Value = metric.ComponentType ?? UnknownValue;
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
        /// Selects the top <paramref name="limit"/> metrics ordered by P95 descending,
        /// falling back to maximum value descending for metrics without P95 data.
        /// </summary>
        /// <param name="metrics">Full list of metric rows for the current sheet (may contain duplicates by MetricId).</param>
        /// <param name="metricData">Aggregated period statistics used to rank metrics.</param>
        /// <param name="limit">Maximum number of unique metric IDs to keep.</param>
        /// <returns>
        /// A tuple with the filtered metric list and the original number of unique metric IDs
        /// (before filtering).
        /// </returns>
        private static (List<MetricReportRow> Metrics, int TotalUniqueCount) SelectTopMetricsByP95(
            List<MetricReportRow> metrics,
            IReadOnlyCollection<MetricDataReportRow> metricData,
            int limit)
        {
            var uniqueIds = metrics.Select(m => m.MetricId).Distinct().ToList();
            if(uniqueIds.Count <= limit)
            {
                return (metrics, uniqueIds.Count);
            }

            // Build lookup: metricId -> (P95, Max) from aggregated period statistics
            var statsById = metricData
                .Where(md => md.MetricId > 0)
                .GroupBy(md => md.MetricId)
                .ToDictionary(
                    g => g.Key,
                    g => (P95: g.Max(d => d.Percentile95Value), Max: g.Max(d => d.MaxValue)));

            // Metrics with P95: ordered by P95 descending
            var withP95 = uniqueIds
                .Where(id => statsById.TryGetValue(id, out var s) && s.P95.HasValue)
                .OrderByDescending(id => statsById[id].P95!.Value)
                .ToList();

            // Metrics without P95: ordered by Max descending
            var withP95Set = withP95.ToHashSet();
            var withoutP95 = uniqueIds
                .Where(id => !withP95Set.Contains(id))
                .OrderByDescending(id => statsById.TryGetValue(id, out var s) ? s.Max ?? 0.0 : 0.0)
                .ToList();

            var selectedIds = withP95.Concat(withoutP95).Take(limit).ToHashSet();
            return ([.. metrics.Where(m => selectedIds.Contains(m.MetricId))], uniqueIds.Count);
        }

        /// <summary>
        /// Writes a metric time series sheet with max values grouped by the selected observed_at bucket interval.
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

                // Apply top-N selection by P95 (fallback to Max) before writing anything
                var maxColumns = report.MaxTimeSeriesColumns > 0 ? report.MaxTimeSeriesColumns : 20;
                var (selectedMetrics, totalUniqueCount) = SelectTopMetricsByP95(metrics, report.MetricData, maxColumns);
                metrics = selectedMetrics;
                var wasLimited = totalUniqueCount > maxColumns;

                // Title
                var bucketLabel = FormatBucketLabel(report.MetricDataBucket);
                var title = wasLimited
                    ? $"Time Series: {metricName} ({componentType}) - Top {maxColumns} of {totalUniqueCount} by P95 - Max every {bucketLabel}"
                    : $"Time Series: {metricName} ({componentType}) - Max every {bucketLabel}";
                ws.Cell(row, 1).Value = title;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 16;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
                row += 2;

                if(wasLimited)
                {
                    Log.Warning(
                        "Time series for '{MetricName}' in '{ComponentType}': limiting to top {Max} of {Total} metrics by P95/Max.",
                        metricName, componentType, maxColumns, totalUniqueCount);
                }

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
                    Log.Debug("No time series data found for metric '{MetricName}' in '{ComponentType}'", metricName, componentType);
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
                    Log.Debug("Removed {Count} duplicate time series data point(s) for metric '{MetricName}' in '{ComponentType}'. Original: {Original}, Unique: {Unique}",
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
                    Log.Debug("Removed {Count} duplicate metric(s) in time series for '{MetricName}' in '{ComponentType}'. Original: {Original}, Unique: {Unique}",
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
                        baseName = $"{component.ComponentName ?? $"Component_{component.ComponentId}"} - {component.MetricName ?? UnknownValue}";
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
                        Log.Debug("Duplicate component name '{ComponentName}' in time series for metric '{MetricName}', renamed to '{UniqueName}' in sheet '{SheetName}'",
                            baseName, metricName, uniqueName, ws.Name);
                    }
                    else
                    {
                        columnNames[baseName] = 0;
                        finalComponentColumns.Add((component, baseName));
                    }
                }

                // Detect if this is a consolidated metric sheet (multiple unique metric names)
                var uniqueMetricNames = components
                    .Select(m => m.MetricName ?? UnknownValue)
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
                    ws.Cell(currentRow, 1).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";

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
        /// Writes the components summary table with only non-empty columns and type-specific fields when available.
        /// </summary>
        private static int WriteComponentsSummaryTable(
            IXLWorksheet ws, 
            int startRow, 
            List<ComponentReportRow> components, 
            MonitorExcelReport report,
            List<string>? typeSpecificFields = null)
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
                    Log.Debug("Removed {Count} duplicate component(s) in sheet '{SheetName}'. Original: {Original}, Unique: {Unique}",
                        components.Count - uniqueComponents.Count, ws.Name, components.Count, uniqueComponents.Count);
                }

                // Get all properties (only declared in this class, not inherited)
                var properties = typeof(ComponentReportRow).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                Log.Debug("Found {Count} properties in ComponentReportRow: {Properties}",
                    properties.Length, string.Join(", ", properties.Select(p => p.Name)));

                // Filter properties to include only type-specific fields if provided
                var propertiesToUse = properties;
                if(typeSpecificFields != null && typeSpecificFields.Count > 0)
                {
                    // Map API field names (snake_case) to C# property names (PascalCase)
                    var apiFieldsAsProperties = MapApiFieldsToProperties(typeSpecificFields);
                    var matchedProperties = properties.Where(p =>
                        apiFieldsAsProperties.Contains(p.Name)).ToArray();

                    if(matchedProperties.Length > 0)
                    {
                        Log.Information("Component type '{Type}' restricts fields to {Count} type-specific properties: {Properties}",
                            uniqueComponents.FirstOrDefault()?.Type ?? "Unknown",
                            matchedProperties.Length,
                            string.Join(", ", matchedProperties.Select(p => p.Name)));

                        Log.Debug("API field names for component type: {Fields}",
                            string.Join(", ", typeSpecificFields));

                        propertiesToUse = matchedProperties;
                    }
                    else
                    {
                        Log.Debug("No properties matched component type fields. Using all non-empty properties.");
                    }
                }

                // Filter properties that have at least one non-null, non-empty value
                // Exclude CollectionName and SystemId from component summary sheets
                var nonEmptyProperties = propertiesToUse
                    .Where(p => uniqueComponents.Any(c =>
                    {
                        // Skip excluded columns
                        if(p.Name is "CollectionName" or "SystemId")
                        {
                            return false;
                        }

                        var value = p.GetValue(c);
                        return value != null &&
                               !(value is string str && string.IsNullOrWhiteSpace(str)) &&
                               !(value is int intVal && intVal == 0 && p.Name.EndsWith("Count"));
                    }))
                    .ToList();

                Log.Debug("Filtered to {Count} non-empty properties: {Properties}",
                    nonEmptyProperties.Count, string.Join(", ", nonEmptyProperties.Select(p => p.Name)));

                // Build alias mapping from ResourceFields
                var aliasMapping = BuildPropertyAliasMapping(report.ResourceFields);

                // Create unique column names (in case of duplicates)
                var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var finalColumnNames = new List<(PropertyInfo Property, string ColumnName)>();

                foreach(var prop in nonEmptyProperties)
                {
                    var baseName = prop.Name;

                    // Get alias from mapping, or use property name as fallback
                    var displayName = aliasMapping.TryGetValue(baseName, out var alias) ? alias : baseName;

                    if(columnNames.TryGetValue(displayName, out var value))
                    {
                        columnNames[displayName] = ++value;
                        var uniqueName = $"{displayName}_{value}";
                        finalColumnNames.Add((prop, uniqueName));
                        Log.Debug("Duplicate column name detected: '{ColumnName}', renamed to '{UniqueName}' in sheet '{SheetName}'",
                            displayName, uniqueName, ws.Name);
                    }
                    else
                    {
                        columnNames[displayName] = 0;
                        finalColumnNames.Add((prop, displayName));
                    }
                }

                if(finalColumnNames.Count == 0)
                {
                    ws.Cell(startRow, 1).Value = "No displayable columns found";
                    Log.Debug("No displayable columns found for components in sheet '{SheetName}'", ws.Name);
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
                            Log.Debug(ex, "Failed to get value for property '{Property}' on component '{Component}'",
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
        /// Writes the "Agents, Labels & Metrics" sheet with consolidated information about agents, labels, and metrics.
        /// </summary>
        private static void WriteAgentsLabelsMetricsSheet(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
        {
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName("Agents & Labels"));
            sheetRegistry.Register("Agents & Labels", ws.Name);
            WriteBackToIndex(ws);

            var currentRow = 3;

            // ==================== AGENTS SECTION ====================
            ws.Cell(currentRow, 1).Value = "Agents";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Font.FontSize = 14;
            currentRow += 2;

            if(report.Agents.Count == 0)
            {
                ws.Cell(currentRow, 1).Value = "No agents found.";
                currentRow += 2;
            }
            else
            {
                var agentHeaderRow = currentRow;
                string[] agentHeaders =
                [
                    "Id", "Created At", "Name", "Description", "Version",
                    "Address", "Platform", "Connected", "Through Connection Id"
                ];

                for(var col = 0; col < agentHeaders.Length; col++)
                {
                    var cell = ws.Cell(agentHeaderRow, col + 1);
                    cell.Value = agentHeaders[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                currentRow = agentHeaderRow + 1;
                foreach(var agent in report.Agents)
                {
                    ws.Cell(currentRow, 1).Value = agent.Id;
                    SetCellValue(ws.Cell(currentRow, 2), agent.CreatedAt);
                    ws.Cell(currentRow, 3).Value = agent.Name ?? string.Empty;
                    ws.Cell(currentRow, 4).Value = agent.Description ?? string.Empty;
                    ws.Cell(currentRow, 5).Value = agent.Version ?? string.Empty;
                    ws.Cell(currentRow, 6).Value = agent.Address ?? string.Empty;
                    ws.Cell(currentRow, 7).Value = agent.Platform ?? string.Empty;
                    ws.Cell(currentRow, 8).Value = agent.IsConnected?.ToString() ?? string.Empty;
                    SetCellValue(ws.Cell(currentRow, 9), agent.ThroughConnectionId);
                    currentRow++;
                }

                var agentRange = ws.Range(agentHeaderRow, 1, Math.Max(currentRow - 1, agentHeaderRow), agentHeaders.Length);
                agentRange.CreateTable();
                currentRow += 2;
            }

            // ==================== LABELS SECTION ====================
            ws.Cell(currentRow, 1).Value = "Labels";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Font.FontSize = 14;
            currentRow += 2;

            if(report.Labels.Count == 0)
            {
                ws.Cell(currentRow, 1).Value = "No labels found.";
                currentRow += 2;
            }
            else
            {
                var labelHeaderRow = currentRow;
                string[] labelHeaders = ["Id", "Created At", "Name", "Description", "Color"];

                for(var col = 0; col < labelHeaders.Length; col++)
                {
                    var cell = ws.Cell(labelHeaderRow, col + 1);
                    cell.Value = labelHeaders[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                currentRow = labelHeaderRow + 1;
                foreach(var label in report.Labels)
                {
                    ws.Cell(currentRow, 1).Value = label.Id;
                    SetCellValue(ws.Cell(currentRow, 2), label.CreatedAt);
                    ws.Cell(currentRow, 3).Value = label.Name ?? string.Empty;
                    ws.Cell(currentRow, 4).Value = label.Description ?? string.Empty;
                    ws.Cell(currentRow, 5).Value = label.Color ?? string.Empty;

                    // Apply color to the color cell if available
                    if(!string.IsNullOrEmpty(label.Color))
                    {
                        try
                        {
                            var colorCell = ws.Cell(currentRow, 5);
                            // Try to parse hex color
                            if(label.Color.StartsWith('#') && label.Color.Length == 7)
                            {
                                colorCell.Style.Fill.BackgroundColor = XLColor.FromHtml(label.Color);
                                colorCell.Style.Font.FontColor = XLColor.White;
                            }
                        }
                        catch
                        {
                            // If color parsing fails, just display the color code as text
                        }
                    }

                    currentRow++;
                }

                var labelRange = ws.Range(labelHeaderRow, 1, Math.Max(currentRow - 1, labelHeaderRow), labelHeaders.Length);
                labelRange.CreateTable();
                currentRow += 2;
            }

            // ==================== METRICS SECTION ====================
            ws.Cell(currentRow, 1).Value = "Metrics";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Font.FontSize = 14;
            currentRow += 2;

            if(report.Metrics.Count == 0)
            {
                ws.Cell(currentRow, 1).Value = "No metrics found.";
            }
            else
            {
                var metricHeaderRow = currentRow;
                string[] metricHeaders =
                [
                    "Collection", "Component Id", "Component Name", "Component Type", "Component Subtype",
                    "Metric Id", "Metric Name", "RId", "Unit", "Status", "Alerting Enabled", "Aggregation"
                ];

                for(var col = 0; col < metricHeaders.Length; col++)
                {
                    var cell = ws.Cell(metricHeaderRow, col + 1);
                    cell.Value = metricHeaders[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                currentRow = metricHeaderRow + 1;
                foreach(var metric in report.Metrics)
                {
                    ws.Cell(currentRow, 1).Value = metric.CollectionName;
                    SetCellValue(ws.Cell(currentRow, 2), metric.ComponentId);
                    ws.Cell(currentRow, 3).Value = metric.ComponentName ?? string.Empty;
                    ws.Cell(currentRow, 4).Value = metric.ComponentType ?? string.Empty;
                    ws.Cell(currentRow, 5).Value = metric.ComponentSubtype ?? string.Empty;
                    SetCellValue(ws.Cell(currentRow, 6), metric.MetricId);
                    ws.Cell(currentRow, 7).Value = metric.MetricName ?? string.Empty;
                    ws.Cell(currentRow, 8).Value = metric.RId ?? string.Empty;
                    ws.Cell(currentRow, 9).Value = metric.Unit ?? string.Empty;
                    SetCellValue(ws.Cell(currentRow, 10), metric.Status);
                    ws.Cell(currentRow, 11).Value = metric.IsAlertingEnabled?.ToString() ?? string.Empty;
                    ws.Cell(currentRow, 12).Value = metric.Aggregation ?? string.Empty;
                    currentRow++;
                }

                var metricRange = ws.Range(metricHeaderRow, 1, Math.Max(currentRow - 1, metricHeaderRow), metricHeaders.Length);
                metricRange.CreateTable();
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(3);
        }

        /// <summary>
        /// Writes the Agents sheet with all agent attributes extracted from API metadata.
        /// Uses dynamic reflection and aliases to display all available agent fields.
        /// </summary>
        private static void WriteAgentsSheet(
            XLWorkbook workbook,
            SheetRegistry sheetRegistry,
            List<AgentReportRow> agents,
            MonitorExcelReport report)
        {
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName("Agents"));
            WriteBackToIndex(ws);

            if(agents.Count == 0)
            {
                ws.Cell(3, 1).Value = "No agents found.";
                return;
            }

            const int headerRow = 3;

            // Get all properties from AgentReportRow
            var properties = typeof(AgentReportRow).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Log.Debug("Found {Count} properties in AgentReportRow: {Properties}",
                properties.Length, string.Join(", ", properties.Select(p => p.Name)));

            // Filter to include only properties with at least one non-null, non-empty value
            var nonEmptyProperties = properties
                .Where(p => agents.Any(a =>
                {
                    var value = p.GetValue(a);
                    return value != null &&
                           !(value is string str && string.IsNullOrWhiteSpace(str)) &&
                           !(value is int intVal && intVal == 0 && p.Name.EndsWith("Count")) &&
                           !(value is bool boolVal && !boolVal && p.Name.StartsWith("Is"));
                }))
                .ToList();

            Log.Debug("Filtered to {Count} non-empty properties: {Properties}",
                nonEmptyProperties.Count, string.Join(", ", nonEmptyProperties.Select(p => p.Name)));

            // Build alias mapping from ResourceFields
            var aliasMapping = BuildPropertyAliasMapping(report.ResourceFields);

            // Create display names for headers
            var finalHeaders = new List<(PropertyInfo Property, string DisplayName)>();
            foreach(var prop in nonEmptyProperties)
            {
                var displayName = aliasMapping.TryGetValue(prop.Name, out var alias) ? alias : prop.Name;
                finalHeaders.Add((prop, displayName));
            }

            // Write headers
            for(var col = 0; col < finalHeaders.Count; col++)
            {
                var cell = ws.Cell(headerRow, col + 1);
                cell.Value = finalHeaders[col].DisplayName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Write data rows
            var row = headerRow + 1;
            foreach(var agent in agents)
            {
                var col = 1;

                foreach(var (property, _) in finalHeaders)
                {
                    var cell = ws.Cell(row, col);
                    var value = property.GetValue(agent);
                    SetCellValue(cell, value);
                    col++;
                }

                row++;
            }

            var usedRange = ws.Range(headerRow, 1, Math.Max(row - 1, headerRow), finalHeaders.Count);
            usedRange.CreateTable();
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(headerRow);
            ws.SheetView.FreezeColumns(1);
        }

        /// <summary>
        /// Writes the Alerts sheet with severity labels and color coding.
        /// Replaces the numeric Status code (1=info, 2=warning, 3=critical) with a labeled, colored cell.
        /// </summary>
        private static void WriteAlertsSheet(
            XLWorkbook workbook, 
            SheetRegistry sheetRegistry, 
            List<AlertReportRow> alerts,
            MonitorExcelReport report)
        {
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName("Alerts"));
            WriteBackToIndex(ws);

            if(alerts.Count == 0)
            {
                ws.Cell(3, 1).Value = "No alerts.";
                return;
            }

            // Deduplicate by AlertId
            var uniqueAlerts = alerts
                .GroupBy(a => a.AlertId)
                .Select(g => g.First())
                .ToList();

            const int headerRow = 3;

            // Get all properties from AlertReportRow
            var properties = typeof(AlertReportRow).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Log.Debug("Found {Count} properties in AlertReportRow: {Properties}",
                properties.Length, string.Join(", ", properties.Select(p => p.Name)));

            // Filter to include only properties with at least one non-null, non-empty value
            // and exclude CollectionName
            var nonEmptyProperties = properties
                .Where(p => uniqueAlerts.Any(a =>
                {
                    // Skip excluded columns
                    if(p.Name == "CollectionName")
                    {
                        return false;
                    }

                    var value = p.GetValue(a);
                    return value != null &&
                           !(value is string str && string.IsNullOrWhiteSpace(str)) &&
                           !(value is int intVal && intVal == 0 && p.Name.EndsWith("Count"));
                }))
                .ToList();

            Log.Debug("Filtered to {Count} non-empty properties: {Properties}",
                nonEmptyProperties.Count, string.Join(", ", nonEmptyProperties.Select(p => p.Name)));

            // Build alias mapping from ResourceFields
            var aliasMapping = BuildPropertyAliasMapping(report.ResourceFields);

            // Create display names for headers
            var finalHeaders = new List<(PropertyInfo Property, string DisplayName)>();
            foreach(var prop in nonEmptyProperties)
            {
                var displayName = aliasMapping.TryGetValue(prop.Name, out var alias) ? alias : prop.Name;
                finalHeaders.Add((prop, displayName));
            }

            // Write headers
            for(var col = 0; col < finalHeaders.Count; col++)
            {
                var cell = ws.Cell(headerRow, col + 1);
                cell.Value = finalHeaders[col].DisplayName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Write data rows
            var row = headerRow + 1;
            foreach(var alert in uniqueAlerts)
            {
                var severity = ResolveSeverity(alert);
                var col = 1;

                foreach(var (property, _) in finalHeaders)
                {
                    var cell = ws.Cell(row, col);

                    // Special handling for Status -> Severity conversion
                    if(property.Name == "Status")
                    {
                        cell.Value = severity ?? string.Empty;
                        if(severity != null)
                        {
                            ApplySeverityColor(cell, severity);
                        }
                    }
                    else
                    {
                        var value = property.GetValue(alert);
                        SetCellValue(cell, value);
                    }

                    col++;
                }

                row++;
            }

            var usedRange = ws.Range(headerRow, 1, Math.Max(row - 1, headerRow), finalHeaders.Count);
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
                        Log.Debug(ex, "Failed to check worksheet '{SheetName}' for duplicate columns", worksheet.Name);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Debug(ex, "Failed to log duplicate columns information");
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
        /// Formats a bucket label for human-readable display in Excel titles and descriptions.
        /// </summary>
        /// <param name="bucket">The bucket interval (e.g., "15m", "hour", "day", "5m").</param>
        /// <returns>A formatted string for display (e.g., "15 minutes", "1 hour", "1 day", "5 minutes").</returns>
        /// <example>
        /// FormatBucketLabel("15m") returns "15 minutes"
        /// FormatBucketLabel("hour") returns "1 hour"
        /// FormatBucketLabel("day") returns "1 day"
        /// </example>
        private static string FormatBucketLabel(string bucket) => string.IsNullOrWhiteSpace(bucket)
                ? "15 minutes"
                : bucket switch
                {
                    "5m" => "5 minutes",
                    "15m" => "15 minutes",
                    "hour" => "1 hour",
                    "day" => "1 day",
                    _ => bucket
                };

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

        /// <summary>
        /// Writes the Labels sheet with all label data.
        /// </summary>
        private static void WriteLabelsSheet(
            XLWorkbook workbook,
            SheetRegistry sheetRegistry,
            List<LabelReportRow> labels,
            MonitorExcelReport report)
        {
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName("Labels"));
            WriteBackToIndex(ws);

            if(labels.Count == 0)
            {
                ws.Cell(3, 1).Value = "No labels found.";
                return;
            }

            const int headerRow = 3;

            // Get all properties from LabelReportRow
            var properties = typeof(LabelReportRow).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Log.Debug("Found {Count} properties in LabelReportRow: {Properties}",
                properties.Length, string.Join(", ", properties.Select(p => p.Name)));

            // Filter to include only properties with at least one non-null, non-empty value
            var nonEmptyProperties = properties
                .Where(p => labels.Any(l =>
                {
                    var value = p.GetValue(l);
                    return value != null &&
                           !(value is string str && string.IsNullOrWhiteSpace(str)) &&
                           !(value is int intVal && intVal == 0 && p.Name.EndsWith("Count"));
                }))
                .ToList();

            Log.Debug("Filtered to {Count} non-empty properties: {Properties}",
                nonEmptyProperties.Count, string.Join(", ", nonEmptyProperties.Select(p => p.Name)));

            // Build alias mapping from ResourceFields
            var aliasMapping = BuildPropertyAliasMapping(report.ResourceFields);

            // Create display names for headers
            var finalHeaders = new List<(PropertyInfo Property, string DisplayName)>();
            foreach(var prop in nonEmptyProperties)
            {
                var displayName = aliasMapping.TryGetValue(prop.Name, out var alias) ? alias : prop.Name;
                finalHeaders.Add((prop, displayName));
            }

            // Write headers
            for(var col = 0; col < finalHeaders.Count; col++)
            {
                var cell = ws.Cell(headerRow, col + 1);
                cell.Value = finalHeaders[col].DisplayName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Write data rows
            var row = headerRow + 1;
            foreach(var label in labels)
            {
                var col = 1;

                foreach(var (property, _) in finalHeaders)
                {
                    var cell = ws.Cell(row, col);
                    var value = property.GetValue(label);
                    SetCellValue(cell, value);
                    col++;
                }

                row++;
            }

            var usedRange = ws.Range(headerRow, 1, Math.Max(row - 1, headerRow), finalHeaders.Count);
            usedRange.CreateTable();
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(headerRow);
            ws.SheetView.FreezeColumns(1);
        }

        /// <summary>
        /// Writes a hyperlink cell that links to another sheet.
        /// </summary>
        /// <param name="ws">The worksheet to write to.</param>
        /// <param name="row">The row number (1-based).</param>
        /// <param name="sheetName">The physical name of the target sheet.</param>
        /// <param name="label">The display text for the link.</param>
        /// <param name="description">The description text displayed in the next column.</param>
        private static void WriteIndexLink(IXLWorksheet ws, int row, string sheetName, string label, string description)
        {
            var cell = ws.Cell(row, 1);
            cell.Value = label;
            cell.SetHyperlink(new XLHyperlink($"'{sheetName}'!A1"));
            cell.Style.Font.Underline = XLFontUnderlineValues.Single;

            ws.Cell(row, 2).Value = description;
        }

        /// <summary>
        /// Writes a "Back to Index" hyperlink at the top of a worksheet.
        /// </summary>
        /// <param name="ws">The worksheet to write to.</param>
        private static void WriteBackToIndex(IXLWorksheet ws)
        {
            ws.Cell(1, 1).Value = "← Back to Index";
            var cell = ws.Cell(1, 1);
            cell.SetHyperlink(new XLHyperlink("'Summary'!A1"));
            cell.Style.Font.Underline = XLFontUnderlineValues.Single;
        }

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
