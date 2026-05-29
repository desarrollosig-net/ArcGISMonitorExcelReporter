using ArcGISMonitorExcelReporterLib.Client;
using ArcGISMonitorExcelReporterLib.Models;
using Serilog;

namespace ArcGISMonitorExcelReporterLib.Reporting
{
    /// <summary>
    /// Service for building comprehensive ArcGIS Monitor reports with components, metrics, and alerts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service orchestrates the entire report building process, coordinating multiple
    /// API queries to gather complete data about collections, components, metrics, and alerts.
    /// </para>
    /// <para>
    /// The service:
    /// <list type="bullet">
    /// <item><description><b>Queries components and metrics:</b> Retrieves data from ArcGIS Monitor using <see cref="ArcGisMonitorQueryService"/></description></item>
    /// <item><description><b>Applies filters:</b> Filters metrics based on include/exclude lists and alerting status</description></item>
    /// <item><description><b>Fetches time series:</b> Optionally retrieves time-bucketed metric data</description></item>
    /// <item><description><b>Builds report model:</b> Constructs a <see cref="MonitorExcelReport"/> ready for Excel export</description></item>
    /// <item><description><b>Logs progress:</b> Provides detailed logging at Information and Debug levels</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The report building process validates inputs, iterates through collections and component types,
    /// merges duplicate data, applies filters, and optionally fetches time series data.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var client = new ArcGisMonitorClient(new Uri("https://monitor.example.com:30443/"));
    /// await client.AuthenticateAsync("username", "password");
    /// 
    /// var queryService = new ArcGisMonitorQueryService(client);
    /// var reportService = new MonitorReportService(queryService);
    /// 
    /// var request = new MonitorReportRequest
    /// {
    ///     CollectionNames = ["Production"],
    ///     ComponentTypes = ["host", "service"],
    ///     FromUtc = DateTimeOffset.UtcNow.AddDays(-7),
    ///     ToUtc = DateTimeOffset.UtcNow,
    ///     IncludeMetricTimeSeries = false
    /// };
    /// 
    /// var report = await reportService.BuildReportAsync(request);
    /// Console.WriteLine($"Report: {report.Components.Count} components, {report.Metrics.Count} metrics");
    /// </code>
    /// </example>
    public sealed class MonitorReportService
    {
        private readonly ArcGisMonitorQueryService _queries;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorReportService"/> class.
        /// </summary>
        /// <param name="queries">The query service for accessing ArcGIS Monitor API.</param>
        /// <remarks>
        /// The provided query service must be configured with an authenticated client.
        /// </remarks>
        /// <example>
        /// <code>
        /// var client = new ArcGisMonitorClient(baseUri);
        /// await client.AuthenticateAsync(username, password);
        /// 
        /// var queryService = new ArcGisMonitorQueryService(client);
        /// var reportService = new MonitorReportService(queryService);
        /// </code>
        /// </example>
        public MonitorReportService(ArcGisMonitorQueryService queries)
        {
            _queries = queries;
        }

        /// <summary>
        /// Builds a comprehensive ArcGIS Monitor report based on the provided request parameters.
        /// </summary>
        /// <param name="request">The report request specifying collections, types, time range, and filters.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A <see cref="MonitorExcelReport"/> containing all queried and filtered data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if request validation fails (empty types, invalid date range).</exception>
        /// <remarks>
        /// <para>
        /// This method performs the following steps:
        /// <list type="number">
        /// <item><description><b>Validation:</b> Validates request parameters</description></item>
        /// <item><description><b>Data gathering:</b> Queries components with metrics/statistics for each collection and component type</description></item>
        /// <item><description><b>Deduplication:</b> Merges duplicate components when querying by metric name</description></item>
        /// <item><description><b>Filtering:</b> Applies include/exclude filters and alerting-only filter</description></item>
        /// <item><description><b>Time series:</b> Optionally fetches time-bucketed metric data (if <see cref="MonitorReportRequest.IncludeMetricTimeSeries"/> is true)</description></item>
        /// <item><description><b>Statistics update:</b> Recalculates component and collection counts after filtering</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b>
        /// <list type="bullet">
        /// <item><description>Empty list: Queries all collections</description></item>
        /// <item><description>Single entry with <c>null</c>, <c>""</c>, or <c>"*"</c>: Queries all collections</description></item>
        /// <item><description>Specific names: Queries only those collections</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Query modes:</b>
        /// <list type="bullet">
        /// <item><description>If <see cref="MonitorReportRequest.MetricNameLikes"/> is empty: Fetches all metrics without statistics</description></item>
        /// <item><description>If metric patterns specified: Fetches only matching metrics with aggregated statistics and alerts</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Performance considerations:</b>
        /// <list type="bullet">
        /// <item><description>Pagination is handled automatically by <see cref="ArcGisMonitorQueryService"/></description></item>
        /// <item><description>Use larger <see cref="MonitorReportRequest.PageSize"/> (200-500) for better performance with large datasets</description></item>
        /// <item><description>Set <see cref="MonitorReportRequest.IncludeMetricTimeSeries"/> to false if time series data is not needed</description></item>
        /// <item><description>Limit metrics with <see cref="MonitorReportRequest.MaxMetricIdsForTimeSeries"/> to control time series data volume</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Logging:</b> The method logs progress at Information level and detailed operations at Debug level.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>
        /// Basic report without time series:
        /// </para>
        /// <code>
        /// var request = new MonitorReportRequest
        /// {
        ///     CollectionNames = ["Production"],
        ///     ComponentTypes = ["host"],
        ///     FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
        ///     ToUtc = DateTimeOffset.UtcNow,
        ///     IncludeMetricTimeSeries = false
        /// };
        /// 
        /// var report = await reportService.BuildReportAsync(request);
        /// </code>
        /// <para>
        /// Report with all collections:
        /// </para>
        /// <code>
        /// var request = new MonitorReportRequest
        /// {
        ///     CollectionNames = ["*"],  // or [] or [""]
        ///     ComponentTypes = ["host"],
        ///     FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
        ///     ToUtc = DateTimeOffset.UtcNow,
        ///     IncludeMetricTimeSeries = false
        /// };
        /// 
        /// var report = await reportService.BuildReportAsync(request);
        /// </code>
        /// <para>
        /// Report with metric filters:
        /// </para>
        /// <code>
        /// var request = new MonitorReportRequest
        /// {
        ///     CollectionNames = ["Production"],
        ///     ComponentTypes = ["host", "service"],
        ///     MetricNameLikes = ["CPU", "Memory"],
        ///     FromUtc = DateTimeOffset.UtcNow.AddDays(-7),
        ///     ToUtc = DateTimeOffset.UtcNow,
        ///     IncludeOnlyMetricNames = ["CPU Utilized", "Memory Available"],
        ///     AlertingOnOnly = true,
        ///     IncludeMetricTimeSeries = true,
        ///     MaxMetricIdsForTimeSeries = 500
        /// };
        /// 
        /// var report = await reportService.BuildReportAsync(request);
        /// 
        /// Console.WriteLine($"Collections: {report.Collections.Count}");
        /// Console.WriteLine($"Components: {report.Components.Count}");
        /// Console.WriteLine($"Metrics: {report.Metrics.Count}");
        /// Console.WriteLine($"Alerts: {report.Alerts.Count}");
        /// Console.WriteLine($"Time series points: {report.MetricData.Count}");
        /// </code>
        /// </example>
        public async Task<MonitorExcelReport> BuildReportAsync(
            MonitorReportRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Allow empty collection names or "*" to query all collections
            var isAllCollections = request.CollectionNames.Count == 0 || 
                                  (request.CollectionNames.Count == 1 && 
                                   (string.IsNullOrWhiteSpace(request.CollectionNames[0]) || 
                                    request.CollectionNames[0].Trim() == "*"));

            if (!isAllCollections && request.CollectionNames.Count == 0)
                throw new ArgumentException("Must specify at least one collection or use \"*\" for all collections.", nameof(request));

            if (request.ComponentTypes.Count == 0)
                throw new ArgumentException("Must specify at least one component type.", nameof(request));
            if (request.FromUtc >= request.ToUtc)
                throw new ArgumentException("FromUtc must be less than ToUtc.", nameof(request));

            // Normalize collection names: if querying all, use a single entry with "*" or null
            var collectionsToQuery = isAllCollections 
                ? new List<string> { "*" } 
                : request.CollectionNames.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            Log.Information("Building report for {CollectionCount} collections and {TypeCount} component types", 
                isAllCollections ? "all" : collectionsToQuery.Count.ToString(), request.ComponentTypes.Count);

            var report = new MonitorExcelReport
            {
                FromUtc = request.FromUtc,
                ToUtc = request.ToUtc
            };

            foreach (var collectionName in collectionsToQuery)
            {
                foreach (var componentType in request.ComponentTypes.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var displayCollection = (collectionName == "*" || string.IsNullOrWhiteSpace(collectionName)) ? "All Collections" : collectionName;
                    Log.Information("Querying collection: {Collection}, component type: {Type}", displayCollection, componentType);

                    var components = new List<ComponentFeature>();

                    if (request.MetricNameLikes.Count == 0)
                    {
                        Log.Debug("Fetching all metrics for {Collection}/{Type}", displayCollection, componentType);
                        components.AddRange(await _queries.GetComponentsWithAllMetricsAsync(
                            collectionName,
                            componentType,
                            request.PageSize,
                            cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        Log.Debug("Fetching specific metrics: {Metrics}", string.Join(", ", request.MetricNameLikes));
                        foreach (var metricNameLike in request.MetricNameLikes.Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            components.AddRange(await _queries.GetComponentsWithMetricStatsAsync(
                                collectionName,
                                componentType,
                                metricNameLike,
                                request.FromUtc,
                                request.ToUtc,
                                request.PageSize,
                                cancellationToken).ConfigureAwait(false));
                        }

                        components = components
                            .GroupBy(c => c.Attributes.Id)
                            .Select(g => MergeComponentMetrics(g))
                            .ToList();
                    }

                    Log.Information("Retrieved {Count} components for {Collection}/{Type}", 
                        components.Count, displayCollection, componentType);

                    // Use the actual collection name from components or "All Collections" if querying all
                    var effectiveCollectionName = displayCollection;
                    MonitorReportMapper.AddComponentTree(report, effectiveCollectionName, components);

                    report.Collections.Add(new CollectionReportRow(
                        effectiveCollectionName,
                        componentType,
                        components.Count,
                        components.SelectMany(c => c.Metrics ?? []).Count(),
                        components.SelectMany(c => c.Metrics ?? []).SelectMany(m => m.Alerts ?? []).Count()));
                }
            }

            Log.Information("Applying metric filters...");
            ApplyMetricFilters(report, request);

            if (request.IncludeMetricTimeSeries && report.Metrics.Count > 0)
            {
                Log.Information("Fetching metric time series data...");

                var metricIds = report.Metrics
                    .Select(m => m.MetricId)
                    .Where(id => id > 0)
                    .Distinct()
                    .Take(request.MaxMetricIdsForTimeSeries ?? int.MaxValue)
                    .ToList();

                if (metricIds.Count > 0)
                {
                    Log.Debug("Requesting time series for {Count} metrics", metricIds.Count);

                    var series = await _queries.GetMetricTimeSeriesAsync(
                        metricIds,
                        request.FromUtc,
                        request.ToUtc,
                        request.MetricBucket,
                        cancellationToken).ConfigureAwait(false);

                    var dataPointCount = 0;
                    foreach (var metric in series.Features)
                    {
                        var metricAttributes = metric.Attributes;
                        foreach (var data in metric.MetricsData ?? [])
                        {
                            var d = data.Attributes;
                            report.MetricData.Add(new MetricDataReportRow
                            {
                                CollectionName = ResolveCollectionName(report, metricAttributes.Id),
                                MetricId = d.MetricId ?? metricAttributes.Id,
                                MetricName = metricAttributes.Name,
                                ComponentId = metricAttributes.ComponentId,
                                ComponentName = metricAttributes.ComponentName,
                                ObservedAt = d.ObservedAt,
                                MinValue = d.MinValue,
                                MaxValue = d.MaxValue,
                                AvgValue = d.AvgValue,
                                StdDevValue = d.StdDevValue,
                                Percentile95Value = d.Percentile95Value,
                                SumValue = d.SumValue,
                                CountValue = d.CountValue
                            });
                            dataPointCount++;
                        }
                    }

                    Log.Information("Retrieved {DataPoints} time series data points", dataPointCount);
                }
            }

            Log.Information("Report build completed: {Collections} collections, {Components} components, {Metrics} metrics, {Alerts} alerts, {DataPoints} data points",
                report.Collections.Count, report.Components.Count, report.Metrics.Count, report.Alerts.Count, report.MetricData.Count);

            return report;
        }

        /// <summary>
        /// Builds a report and saves it directly to an Excel file.
        /// </summary>
        /// <param name="request">The report request specifying collections, types, time range, and filters.</param>
        /// <param name="outputPath">Full path where the Excel file will be saved.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if request validation fails or <paramref name="outputPath"/> is invalid.</exception>
        /// <remarks>
        /// <para>
        /// This is a convenience method that combines <see cref="BuildReportAsync"/> and
        /// <see cref="MonitorExcelReportWriter.Save"/> in a single call.
        /// </para>
        /// <para>
        /// The method:
        /// <list type="number">
        /// <item><description>Builds the report using <see cref="BuildReportAsync"/></description></item>
        /// <item><description>Saves it to Excel using <see cref="MonitorExcelReportWriter.Save"/></description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The output directory will be created if it doesn't exist.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var request = new MonitorReportRequest
        /// {
        ///     CollectionNames = ["Production"],
        ///     ComponentTypes = ["host"],
        ///     FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
        ///     ToUtc = DateTimeOffset.UtcNow
        /// };
        /// 
        /// await reportService.BuildAndSaveExcelAsync(
        ///     request,
        ///     @"C:\Reports\monitor_report.xlsx");
        /// 
        /// Console.WriteLine("Report saved successfully!");
        /// </code>
        /// </example>
        public async Task BuildAndSaveExcelAsync(
            MonitorReportRequest request,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            var report = await BuildReportAsync(request, cancellationToken).ConfigureAwait(false);
            MonitorExcelReportWriter.Save(report, outputPath);
        }

        /// <summary>
        /// Applies metric filtering based on include/exclude lists and alerting status.
        /// </summary>
        /// <param name="report">The report to filter.</param>
        /// <param name="request">The request containing filter criteria.</param>
        /// <remarks>
        /// <para>
        /// This method filters the report data in place using the following criteria:
        /// <list type="bullet">
        /// <item><description><b>Include filter:</b> If <see cref="MonitorReportRequest.IncludeOnlyMetricNames"/> is specified, only metrics containing these patterns are kept</description></item>
        /// <item><description><b>Exclude filter:</b> Metrics containing any pattern from <see cref="MonitorReportRequest.ExcludeMetricNames"/> are removed</description></item>
        /// <item><description><b>Alerting filter:</b> If <see cref="MonitorReportRequest.AlertingOnOnly"/> is true, only metrics with alerting enabled are kept</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// After filtering metrics, the method also:
        /// <list type="bullet">
        /// <item><description>Removes metric data for excluded metrics</description></item>
        /// <item><description>Removes alerts for excluded metrics</description></item>
        /// <item><description>Updates metric and alert counts for each component</description></item>
        /// <item><description>Recalculates collection summaries</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Pattern matching is case-insensitive and uses substring matching (CONTAINS semantics).
        /// </para>
        /// </remarks>
        private static void ApplyMetricFilters(MonitorExcelReport report, MonitorReportRequest request)
        {
            var include = request.IncludeOnlyMetricNames
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var exclude = request.ExcludeMetricNames
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool KeepMetric(MetricReportRow metric)
            {
                var name = metric.MetricName ?? string.Empty;
                if (include.Count > 0 && !include.Any(i => name.Contains(i, StringComparison.OrdinalIgnoreCase)))
                    return false;
                if (exclude.Count > 0 && exclude.Any(e => name.Contains(e, StringComparison.OrdinalIgnoreCase)))
                    return false;
                if (request.AlertingOnOnly && metric.IsAlertingEnabled != true)
                    return false;
                return true;
            }

            var keptMetricIds = report.Metrics
                .Where(KeepMetric)
                .Select(m => m.MetricId)
                .ToHashSet();

            report.Metrics = report.Metrics
                .Where(m => keptMetricIds.Contains(m.MetricId))
                .ToList();

            report.MetricData = report.MetricData
                .Where(d => keptMetricIds.Contains(d.MetricId))
                .ToList();

            report.Alerts = report.Alerts
                .Where(a => a.MetricId.HasValue && keptMetricIds.Contains(a.MetricId.Value))
                .ToList();

            var metricsByComponent = report.Metrics
                .GroupBy(m => (m.CollectionName, m.ComponentId))
                .ToDictionary(g => g.Key, g => g.Count());
            var alertsByComponent = report.Alerts
                .Where(a => a.ComponentId.HasValue)
                .GroupBy(a => (a.CollectionName, ComponentId: a.ComponentId!.Value))
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var component in report.Components)
            {
                component.MetricCount = metricsByComponent.GetValueOrDefault((component.CollectionName, component.ComponentId));
                component.AlertCount = alertsByComponent.GetValueOrDefault((component.CollectionName, component.ComponentId));
            }

            report.Collections = report.Collections
                .Select(c =>
                {
                    var componentCount = report.Components.Count(x => string.Equals(x.CollectionName, c.CollectionName, StringComparison.OrdinalIgnoreCase)
                                                                 && string.Equals(x.Type, c.ComponentType, StringComparison.OrdinalIgnoreCase));
                    var metricCount = report.Metrics.Count(x => string.Equals(x.CollectionName, c.CollectionName, StringComparison.OrdinalIgnoreCase)
                                                            && string.Equals(x.ComponentType, c.ComponentType, StringComparison.OrdinalIgnoreCase));
                    var alertCount = report.Alerts.Count(x => string.Equals(x.CollectionName, c.CollectionName, StringComparison.OrdinalIgnoreCase)
                                                           && report.Components.Any(comp => comp.ComponentId == x.ComponentId
                                                                                       && string.Equals(comp.CollectionName, c.CollectionName, StringComparison.OrdinalIgnoreCase)
                                                                                       && string.Equals(comp.Type, c.ComponentType, StringComparison.OrdinalIgnoreCase)));
                    return c with { ComponentCount = componentCount, MetricCount = metricCount, AlertCount = alertCount };
                })
                .ToList();
        }

        /// <summary>
        /// Resolves the collection name for a given metric ID by searching the report's metrics.
        /// </summary>
        /// <param name="report">The report containing metrics.</param>
        /// <param name="metricId">The metric ID to look up.</param>
        /// <returns>The collection name if found; otherwise an empty string.</returns>
        /// <remarks>
        /// This helper method is used when processing time series data to associate
        /// each data point with the correct collection.
        /// </remarks>
        private static string ResolveCollectionName(MonitorExcelReport report, int metricId)
        {
            return report.Metrics.FirstOrDefault(m => m.MetricId == metricId)?.CollectionName ?? string.Empty;
        }

        /// <summary>
        /// Merges metrics from multiple component instances into a single component.
        /// </summary>
        /// <param name="group">A group of component features with the same component ID.</param>
        /// <returns>A single component feature with all metrics merged and deduplicated.</returns>
        /// <remarks>
        /// <para>
        /// This method is used when querying components by metric name patterns, which can
        /// return the same component multiple times (once for each matching metric).
        /// </para>
        /// <para>
        /// The method:
        /// <list type="number">
        /// <item><description>Takes the first component instance as the base</description></item>
        /// <item><description>Merges all metrics from all instances</description></item>
        /// <item><description>Deduplicates metrics by ID</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Component attributes (name, type, status, etc.) are taken from the first instance.
        /// Only metrics are merged across instances.
        /// </para>
        /// </remarks>
        private static ComponentFeature MergeComponentMetrics(IEnumerable<ComponentFeature> group)
        {
            var first = group.First();
            first.Metrics = group
                .SelectMany(c => c.Metrics ?? [])
                .GroupBy(m => m.Attributes.Id)
                .Select(g => g.First())
                .ToList();
            return first;
        }
    }
}
