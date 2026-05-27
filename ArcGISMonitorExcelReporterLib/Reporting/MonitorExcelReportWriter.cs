using ClosedXML.Excel;
using System.Reflection;
using Serilog;

namespace ArcGISMonitorExcelReporterLib.Reporting;

public sealed class MonitorExcelReportWriter
{
    public void Save(MonitorExcelReport report, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Excel file path is required.", nameof(outputPath));

        Log.Information("Creating Excel workbook...");

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            Log.Debug("Output directory: {Directory}", directory);
        }

        using var workbook = new XLWorkbook();
        var sheetRegistry = new SheetRegistry(workbook);

        Log.Debug("Writing Summary sheet...");
        WriteSummary(workbook, sheetRegistry, report);

        Log.Debug("Writing Collections sheet ({Count} rows)...", report.Collections.Count);
        WriteTableSheet(workbook, sheetRegistry, "Collections", report.Collections);

        Log.Debug("Writing Components sheet ({Count} rows)...", report.Components.Count);
        WriteTableSheet(workbook, sheetRegistry, "Components", report.Components);

        Log.Debug("Writing Metrics sheet ({Count} rows)...", report.Metrics.Count);
        WriteTableSheet(workbook, sheetRegistry, "Metrics", report.Metrics);

        Log.Debug("Writing Metric_Data sheet ({Count} rows)...", report.MetricData.Count);
        WriteTableSheet(workbook, sheetRegistry, "Metric_Data", report.MetricData);

        Log.Debug("Writing Alerts sheet ({Count} rows)...", report.Alerts.Count);
        WriteTableSheet(workbook, sheetRegistry, "Alerts", report.Alerts);

        Log.Debug("Writing collection-specific sheets...");
        WriteCollectionSheets(workbook, sheetRegistry, report);

        Log.Debug("Writing metric-specific sheets...");
        WriteMetricSheets(workbook, sheetRegistry, report);

        Log.Information("Saving Excel file to: {OutputPath}", outputPath);
        workbook.SaveAs(outputPath);

        var fileInfo = new FileInfo(outputPath);
        Log.Information("Excel file saved successfully. Size: {Size:N0} bytes", fileInfo.Length);
    }

    private static void WriteSummary(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
    {
        var ws = workbook.Worksheets.Add("Summary");
        sheetRegistry.Register("Summary", "Summary");

        ws.Cell(1, 1).Value = "ArcGIS Monitor - Excel Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        var metadata = new (string Label, object? Value)[]
        {
            ("Generated UTC", report.GeneratedAtUtc),
            ("From UTC", report.FromUtc),
            ("To UTC", report.ToUtc),
            ("Collections", report.Collections.Count),
            ("Components", report.Components.Count),
            ("Metrics", report.Metrics.Count),
            ("Metric Data", report.MetricData.Count),
            ("Alerts", report.Alerts.Count)
        };

        var row = 3;
        foreach (var item in metadata)
        {
            ws.Cell(row, 1).Value = item.Label;
            SetCellValue(ws.Cell(row, 2), item.Value);
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Index";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Collections"), "Collections", "Summary of queried collections");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Components"), "Components", "Inventory of returned components");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Metrics"), "Metrics", "Catalog of metrics associated with components");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Metric_Data"), "Metric_Data", "Series or aggregates of metric data");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Alerts"), "Alerts", "Alerts associated with queried metrics");

        row += 1;
        ws.Cell(row, 1).Value = "Queried Collections";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Collection";
        ws.Cell(row, 2).Value = "Component Type";
        ws.Cell(row, 3).Value = "Components";
        ws.Cell(row, 4).Value = "Metrics";
        ws.Cell(row, 5).Value = "Alerts";
        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        row++;

        foreach (var collection in report.Collections.OrderBy(c => c.CollectionName).ThenBy(c => c.ComponentType))
        {
            var collectionSheetName = SheetRegistry.BuildCollectionSheetName(collection.CollectionName, collection.ComponentType);
            ws.Cell(row, 1).FormulaA1 = HyperlinkFormula(sheetRegistry.GetOrCreatePhysicalName(collectionSheetName), collection.CollectionName);
            ws.Cell(row, 2).Value = collection.ComponentType;
            ws.Cell(row, 3).Value = collection.ComponentCount;
            ws.Cell(row, 4).Value = collection.MetricCount;
            ws.Cell(row, 5).Value = collection.AlertCount;
            row++;
        }

        row += 1;
        ws.Cell(row, 1).Value = "Metrics";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Metric";
        ws.Cell(row, 2).Value = "Count";
        ws.Range(row, 1, row, 2).Style.Font.Bold = true;
        row++;

        foreach (var metricGroup in report.Metrics
                     .GroupBy(m => string.IsNullOrWhiteSpace(m.MetricName) ? $"MetricId {m.MetricId}" : m.MetricName!)
                     .OrderBy(g => g.Key))
        {
            var metricSheetName = SheetRegistry.BuildMetricSheetName(metricGroup.Key);
            ws.Cell(row, 1).FormulaA1 = HyperlinkFormula(sheetRegistry.GetOrCreatePhysicalName(metricSheetName), metricGroup.Key);
            ws.Cell(row, 2).Value = metricGroup.Count();
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static void WriteCollectionSheets(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
    {
        foreach (var collection in report.Collections.OrderBy(c => c.CollectionName).ThenBy(c => c.ComponentType))
        {
            var logicalName = SheetRegistry.BuildCollectionSheetName(collection.CollectionName, collection.ComponentType);
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName(logicalName));
            WriteBackToIndex(ws);

            ws.Cell(2, 1).Value = "Collection";
            ws.Cell(2, 2).Value = collection.CollectionName;
            ws.Cell(3, 1).Value = "Component Type";
            ws.Cell(3, 2).Value = collection.ComponentType;

            var rows = report.Components
                .Where(c => c.CollectionName.Equals(collection.CollectionName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(c.Type, collection.ComponentType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name)
                .ToList();

            WriteRows(ws, 5, rows);
        }
    }

    private static void WriteMetricSheets(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
    {
        foreach (var metricGroup in report.Metrics.GroupBy(m => string.IsNullOrWhiteSpace(m.MetricName) ? $"MetricId {m.MetricId}" : m.MetricName!))
        {
            var logicalName = SheetRegistry.BuildMetricSheetName(metricGroup.Key);
            var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName(logicalName));
            WriteBackToIndex(ws);

            ws.Cell(2, 1).Value = "Metric";
            ws.Cell(2, 2).Value = metricGroup.Key;

            WriteRows(ws, 4, metricGroup.OrderBy(m => m.CollectionName).ThenBy(m => m.ComponentName).ToList());
        }
    }

    private static void WriteTableSheet<T>(XLWorkbook workbook, SheetRegistry sheetRegistry, string logicalName, IReadOnlyCollection<T> rows)
    {
        var ws = workbook.Worksheets.Add(sheetRegistry.GetOrCreatePhysicalName(logicalName));
        WriteBackToIndex(ws);
        WriteRows(ws, 3, rows);
    }

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

    private static void WriteBackToIndex(IXLWorksheet ws)
    {
        ws.Cell(1, 1).FormulaA1 = HyperlinkFormula("Summary", "Back to index");
        ws.Cell(1, 1).Style.Font.Bold = true;
    }

    private static void WriteIndexLink(IXLWorksheet ws, int row, string targetSheet, string label, string description)
    {
        ws.Cell(row, 1).FormulaA1 = HyperlinkFormula(targetSheet, label);
        ws.Cell(row, 2).Value = description;
    }

    private static string HyperlinkFormula(string sheetName, string text)
    {
        var escapedSheet = sheetName.Replace("'", "''");
        var escapedText = text.Replace("\"", "\"\"");
        return $"HYPERLINK(\"#'{escapedSheet}'!A1\",\"{escapedText}\")";
    }

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

    private sealed class SheetRegistry
    {
        private readonly XLWorkbook _workbook;
        private readonly Dictionary<string, string> _logicalToPhysical = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _usedPhysicalNames = new(StringComparer.OrdinalIgnoreCase);

        public SheetRegistry(XLWorkbook workbook)
        {
            _workbook = workbook;
        }

        public void Register(string logicalName, string physicalName)
        {
            _logicalToPhysical[logicalName] = physicalName;
            _usedPhysicalNames.Add(physicalName);
        }

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

        public static string BuildCollectionSheetName(string collectionName, string componentType)
            => $"COL_{collectionName}_{componentType}";

        public static string BuildMetricSheetName(string metricName)
            => $"MET_{metricName}";

        private static string SanitizeSheetName(string value)
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
