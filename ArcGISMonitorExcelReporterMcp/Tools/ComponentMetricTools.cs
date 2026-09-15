using System.ComponentModel;
using System.Text.Json;

using ArcGISMonitorExcelReporterLib;

using ModelContextProtocol.Server;

using Serilog;

namespace ArcGISMonitorExcelReporterMcp.Tools
{
    /// <summary>
    /// Low-level MCP tools for querying individual components, their metric definitions, metric
    /// statistics, raw time series, and open alerts directly from ArcGIS Monitor, without going
    /// through the full report-building pipeline in <see cref="MonitorReportTools"/>.
    /// </summary>
    [McpServerToolType]
    public sealed class ComponentMetricTools
    {
        private static readonly JsonSerializerOptions ResponseJsonOptions = ConfigurationLoader.ResponseJsonOptions;

        [McpServerTool(Name = "list_components"),
         Description("Lists components (id, name, type, state) from ArcGIS Monitor, optionally filtered by collection, component type, or a name substring. Use this to discover component ids before calling get_component_metrics or get_metric_time_series.")]
        public static async Task<string> ListComponentsAsync(
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            [Description("Collection name to query. Use null, empty, or \"*\" for all collections.")] string? collectionName = "*",
            [Description("Component type to filter by (e.g. \"host\", \"service\", \"database\"). Omit for all types.")] string? componentType = null,
            [Description("If provided, only components whose name contains this text (case-insensitive) are returned.")] string? nameContains = null,
            [Description("Number of records per page when paginating through ArcGIS Monitor. Default is 100.")] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
            var reporter = new ArcGisMonitorExcelReporter();

            using var connection = await reporter.ConnectAsync(configuration, cancellationToken).ConfigureAwait(false);

            var toUtc = DateTimeOffset.UtcNow;
            var fromUtc = toUtc.AddDays(-1);
            var componentTypes = string.IsNullOrWhiteSpace(componentType) ? null : new List<string> { componentType };

            Log.Information("Listing components for collection {Collection}, type {Type}", collectionName, componentType);

            var components = await connection.Queries.GetAllComponentsWithMetricsAsync(
                string.IsNullOrWhiteSpace(collectionName) ? "*" : collectionName,
                fromUtc,
                toUtc,
                pageSize,
                componentTypes,
                cancellationToken).ConfigureAwait(false);

            var items = components
                .Select(c => c.Attributes)
                .Where(a => string.IsNullOrWhiteSpace(nameContains) || (a.Name?.Contains(nameContains, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    type = a.Type,
                    subtype = a.Subtype,
                    state = a.State,
                    systemId = a.SystemId
                })
                .ToList();

            return JsonSerializer.Serialize(new { count = items.Count, components = items }, ResponseJsonOptions);
        }

        [McpServerTool(Name = "get_component_metrics"),
         Description("Returns the metric definitions (name, unit, thresholds, alerting config) for a single component, looked up by componentId or componentName. Use list_components first to find the id.")]
        public static async Task<string> GetComponentMetricsAsync(
            [Description("Component id, as returned by list_components. Provide this or componentName.")] long? componentId = null,
            [Description("Component name (exact match, case-insensitive). Provide this or componentId.")] string? componentName = null,
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            [Description("Collection name to search in. Use null, empty, or \"*\" for all collections.")] string? collectionName = "*",
            [Description("Component type to narrow the search (e.g. \"host\"). Omit to search all types.")] string? componentType = null,
            CancellationToken cancellationToken = default)
        {
            if(componentId is null && string.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentException("Provide either componentId or componentName.");
            }

            var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
            var reporter = new ArcGisMonitorExcelReporter();

            using var connection = await reporter.ConnectAsync(configuration, cancellationToken).ConfigureAwait(false);

            var toUtc = DateTimeOffset.UtcNow;
            var fromUtc = toUtc.AddDays(-1);
            var componentTypes = string.IsNullOrWhiteSpace(componentType) ? null : new List<string> { componentType };

            var components = await connection.Queries.GetAllComponentsWithMetricsAsync(
                string.IsNullOrWhiteSpace(collectionName) ? "*" : collectionName,
                fromUtc,
                toUtc,
                100,
                componentTypes,
                cancellationToken).ConfigureAwait(false);

            var component = components.FirstOrDefault(c =>
                (componentId.HasValue && c.Attributes.Id == componentId.Value) ||
                (!string.IsNullOrWhiteSpace(componentName) && string.Equals(c.Attributes.Name, componentName, StringComparison.OrdinalIgnoreCase)));

            if(component is null)
            {
                return JsonSerializer.Serialize(new { found = false, component = (object?)null, metrics = Array.Empty<object>() }, ResponseJsonOptions);
            }

            var metrics = (component.Metrics ?? [])
                .Select(m => new
                {
                    metricId = m.Attributes.Id,
                    name = m.Attributes.Name,
                    unit = m.Attributes.Unit,
                    isAlertingEnabled = m.Attributes.IsAlertingEnabled,
                    aggregation = m.Attributes.Aggregation,
                    @operator = m.Attributes.Operator,
                    infoThreshold = m.Attributes.InfoThreshold,
                    warningThreshold = m.Attributes.WarningThreshold,
                    criticalThreshold = m.Attributes.CriticalThreshold
                })
                .ToList();

            var result = new
            {
                found = true,
                component = new
                {
                    id = component.Attributes.Id,
                    name = component.Attributes.Name,
                    type = component.Attributes.Type,
                    state = component.Attributes.State
                },
                metricsCount = metrics.Count,
                metrics
            };

            return JsonSerializer.Serialize(result, ResponseJsonOptions);
        }

        [McpServerTool(Name = "get_metric_stats"),
         Description("Returns aggregated statistics (avg, min, max, stddev, p95, count) for metrics matching a name pattern, over a time range, grouped by component. Use this to answer 'how did metric X behave' without pulling a full report.")]
        public static async Task<string> GetMetricStatsAsync(
            [Description("Substring to match against metric names (case-insensitive), e.g. \"CPU\".")] string metricNameLike,
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            [Description("Collection name to query. Use null, empty, or \"*\" for all collections.")] string? collectionName = "*",
            [Description("Component type to filter by (e.g. \"host\"). Use \"*\" for all types.")] string componentType = "*",
            [Description("Start of the statistics period (UTC). Defaults to 24 hours before toUtc.")] DateTimeOffset? fromUtc = null,
            [Description("End of the statistics period (UTC). Defaults to now.")] DateTimeOffset? toUtc = null,
            [Description("Number of records per page when paginating through ArcGIS Monitor. Default is 100.")] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(metricNameLike))
            {
                throw new ArgumentException("metricNameLike must be provided.", nameof(metricNameLike));
            }

            var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
            var reporter = new ArcGisMonitorExcelReporter();

            using var connection = await reporter.ConnectAsync(configuration, cancellationToken).ConfigureAwait(false);

            var resolvedToUtc = toUtc ?? DateTimeOffset.UtcNow;
            var resolvedFromUtc = fromUtc ?? resolvedToUtc.AddDays(-1);

            Log.Information("Fetching metric stats for {MetricLike} from {From} to {To}", metricNameLike, resolvedFromUtc, resolvedToUtc);

            var components = await connection.Queries.GetComponentsWithMetricStatsAsync(
                string.IsNullOrWhiteSpace(collectionName) ? "*" : collectionName,
                string.IsNullOrWhiteSpace(componentType) ? "*" : componentType,
                metricNameLike,
                resolvedFromUtc,
                resolvedToUtc,
                pageSize,
                cancellationToken).ConfigureAwait(false);

            var rows = components
                .SelectMany(c => (c.Metrics ?? []).Select(m =>
                {
                    var stats = m.MetricsData?.FirstOrDefault()?.Attributes;
                    return new
                    {
                        componentId = c.Attributes.Id,
                        componentName = c.Attributes.Name,
                        componentType = c.Attributes.Type,
                        metricId = m.Attributes.Id,
                        metricName = m.Attributes.Name,
                        unit = m.Attributes.Unit,
                        avg = stats?.AvgValue,
                        min = stats?.MinValue,
                        max = stats?.MaxValue,
                        stdDev = stats?.StdDevValue,
                        p95 = stats?.Percentile95Value,
                        sum = stats?.SumValue,
                        count = stats?.CountValue
                    };
                }))
                .ToList();

            return JsonSerializer.Serialize(new { fromUtc = resolvedFromUtc, toUtc = resolvedToUtc, count = rows.Count, metrics = rows }, ResponseJsonOptions);
        }

        [McpServerTool(Name = "get_metric_time_series"),
         Description("Returns raw time-bucketed data points (avg/min/max/count per bucket) for one or more metric ids over a time range. Use list_components/get_component_metrics or get_metric_stats first to find metric ids.")]
        public static async Task<string> GetMetricTimeSeriesAsync(
            [Description("Metric ids to fetch time series for, as returned by get_component_metrics or get_metric_stats.")] long[] metricIds,
            [Description("Start of the time series period (UTC).")] DateTimeOffset fromUtc,
            [Description("End of the time series period (UTC).")] DateTimeOffset toUtc,
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            [Description("Time bucket for aggregation, as \"observed_at:<interval>\" (e.g. \"observed_at:15m\", \"observed_at:1h\", \"observed_at:1d\"). Default is 15-minute buckets.")] string bucket = "observed_at:15m",
            CancellationToken cancellationToken = default)
        {
            if(metricIds is null || metricIds.Length == 0)
            {
                throw new ArgumentException("At least one metric id must be provided.", nameof(metricIds));
            }

            if(fromUtc >= toUtc)
            {
                throw new ArgumentException("fromUtc must be earlier than toUtc.", nameof(fromUtc));
            }

            var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
            var reporter = new ArcGisMonitorExcelReporter();

            using var connection = await reporter.ConnectAsync(configuration, cancellationToken).ConfigureAwait(false);

            Log.Information("Fetching time series for {Count} metric(s) with bucket {Bucket}", metricIds.Length, bucket);

            var response = await connection.Queries.GetMetricTimeSeriesAsync(
                metricIds,
                fromUtc,
                toUtc,
                bucket,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var series = response.Features
                .Select(f => new
                {
                    metricId = f.Attributes.Id,
                    metricName = f.Attributes.Name,
                    componentId = f.Attributes.ComponentId,
                    componentName = f.Attributes.ComponentName,
                    unit = f.Attributes.Unit,
                    dataPoints = (f.MetricsData ?? [])
                        .Select(d => new
                        {
                            observedAt = d.Attributes.ObservedAt,
                            avg = d.Attributes.AvgValue,
                            min = d.Attributes.MinValue,
                            max = d.Attributes.MaxValue,
                            stdDev = d.Attributes.StdDevValue,
                            p95 = d.Attributes.Percentile95Value,
                            sum = d.Attributes.SumValue,
                            count = d.Attributes.CountValue
                        })
                        .ToList()
                })
                .ToList();

            return JsonSerializer.Serialize(new { bucket, fromUtc, toUtc, metrics = series }, ResponseJsonOptions);
        }

        [McpServerTool(Name = "list_open_alerts"),
         Description("Lists currently open alerts (metric threshold breaches that have not closed) for a collection/component type, without generating a full report.")]
        public static async Task<string> ListOpenAlertsAsync(
            [Description("Path to a JSON configuration file on disk. Provide this or configJson, not both. Only available over stdio transport; rejected when the server runs in --http mode.")] string? configPath = null,
            [Description("Inline JSON configuration content (same shape as the config file). Provide this or configPath, not both.")] string? configJson = null,
            [Description("Collection name to query. Use null, empty, or \"*\" for all collections.")] string? collectionName = "*",
            [Description("Component type to filter by (e.g. \"host\"). Omit for all types.")] string? componentType = null,
            [Description("Start of the alert lookup window (UTC). Defaults to 24 hours before toUtc.")] DateTimeOffset? fromUtc = null,
            [Description("End of the alert lookup window (UTC). Defaults to now.")] DateTimeOffset? toUtc = null,
            [Description("Number of records per page when paginating through ArcGIS Monitor. Default is 100.")] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath, configJson, cancellationToken).ConfigureAwait(false);
            var reporter = new ArcGisMonitorExcelReporter();

            using var connection = await reporter.ConnectAsync(configuration, cancellationToken).ConfigureAwait(false);

            var resolvedToUtc = toUtc ?? DateTimeOffset.UtcNow;
            var resolvedFromUtc = fromUtc ?? resolvedToUtc.AddDays(-1);
            var componentTypes = string.IsNullOrWhiteSpace(componentType) ? null : new List<string> { componentType };

            Log.Information("Listing open alerts for collection {Collection}, type {Type}", collectionName, componentType);

            var components = await connection.Queries.GetAllComponentsWithMetricsAsync(
                string.IsNullOrWhiteSpace(collectionName) ? "*" : collectionName,
                resolvedFromUtc,
                resolvedToUtc,
                pageSize,
                componentTypes,
                cancellationToken).ConfigureAwait(false);

            var openAlerts = components
                .SelectMany(c => c.Metrics ?? [])
                .SelectMany(m => m.Alerts ?? [])
                .Where(a => string.Equals(a.Attributes.State, "open", StringComparison.OrdinalIgnoreCase))
                .Select(a => new
                {
                    alertId = a.Attributes.Id,
                    state = a.Attributes.State,
                    openedAt = a.Attributes.OpenedAt,
                    componentId = a.Attributes.ComponentId,
                    componentName = a.Attributes.ComponentName,
                    componentType = a.Attributes.ComponentType,
                    metricId = a.Attributes.MetricId,
                    metricName = a.Attributes.MetricName,
                    metricUnit = a.Attributes.MetricUnit,
                    warningThreshold = a.Attributes.WarningThreshold,
                    criticalThreshold = a.Attributes.CriticalThreshold
                })
                .ToList();

            return JsonSerializer.Serialize(new { fromUtc = resolvedFromUtc, toUtc = resolvedToUtc, count = openAlerts.Count, alerts = openAlerts }, ResponseJsonOptions);
        }
    }
}
