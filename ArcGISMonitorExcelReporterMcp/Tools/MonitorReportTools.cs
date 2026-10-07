using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

using ArcGISMonitorExcelReporterLib;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using Serilog;

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
        private static readonly JsonSerializerOptions ResponseJsonOptions = ConfigurationLoader.ResponseJsonOptions;

        [McpServerTool(Name = "validate_configuration"),
         Description("Validates an ArcGIS Monitor Excel Reporter configuration (server connection and report settings) without querying the server. Returns { valid, error }.")]
        public static async Task<string> ValidateConfigurationAsync(
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
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
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            CancellationToken cancellationToken = default)
        {
            return await ToolErrorHandling.RunAsync("build_report_summary", async () =>
            {
                var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
                var reporter = new ArcGisMonitorExcelReporter();

                Log.Information("Building report summary (no Excel output)");
                var report = await reporter.BuildReportAsync(configuration, cancellationToken).ConfigureAwait(false);

                var summary = new
                {
                    serverUrl = report.ServerUrl,
                    // The library leaves CollectionName null when every collection was queried.
                    collectionName = report.CollectionName ?? "*",
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
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Whether <c>generate_excel_report</c> returns the workbook inside the tool result instead of
        /// leaving it on the server's disk. Set to <c>true</c> under the HTTP transport: a remote
        /// client cannot reach the server's file system, and the container's disk is ephemeral.
        /// </summary>
        public static bool EmbedGeneratedReport { get; set; }

        /// <summary>
        /// Upper bound for a workbook returned inline. Base64 inflates the payload by ~33%, and very
        /// large results are impractical for MCP clients; callers should narrow the report instead.
        /// </summary>
        internal const long MaxEmbeddedReportBytes = 20 * 1024 * 1024;

        private const string XlsxMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        [McpServerTool(Name = "generate_excel_report"),
         Description("Queries ArcGIS Monitor and builds a full Excel report. Over stdio the .xlsx is written to disk and { outputPath, executionTime } is returned. Over HTTP the .xlsx is returned in the result as an embedded resource, together with { fileName, sizeBytes, executionTime }.")]
        public static async Task<CallToolResult> GenerateExcelReportAsync(
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            [Description("Full path where the .xlsx file should be written. If omitted, a timestamped file is created under a 'reports' folder next to the server executable. Only available over stdio transport; rejected when the server runs in --http mode.")] string? outputPath = null,
            CancellationToken cancellationToken = default)
        {
            return await ToolErrorHandling.RunAsync("generate_excel_report", async () =>
            {
                if(EmbedGeneratedReport && !string.IsNullOrWhiteSpace(outputPath))
                {
                    throw new ArgumentException("outputPath is not available over the HTTP transport (it would let a remote client write arbitrary files on the server). Omit it; the report is returned in the result.");
                }

                var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
                var reporter = new ArcGisMonitorExcelReporter();
                var resolvedOutputPath = EmbedGeneratedReport ? CreateTemporaryOutputPath() : ResolveOutputPath(outputPath);

                Log.Information("Generating Excel report to {OutputPath}", resolvedOutputPath);
                var stopwatch = Stopwatch.StartNew();
                var generatedPath = await reporter.GenerateExcelAsync(configuration, resolvedOutputPath, stopwatch, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                var executionTime = stopwatch.Elapsed.ToString("hh\\:mm\\:ss");

                if(!EmbedGeneratedReport)
                {
                    var result = new
                    {
                        outputPath = Path.GetFullPath(generatedPath),
                        executionTime
                    };

                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result, ResponseJsonOptions) }]
                    };
                }

                return await EmbedReportAsync(generatedPath, executionTime, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a generated workbook into a tool result (summary text plus the .xlsx as an embedded
        /// blob resource) and deletes the temporary file and its folder.
        /// </summary>
        internal static async Task<CallToolResult> EmbedReportAsync(string generatedPath, string executionTime, CancellationToken cancellationToken)
        {
            try
            {
                var fileInfo = new FileInfo(generatedPath);
                if(fileInfo.Length > MaxEmbeddedReportBytes)
                {
                    throw new InvalidOperationException(
                        $"The generated report is {fileInfo.Length / (1024 * 1024)} MB, above the {MaxEmbeddedReportBytes / (1024 * 1024)} MB limit for returning it over HTTP. " +
                        "Narrow the report (fewer component types, a shorter period, report.metrics.include_only, or max_metric_ids_for_time_series) and try again.");
                }

                var bytes = await File.ReadAllBytesAsync(generatedPath, cancellationToken).ConfigureAwait(false);
                var fileName = fileInfo.Name;

                var summary = new
                {
                    fileName,
                    sizeBytes = bytes.LongLength,
                    executionTime
                };

                return new CallToolResult
                {
                    Content =
                    [
                        new TextContentBlock { Text = JsonSerializer.Serialize(summary, ResponseJsonOptions) },
                        new EmbeddedResourceBlock
                        {
                            Resource = BlobResourceContents.FromBytes(bytes, $"report://{Uri.EscapeDataString(fileName)}", XlsxMimeType)
                        }
                    ]
                };
            }
            finally
            {
                TryDeleteDirectory(Path.GetDirectoryName(generatedPath));
            }
        }

        private static string CreateTemporaryOutputPath()
        {
            // A per-call folder keeps concurrent requests from colliding on the timestamped name.
            var folder = Path.Combine(Path.GetTempPath(), "arcgis-mcp-reports", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        private static void TryDeleteDirectory(string? folder)
        {
            if(string.IsNullOrEmpty(folder))
            {
                return;
            }

            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
            {
                Log.Warning(ex, "Could not delete temporary report folder {Folder}", folder);
            }
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
