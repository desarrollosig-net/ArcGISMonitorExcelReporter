using ClosedXML.Excel;
using System.Reflection;

namespace ArcGISMonitorExcelReporterLib.Reporting;

public sealed class MonitorExcelReportWriter
{
    public void Save(MonitorExcelReport report, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("La ruta del archivo Excel es obligatoria.", nameof(outputPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        using var workbook = new XLWorkbook();
        var sheetRegistry = new SheetRegistry(workbook);

        WriteSummary(workbook, sheetRegistry, report);
        WriteTableSheet(workbook, sheetRegistry, "Colecciones", report.Collections);
        WriteTableSheet(workbook, sheetRegistry, "Componentes", report.Components);
        WriteTableSheet(workbook, sheetRegistry, "Metricas", report.Metrics);
        WriteTableSheet(workbook, sheetRegistry, "Datos_Metricas", report.MetricData);
        WriteTableSheet(workbook, sheetRegistry, "Alertas", report.Alerts);

        WriteCollectionSheets(workbook, sheetRegistry, report);
        WriteMetricSheets(workbook, sheetRegistry, report);

        workbook.SaveAs(outputPath);
    }

    private static void WriteSummary(XLWorkbook workbook, SheetRegistry sheetRegistry, MonitorExcelReport report)
    {
        var ws = workbook.Worksheets.Add("Resumen");
        sheetRegistry.Register("Resumen", "Resumen");

        ws.Cell(1, 1).Value = "ArcGIS Monitor - Reporte Excel";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        var metadata = new (string Label, object? Value)[]
        {
            ("Generado UTC", report.GeneratedAtUtc),
            ("Desde UTC", report.FromUtc),
            ("Hasta UTC", report.ToUtc),
            ("Colecciones", report.Collections.Count),
            ("Componentes", report.Components.Count),
            ("Métricas", report.Metrics.Count),
            ("Datos de métricas", report.MetricData.Count),
            ("Alertas", report.Alerts.Count)
        };

        var row = 3;
        foreach (var item in metadata)
        {
            ws.Cell(row, 1).Value = item.Label;
            SetCellValue(ws.Cell(row, 2), item.Value);
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Índice";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Colecciones"), "Colecciones", "Resumen de colecciones consultadas");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Componentes"), "Componentes", "Inventario de componentes retornados");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Metricas"), "Metricas", "Catálogo de métricas asociadas a componentes");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Datos_Metricas"), "Datos_Metricas", "Series o agregados de datos de métricas");
        WriteIndexLink(ws, row++, sheetRegistry.GetOrCreatePhysicalName("Alertas"), "Alertas", "Alertas asociadas a las métricas consultadas");

        row += 1;
        ws.Cell(row, 1).Value = "Colecciones consultadas";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Colección";
        ws.Cell(row, 2).Value = "Tipo componente";
        ws.Cell(row, 3).Value = "Componentes";
        ws.Cell(row, 4).Value = "Métricas";
        ws.Cell(row, 5).Value = "Alertas";
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
        ws.Cell(row, 1).Value = "Métricas";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Métrica";
        ws.Cell(row, 2).Value = "Cantidad";
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

            ws.Cell(2, 1).Value = "Colección";
            ws.Cell(2, 2).Value = collection.CollectionName;
            ws.Cell(3, 1).Value = "Tipo componente";
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

            ws.Cell(2, 1).Value = "Métrica";
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
            ws.Cell(startRow, 1).Value = "Sin columnas.";
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
        ws.Cell(1, 1).FormulaA1 = HyperlinkFormula("Resumen", "Volver al índice");
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
                sanitized = "Hoja";

            return sanitized.Length <= 31 ? sanitized : sanitized[..31];
        }
    }
}
