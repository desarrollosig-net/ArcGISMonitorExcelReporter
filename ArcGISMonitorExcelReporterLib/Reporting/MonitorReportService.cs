using ArcGISMonitorExcelReporterLib.Client;
using ArcGISMonitorExcelReporterLib.Models;

using Serilog;

using System.Linq;

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
    /// <remarks>
    /// Initializes a new instance of the <see cref="MonitorReportService"/> class.
    /// </remarks>
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
    public sealed class MonitorReportService(ArcGisMonitorQueryService queries)
    {
        private readonly ArcGisMonitorQueryService _queries = queries;

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

            ValidateRequest(request);

            var (collectionsToQuery, typesToFilter, isAllTypes) = NormalizeRequestParameters(request);

            var report = new MonitorExcelReport
            {
                ServerUrl = request.ServerUrl,
                CollectionName = request.CollectionNames.Count == 1 &&
                                 !string.IsNullOrWhiteSpace(request.CollectionNames[0]) &&
                                 request.CollectionNames[0].Trim() != "*"
                    ? request.CollectionNames[0]
                    : null,
                Timezone = request.Timezone,
                PastDays = request.PastDays,
                PastHours = request.PastHours,
                FromUtc = request.FromUtc,
                ToUtc = request.ToUtc
            };

            foreach(var collectionName in collectionsToQuery)
            {
                await ProcessCollectionAsync(report, collectionName, typesToFilter, isAllTypes, request, cancellationToken).ConfigureAwait(false);
            }

            Log.Information("Applying metric filters...");
            ApplyMetricFilters(report, request);

            if(request.IncludeMetricTimeSeries && report.Metrics.Count > 0)
            {
                await FetchMetricTimeSeriesAsync(report, request, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            Log.Information("Report build completed: {Collections} collections, {Components} components, {Metrics} metrics, {Alerts} alerts, {DataPoints} data points",
                report.Collections.Count, report.Components.Count, report.Metrics.Count, report.Alerts.Count, report.MetricData.Count);

            return report;
        }

        /// <summary>
        /// Validates the report request parameters.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        /// <remarks>
        /// Validates that collections, component types, and date range are properly specified.
        /// </remarks>
        private static void ValidateRequest(MonitorReportRequest request)
        {
            var isAllCollections = request.CollectionNames.Count == 0 ||
                                  (request.CollectionNames.Count == 1 &&
                                   (string.IsNullOrWhiteSpace(request.CollectionNames[0]) ||
                                    request.CollectionNames[0].Trim() == "*"));

            if(!isAllCollections && request.CollectionNames.Count == 0)
            {
                throw new ArgumentException("Must specify at least one collection or use \"*\" for all collections.", nameof(request));
            }

            var isAllTypes = request.ComponentTypes.Count == 0 ||
                            (request.ComponentTypes.Count == 1 &&
                             (string.IsNullOrWhiteSpace(request.ComponentTypes[0]) ||
                              request.ComponentTypes[0].Trim() == "*"));

            if(!isAllTypes && request.ComponentTypes.Count == 0)
            {
                throw new ArgumentException("Must specify at least one component type or use \"*\" for all types.", nameof(request));
            }

            if(request.FromUtc >= request.ToUtc)
            {
                throw new ArgumentException("FromUtc must be less than ToUtc.", nameof(request));
            }
        }

        /// <summary>
        /// Normalizes request parameters for collection names and component types.
        /// </summary>
        /// <param name="request">The request containing parameters to normalize.</param>
        /// <returns>A tuple containing normalized collections to query, types to filter, and whether all types are requested.</returns>
        /// <remarks>
        /// Converts wildcard patterns and empty lists into normalized lists for processing.
        /// Empty or "*" values are interpreted as "all items".
        /// </remarks>
        private static (List<string> collectionsToQuery, List<string> typesToFilter, bool isAllTypes) NormalizeRequestParameters(MonitorReportRequest request)
        {
            var isAllCollections = request.CollectionNames.Count == 0 ||
                                  (request.CollectionNames.Count == 1 &&
                                   (string.IsNullOrWhiteSpace(request.CollectionNames[0]) ||
                                    request.CollectionNames[0].Trim() == "*"));

            var isAllTypes = request.ComponentTypes.Count == 0 ||
                            (request.ComponentTypes.Count == 1 &&
                             (string.IsNullOrWhiteSpace(request.ComponentTypes[0]) ||
                              request.ComponentTypes[0].Trim() == "*"));

            var collectionsToQuery = isAllCollections
                ? ["*"]
                : request.CollectionNames.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var typesToFilter = isAllTypes
                ? []
                : request.ComponentTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return (collectionsToQuery, typesToFilter, isAllTypes);
        }

        /// <summary>
        /// Processes a single collection, fetching components and adding them to the report.
        /// </summary>
        /// <param name="report">The report to populate.</param>
        /// <param name="collectionName">The collection name to process.</param>
        /// <param name="typesToFilter">The list of component types to filter by.</param>
        /// <param name="isAllTypes">Whether all types should be included.</param>
        /// <param name="request">The original report request.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <remarks>
        /// This method orchestrates fetching, filtering, and processing components for a single collection.
        /// </remarks>
        private async Task ProcessCollectionAsync(
            MonitorExcelReport report,
            string collectionName,
            List<string> typesToFilter,
            bool isAllTypes,
            MonitorReportRequest request,
            CancellationToken cancellationToken)
        {
            LogCollectionQuery(collectionName);

            var allComponents = await FetchComponentsForCollectionAsync(collectionName, request, cancellationToken).ConfigureAwait(false);

            Log.Information("Retrieved {Count} total components for {Collection}", allComponents.Count, collectionName);

            allComponents = FilterComponentsByType(allComponents, typesToFilter, isAllTypes);

            ProcessComponentsByType(report, collectionName, allComponents);
        }

        /// <summary>
        /// Logs the collection query operation.
        /// </summary>
        /// <param name="collectionName">The collection name being queried.</param>
        /// <remarks>
        /// Logs different messages depending on whether all collections or a specific collection is being queried.
        /// </remarks>
        private static void LogCollectionQuery(string collectionName)
        {
            if(collectionName == "*" || string.IsNullOrWhiteSpace(collectionName))
            {
                Log.Information("Fetching all configured components");
            }
            else
            {
                Log.Information("Querying collection: {Collection}, fetching all configured components", collectionName);
            }
        }

        /// <summary>
        /// Fetches all components for a specific collection.
        /// </summary>
        /// <param name="collectionName">The collection name to query.</param>
        /// <param name="request">The report request containing query parameters.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list of component features with their metrics.</returns>
        /// <remarks>
        /// If metric name patterns are specified, fetches only matching metrics and merges duplicate components.
        /// Otherwise, fetches all components with all metrics.
        /// </remarks>
        private async Task<List<ComponentFeature>> FetchComponentsForCollectionAsync(
            string collectionName,
            MonitorReportRequest request,
            CancellationToken cancellationToken)
        {
            var allComponents = new List<ComponentFeature>();

            if(request.MetricNameLikes.Count == 0)
            {
                allComponents.AddRange(await FetchAllComponentsWithMetricsAsync(collectionName, request, cancellationToken).ConfigureAwait(false));
            }
            else
            {
                allComponents.AddRange(await FetchComponentsWithSpecificMetricsAsync(collectionName, request, cancellationToken).ConfigureAwait(false));
                allComponents = [.. allComponents
                    .GroupBy(c => c.Attributes.Id)
                    .Select(MergeComponentMetrics)];
            }

            return allComponents;
        }

        /// <summary>
        /// Fetches all components with all their metrics for a collection.
        /// </summary>
        /// <param name="collectionName">The collection name to query.</param>
        /// <param name="request">The report request containing query parameters including date range.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list of all components with their metrics.</returns>
        /// <remarks>
        /// This method is used when no specific metric filters are applied.
        /// </remarks>
        private async Task<List<ComponentFeature>> FetchAllComponentsWithMetricsAsync(
            string collectionName,
            MonitorReportRequest request,
            CancellationToken cancellationToken)
        {
            if(collectionName == "*" || string.IsNullOrWhiteSpace(collectionName))
            {
                Log.Debug("Fetching all components with all metrics");
            }
            else
            {
                Log.Debug("Fetching all components with all metrics for {Collection}", collectionName);
            }

            return await _queries.GetAllComponentsWithMetricsAsync(
                collectionName,
                request.FromUtc,
                request.ToUtc,
                request.PageSize,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches components that have specific metrics matching the provided patterns.
        /// </summary>
        /// <param name="collectionName">The collection name to query.</param>
        /// <param name="request">The report request containing metric name patterns.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list of components that have metrics matching the specified patterns.</returns>
        /// <remarks>
        /// This method queries the API once for each metric pattern and returns all matching components.
        /// Components may appear multiple times if they have multiple matching metrics.
        /// </remarks>
        private async Task<List<ComponentFeature>> FetchComponentsWithSpecificMetricsAsync(
            string collectionName,
            MonitorReportRequest request,
            CancellationToken cancellationToken)
        {
            Log.Debug("Fetching components with specific metrics: {Metrics}", string.Join(", ", request.MetricNameLikes));

            var components = new List<ComponentFeature>();
            foreach(var metricNameLike in request.MetricNameLikes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                components.AddRange(await _queries.GetComponentsWithMetricStatsAsync(
                    collectionName,
                    "*",
                    metricNameLike,
                    request.FromUtc,
                    request.ToUtc,
                    request.PageSize,
                    cancellationToken).ConfigureAwait(false));
            }

            return components;
        }

        /// <summary>
        /// Filters components by their type based on the requested types.
        /// </summary>
        /// <param name="components">The list of components to filter.</param>
        /// <param name="typesToFilter">The list of component types to keep.</param>
        /// <param name="isAllTypes">Whether all types should be included (no filtering).</param>
        /// <returns>A filtered list of components matching the requested types.</returns>
        /// <remarks>
        /// If <paramref name="isAllTypes"/> is true, no filtering is performed.
        /// Otherwise, only components with types in <paramref name="typesToFilter"/> are kept.
        /// </remarks>
        private static List<ComponentFeature> FilterComponentsByType(
            List<ComponentFeature> components,
            List<string> typesToFilter,
            bool isAllTypes)
        {
            if(!isAllTypes && typesToFilter.Count > 0)
            {
                var originalCount = components.Count;
                components = [.. components.Where(c => typesToFilter.Contains(c.Attributes.Type, StringComparer.OrdinalIgnoreCase))];

                Log.Information("Filtered to {Count} components matching requested types (excluded {Excluded})",
                    components.Count, originalCount - components.Count);
            }

            return components;
        }

        /// <summary>
        /// Processes components grouped by type and adds them to the report.
        /// </summary>
        /// <param name="report">The report to populate.</param>
        /// <param name="collectionName">The collection name the components belong to.</param>
        /// <param name="allComponents">The list of components to process.</param>
        /// <remarks>
        /// Groups components by type, adds them to the report using the mapper,
        /// and creates collection summary rows.
        /// </remarks>
        private static void ProcessComponentsByType(
            MonitorExcelReport report,
            string collectionName,
            List<ComponentFeature> allComponents)
        {
            var componentsByType = allComponents
                .GroupBy(c => c.Attributes.Type)
                .OrderBy(g => g.Key);

            foreach(var typeGroup in componentsByType)
            {
                var componentType = typeGroup.Key ?? string.Empty;
                var components = typeGroup.ToList();

                Log.Debug("Processing {Count} components of type {Type}", components.Count, componentType);

                MonitorReportMapper.AddComponentTree(report, collectionName, components);

                report.Collections.Add(new CollectionReportRow(
                    collectionName,
                    ComponentType: componentType,
                    components.Count,
                    components.SelectMany(c => c.Metrics ?? []).Count(),
                    components.SelectMany(c => c.Metrics ?? []).SelectMany(m => m.Alerts ?? []).Count()));
            }
        }

        /// <summary>
        /// Fetches time series data for metrics in the report.
        /// </summary>
        /// <param name="report">The report containing metrics to fetch time series for.</param>
        /// <param name="request">The report request with time range and bucket parameters.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <remarks>
        /// Fetches time-bucketed metric data for up to <see cref="MonitorReportRequest.MaxMetricIdsForTimeSeries"/> metrics.
        /// The fetched data is added to the report's MetricData collection.
        /// </remarks>
        private async Task FetchMetricTimeSeriesAsync(
            MonitorExcelReport report,
            MonitorReportRequest request,
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            Log.Information("Fetching metric time series data...");

            var metricIds = report.Metrics
                .Select(m => m.MetricId)
                .Where(id => id > 0)
                .Distinct()
                .Take(request.MaxMetricIdsForTimeSeries ?? int.MaxValue)
                .ToList();

            if(metricIds.Count == 0)
            {
                return;
            }

            Log.Debug("Requesting time series for {Count} metrics", metricIds.Count);

            const string bucket = "observed_at:15m";

            var series = await _queries.GetMetricTimeSeriesAsync(
                metricIds,
                request.FromUtc,
                request.ToUtc,
                bucket,
                batchSize,
                cancellationToken).ConfigureAwait(false);

            var rawRows = ProcessMetricTimeSeries(report, series);
            Log.Debug("Raw time series: {RawCount} data points before downsampling", rawRows.Count);

            var (downsampledRows, effectiveBucket) = DownsampleMetricTimeSeries(rawRows);
            report.MetricDataBucket = effectiveBucket;
            report.TimeSeriesMetricData.AddRange(downsampledRows);

            Log.Information("Retrieved {DataPoints} time series data points (downsampled from {RawCount}, bucket: {Bucket})",
                downsampledRows.Count, rawRows.Count, effectiveBucket);
        }

        /// <summary>
        /// Processes metric time series data and adds it to the report.
        /// </summary>
        /// <param name="report">The report used to resolve collection names.</param>
        /// <param name="series">The time series data returned from the API.</param>
        /// <returns>The list of time series data points parsed from the API response.</returns>
        /// <remarks>
        /// Extracts time series data points from the API response and creates MetricDataReportRow entries.
        /// The returned list contains only the new time series rows and must be added to the report separately.
        /// </remarks>
        private static List<MetricDataReportRow> ProcessMetricTimeSeries(MonitorExcelReport report, dynamic series)
        {
            var rows = new List<MetricDataReportRow>();
            foreach(var metric in series.Features)
            {
                var metricAttributes = metric.Attributes;
                var metricsData = metric.MetricsData ?? Enumerable.Empty<dynamic>();

                foreach(var data in metricsData)
                {
                    var d = data.Attributes;
                    rows.Add(new MetricDataReportRow
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
                        Percentile95Value = CalculateNormalP95(d.AvgValue, d.StdDevValue, d.MaxValue) ?? d.Percentile95Value,
                        SumValue = d.SumValue,
                        CountValue = d.CountValue
                    });
                }
            }

            return rows;
        }

        /// <summary>
        /// Downsamples the metric time series data in the report to approximately
        /// <paramref name="targetDataPoints"/> points per metric series by re-aggregating
        /// raw 15-minute buckets into coarser intervals.
        /// </summary>
        /// <param name="timeSeries">The raw time series rows to downsample (must not contain aggregated stats from the mapper).</param>
        /// <param name="targetDataPoints">Desired maximum number of data points per series. Default is 400.</param>
        /// <remarks>
        /// Selects the finest interval from the progression 15m → 30m → 1h → 6h → 12h → 24h
        /// such that the resulting point count does not exceed <paramref name="targetDataPoints"/>.
        /// Series that already have fewer points than the target are left unchanged.
        /// Aggregation preserves min, max, weighted average, combined standard deviation,
        /// recalculated P95, sum, and count.
        /// </remarks>
        /// <returns>A tuple with the downsampled rows and the bucket label effectively applied (e.g. "15m", "1h").</returns>
        private static (List<MetricDataReportRow> Rows, string Bucket) DownsampleMetricTimeSeries(
            List<MetricDataReportRow> timeSeries,
            int targetDataPoints = 400)
        {
            (int Minutes, string Label)[] buckets =
            [
                (15,   "15m"),
                (30,   "30m"),
                (60,   "1h"),
                (120,  "2h"),
                (240,  "4h"),
                (360,  "6h"),
                (720,  "12h"),
                (1440, "24h")
            ];

            var effectiveBucket = buckets[0].Label;

            if(timeSeries.Count == 0)
            {
                return (timeSeries, effectiveBucket);
            }

            var result = new List<MetricDataReportRow>(timeSeries.Count);

            foreach(var series in timeSeries.Where(d => d.ObservedAt.HasValue).GroupBy(d => d.MetricId))
            {
                var points = series.OrderBy(d => d.ObservedAt).ToList();

                if(points.Count <= targetDataPoints)
                {
                    result.AddRange(points);
                    continue;
                }

                var durationMinutes = (points[^1].ObservedAt!.Value - points[0].ObservedAt!.Value).TotalMinutes;
                var idealBucketMinutes = durationMinutes / targetDataPoints;

                var bucketMinutes = buckets[^1].Minutes;
                var bucketLabel = buckets[^1].Label;
                foreach(var (minutes, label) in buckets)
                {
                    if(minutes >= idealBucketMinutes)
                    {
                        bucketMinutes = minutes;
                        bucketLabel = label;
                        break;
                    }
                }

                // Track the coarsest bucket applied across all series
                if(Array.FindIndex(buckets, b => b.Label == bucketLabel) >
                   Array.FindIndex(buckets, b => b.Label == effectiveBucket))
                {
                    effectiveBucket = bucketLabel;
                }

                result.AddRange(points
                    .GroupBy(d => TruncateToBucket(d.ObservedAt!.Value, bucketMinutes))
                    .Select(g => AggregateBucket(g.Key, g.ToList()))
                    .OrderBy(d => d.ObservedAt));
            }

            // Pass through points without a timestamp unchanged
            result.AddRange(timeSeries.Where(d => !d.ObservedAt.HasValue));

            return (result, effectiveBucket);
        }

        /// <summary>
        /// Truncates a timestamp to the nearest lower boundary of the given bucket interval.
        /// </summary>
        private static DateTimeOffset TruncateToBucket(DateTimeOffset dt, int bucketMinutes)
        {
            var totalMinutes = (long)(dt.UtcDateTime - DateTime.UnixEpoch).TotalMinutes;
            return DateTimeOffset.UnixEpoch.AddMinutes((totalMinutes / bucketMinutes) * bucketMinutes);
        }

        /// <summary>
        /// Aggregates a list of data points within a single time bucket into one representative row.
        /// </summary>
        /// <remarks>
        /// Uses weighted average for <see cref="MetricDataReportRow.AvgValue"/> and the parallel
        /// variance formula for <see cref="MetricDataReportRow.StdDevValue"/>.
        /// <see cref="MetricDataReportRow.Percentile95Value"/> is recalculated from the combined statistics.
        /// </remarks>
        private static MetricDataReportRow AggregateBucket(DateTimeOffset bucketTime, List<MetricDataReportRow> points)
        {
            var first = points[0];

            double? avgValue = null;
            double? stdDevValue = null;
            double? countValue = null;
            double? sumValue = null;

            var countedPoints = points.Where(p => p.AvgValue.HasValue && p.CountValue is > 0).ToList();
            if(countedPoints.Count > 0)
            {
                var totalCount = countedPoints.Sum(p => p.CountValue!.Value);
                avgValue = countedPoints.Sum(p => p.AvgValue!.Value * p.CountValue!.Value) / totalCount;

                // Parallel variance: Σ count_i × (σ_i² + (μ_i − μ)²) / totalCount
                var combinedVariance = countedPoints.Sum(p =>
                {
                    var variance = p.StdDevValue.HasValue ? p.StdDevValue.Value * p.StdDevValue.Value : 0.0;
                    var meanDiff = p.AvgValue!.Value - avgValue.Value;
                    return p.CountValue!.Value * (variance + meanDiff * meanDiff);
                }) / totalCount;

                stdDevValue = Math.Sqrt(combinedVariance);
                countValue = totalCount;
            }
            else
            {
                // Fallback: simple average when count is not available
                var validAvg = points.Where(p => p.AvgValue.HasValue).ToList();
                if(validAvg.Count > 0)
                {
                    avgValue = validAvg.Average(p => p.AvgValue!.Value);
                }
                countValue = points.Sum(p => p.CountValue);
            }

            if(points.Any(p => p.SumValue.HasValue))
            {
                sumValue = points.Sum(p => p.SumValue ?? 0.0);
            }

            var maxValue = points.Any(p => p.MaxValue.HasValue) ? points.Max(p => p.MaxValue) : null;
            var minValue = points.Any(p => p.MinValue.HasValue) ? points.Min(p => p.MinValue) : null;

            return new MetricDataReportRow
            {
                CollectionName = first.CollectionName,
                MetricId = first.MetricId,
                MetricName = first.MetricName,
                ComponentId = first.ComponentId,
                ComponentName = first.ComponentName,
                ObservedAt = bucketTime,
                MinValue = minValue,
                MaxValue = maxValue,
                AvgValue = avgValue,
                StdDevValue = stdDevValue,
                Percentile95Value = CalculateNormalP95(avgValue, stdDevValue, maxValue),
                SumValue = sumValue,
                CountValue = countValue
            };
        }
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
                return (include.Count <= 0 || include.Any(i => name.Contains(i, StringComparison.OrdinalIgnoreCase))) && (exclude.Count <= 0 || !exclude.Any(e => name.Contains(e, StringComparison.OrdinalIgnoreCase))) && (!request.AlertingOnOnly || metric.IsAlertingEnabled == true);
            }

            var keptMetricIds = report.Metrics
                .Where(KeepMetric)
                .Select(m => m.MetricId)
                .ToHashSet();

            report.Metrics = [.. report.Metrics.Where(m => keptMetricIds.Contains(m.MetricId))];

            report.MetricData = [.. report.MetricData.Where(d => keptMetricIds.Contains(d.MetricId))];
            report.TimeSeriesMetricData = [.. report.TimeSeriesMetricData.Where(d => keptMetricIds.Contains(d.MetricId))];

            report.Alerts = [.. report.Alerts.Where(a => a.MetricId.HasValue && keptMetricIds.Contains(a.MetricId.Value))];

            var metricsByComponent = report.Metrics
                .GroupBy(m => (m.CollectionName, m.ComponentId))
                .ToDictionary(g => g.Key, g => g.Count());
            var alertsByComponent = report.Alerts
                .Where(a => a.ComponentId.HasValue)
                .GroupBy(a => (a.CollectionName, ComponentId: a.ComponentId!.Value))
                .ToDictionary(g => g.Key, g => g.Count());

            foreach(var component in report.Components)
            {
                component.MetricCount = metricsByComponent.GetValueOrDefault((component.CollectionName, component.ComponentId));
                component.AlertCount = alertsByComponent.GetValueOrDefault((component.CollectionName, component.ComponentId));
            }

            report.Collections = [.. report.Collections
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
                })];
        }

        ///
        /// Resolves the collection name for a given metric ID by searching the report's metrics.
        /// </summary>
        /// <param name="report">The report containing metrics.</param>
        /// <param name="metricId">The metric ID to look up.</param>
        /// <returns>The collection name if found; otherwise an empty string.</returns>
        /// <remarks>
        /// This helper method is used when processing time series data to associate
        /// each data point with the correct collection.
        /// </remarks>
        private static string ResolveCollectionName(MonitorExcelReport report, int metricId) => report.Metrics.FirstOrDefault(m => m.MetricId == metricId)?.CollectionName ?? string.Empty;

        /// <summary>
        /// Calculates the 95th percentile (P95) of a normal distribution using exact statistics, constrained by the maximum observed value.
        /// </summary>
        /// <param name="mean">The mean (average) of the distribution.</param>
        /// <param name="stdDev">The standard deviation of the distribution.</param>
        /// <param name="maxValue">The maximum observed value (P95 will not exceed this).</param>
        /// <returns>The 95th percentile value, or null if inputs are invalid.</returns>
        /// <remarks>
        /// <para>
        /// This method calculates the exact P95 value using the normal distribution formula:
        /// <c>P95 = min(μ + z₀.₉₅ × σ, max)</c>
        /// </para>
        /// <para>
        /// Where:
        /// <list type="bullet">
        /// <item><description>μ (mu) = mean</description></item>
        /// <item><description>σ (sigma) = standard deviation</description></item>
        /// <item><description>z₀.₉₅ = 1.6448536269514722 (exact z-score for 95th percentile)</description></item>
        /// <item><description>max = maximum observed value</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The calculated P95 is constrained to never exceed the maximum observed value,
        /// ensuring statistical consistency with the actual data distribution. This is important because:
        /// <list type="bullet">
        /// <item><description>Real data may not perfectly follow a normal distribution</description></item>
        /// <item><description>Small sample sizes can lead to theoretical estimates exceeding observed maxima</description></item>
        /// <item><description>The 95th percentile cannot logically exceed the maximum observed value</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Returns <c>null</c> if:
        /// <list type="bullet">
        /// <item><description>Mean is null or NaN</description></item>
        /// <item><description>Standard deviation is null, NaN, or negative</description></item>
        /// <item><description>Maximum value is null or NaN</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// double? mean = 100.0;
        /// double? stdDev = 15.0;
        /// double? max = 120.0;
        /// double? p95 = CalculateNormalP95(mean, stdDev, max);
        /// // Result: 120.0 (capped at max, since 100 + 1.645*15 = 124.67 > 120)
        /// </code>
        /// </example>
        private static double? CalculateNormalP95(double? mean, double? stdDev, double? maxValue)
        {
            // Z-score for 95th percentile in a standard normal distribution
            const double Z_95 = 1.6448536269514722;

            if(!mean.HasValue || double.IsNaN(mean.Value))
            {
                return null;
            }

            if(!stdDev.HasValue || double.IsNaN(stdDev.Value) || stdDev.Value < 0)
            {
                return null;
            }

            if(!maxValue.HasValue || double.IsNaN(maxValue.Value))
            {
                return null;
            }

            // P95 = μ + z₀.₉₅ × σ
            var theoreticalP95 = mean.Value + (Z_95 * stdDev.Value);

            // Ensure P95 does not exceed the maximum observed value
            return Math.Min(theoreticalP95, maxValue.Value);
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
            first.Metrics = [.. group
                .SelectMany(c => c.Metrics ?? [])
                .GroupBy(m => m.Attributes.Id)
                .Select(g => g.First())];
            return first;
        }
    }
}
