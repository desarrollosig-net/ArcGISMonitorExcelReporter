using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

using ArcGISMonitorExcelReporterLib;

using ModelContextProtocol.Server;

using Serilog;

using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

namespace ArcGISMonitorExcelReporterMcp.Tools
{
    /// <summary>
    /// MCP tools that wrap <see cref="ArcGisMonitorExcelReporter"/> so an MCP client can validate
    /// configuration, query a report summary, or generate a full Excel report without running the
    /// console application manually.
    /// </summary>
    [McpServerToolType]
    public sealed class MonitorReportTools
    {
        private static readonly JsonSerializerOptions ResponseJsonOptions = new()
        {
            WriteIndented = true
        };

        [McpServerTool(Name = "validate_configuration"),
         Description("Validates an ArcGIS Monitor Excel Reporter configuration (server connection and report settings) without querying the server. Returns { valid, error }.")]
        public static async Task<string> ValidateConfigurationAsync(
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
                configuration.Validate();
                return JsonSerializer.Serialize(new { valid = true, error = (string?)null }, ResponseJsonOptions);
            }
            catch(Exception ex) when(ex is InvalidOperationException or JsonException or FileNotFoundException or ArgumentException)
            {
                Log.Warning(ex, "Configuration validation failed");
                return JsonSerializer.Serialize(new { valid = false, error = ex.Message }, ResponseJsonOptions);
            }
        }

        [McpServerTool(Name = "build_report_summary"),
         Description("Queries ArcGIS Monitor and returns a lightweight JSON summary of the report (counts, per-collection breakdown, open alerts) without generating an Excel file.")]
        public static async Task<string> BuildReportSummaryAsync(
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            CancellationToken cancellationToken = default)
        {
            var configuration = await LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
            var reporter = new ArcGisMonitorExcelReporter();

            Log.Information("Building report summary (no Excel output)");
            var report = await reporter.BuildReportAsync(configuration, cancellationToken).ConfigureAwait(false);

            var summary = new
            {
                serverUrl = report.ServerUrl,
                collectionName = report.CollectionName,
                fromUtc = report.FromUtc,
                toUtc = report.ToUtc,
                collectionsCount = report.Collections.Count,
                componentsCount = report.Components.Count,
                metricsCount = report.Metrics.Count,
                alertsCount = report.Alerts.Count,
                collections = report.Collections,
                openAlerts = report.Alerts.Where(a => string.Equals(a.State, "open", StringComparison.OrdinalIgnoreCase)).ToList()
            };

            return JsonSerializer.Serialize(summary, ResponseJsonOptions);
        }

        [McpServerTool(Name = "generate_excel_report"),
         Description("Queries ArcGIS Monitor and writes a full Excel report to disk. Returns { outputPath, executionTime }.")]
        public static async Task<string> GenerateExcelReportAsync(
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            [Description("Full path where the .xlsx file should be written. If omitted, a timestamped file is created under a 'reports' folder next to the server executable.")] string? outputPath = null,
            CancellationToken cancellationToken = default)
        {
            var configuration = await LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
            var reporter = new ArcGisMonitorExcelReporter();
            var resolvedOutputPath = ResolveOutputPath(outputPath);

            Log.Information("Generating Excel report to {OutputPath}", resolvedOutputPath);
            var stopwatch = Stopwatch.StartNew();
            var generatedPath = await reporter.GenerateExcelAsync(configuration, resolvedOutputPath, stopwatch, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var result = new
            {
                outputPath = Path.GetFullPath(generatedPath),
                executionTime = stopwatch.Elapsed.ToString("hh\\:mm\\:ss")
            };

            return JsonSerializer.Serialize(result, ResponseJsonOptions);
        }

        private static async Task<ReporterConfiguration> LoadConfigurationAsync(string? configPath, string? configJson, CancellationToken cancellationToken)
        {
            if(!string.IsNullOrWhiteSpace(configPath) && !string.IsNullOrWhiteSpace(configJson))
            {
                throw new ArgumentException("Provide either configPath or configJson, not both.");
            }

            if(!string.IsNullOrWhiteSpace(configPath))
            {
                return await ReporterConfiguration.LoadAsync(configPath, cancellationToken).ConfigureAwait(false);
            }

            if(!string.IsNullOrWhiteSpace(configJson))
            {
                var configuration = JsonSerializer.Deserialize<ReporterConfiguration>(configJson, ArcGISMonitorExcelReporterLib.Models.MonitorJson.Options);
                return configuration ?? throw new JsonException("Unable to deserialize configJson.");
            }

            throw new ArgumentException("Either configPath or configJson must be provided.");
        }

        private static string ResolveOutputPath(string? outputPath)
        {
            if(!string.IsNullOrWhiteSpace(outputPath))
            {
                return outputPath;
            }

            var reportsFolder = Path.Combine(AppContext.BaseDirectory, "reports");
            Directory.CreateDirectory(reportsFolder);
            return Path.Combine(reportsFolder, $"Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }
    }
}
