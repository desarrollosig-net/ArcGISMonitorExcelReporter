// Ignore Spelling: Gis

using System.Text.Json;

using ArcGISMonitorExcelReporterLib.Builders;
using ArcGISMonitorExcelReporterLib.Models;

using Serilog;

namespace ArcGISMonitorExcelReporterLib.Client
{
    /// <summary>
    /// High-level service for querying ArcGIS Monitor with automatic pagination and simplified API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service wraps <see cref="ArcGisMonitorClient"/> to provide a higher-level, more convenient
    /// API for common query scenarios. It handles:
    /// <list type="bullet">
    /// <item><description><b>Automatic pagination:</b> Transparently fetches all pages of results</description></item>
    /// <item><description><b>Count queries:</b> Efficiently counts records before fetching data</description></item>
    /// <item><description><b>Query building:</b> Uses <see cref="MonitorQueryBuilders"/> internally</description></item>
    /// <item><description><b>Logging:</b> Provides debug-level logging of query operations</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This service is the recommended way to query ArcGIS Monitor when you need complete
    /// result sets without manually handling pagination logic.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var client = new ArcGisMonitorClient(new Uri("https://monitor.example.com:30443/"));
    /// await client.AuthenticateAsync("username", "password");
    /// 
    /// var service = new ArcGisMonitorQueryService(client);
    /// 
    /// // Get all components with metrics
    /// var components = await service.GetComponentsWithAllMetricsAsync(
    ///     collectionName: "Production",
    ///     componentType: "host",
    ///     pageSize: 100);
    /// 
    /// Console.WriteLine($"Retrieved {components.Count} components");
    /// </code>
    /// </example>
    public sealed class ArcGisMonitorQueryService(ArcGisMonitorClient client)
    {
        private readonly ArcGisMonitorClient _client = client;

        /// <summary>
        /// Retrieves all components with their associated metrics using automatic pagination.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all collections.</param>
        /// <param name="componentType">The type of components to retrieve (e.g., "host", "service", "database").</param>
        /// <param name="pageSize">Number of records per page. Default is 100.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list containing all components with their nested metrics.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves components along with all their associated metric definitions.
        /// It does not include metric data points or statistics - only metric metadata
        /// (name, unit, thresholds, alerting configuration, etc.).
        /// </para>
        /// <para>
        /// The method:
        /// <list type="number">
        /// <item><description>Performs a count-only query to determine total records</description></item>
        /// <item><description>Fetches pages of components with metrics until all are retrieved</description></item>
        /// <item><description>Logs progress at debug level</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// For metric data with statistics, use <see cref="GetComponentsWithMetricStatsAsync"/> instead.
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b> Pass <c>null</c>, empty string, or <c>"*"</c> as <paramref name="collectionName"/>
        /// to retrieve components from all collections.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query specific collection
        /// var components = await service.GetComponentsWithAllMetricsAsync(
        ///     collectionName: "Production",
        ///     componentType: "host",
        ///     pageSize: 100);
        /// 
        /// // Query all collections
        /// var allComponents = await service.GetComponentsWithAllMetricsAsync(
        ///     collectionName: "*",  // or null or ""
        ///     componentType: "host",
        ///     pageSize: 100);
        /// 
        /// foreach (var component in components)
        /// {
        ///     Console.WriteLine($"Component: {component.Attributes.Name}");
        ///     
        ///     foreach (var metric in component.Metrics ?? [])
        ///     {
        ///         var m = metric.Attributes;
        ///         Console.WriteLine($"  - {m.Name}: {m.Unit}");
        ///         Console.WriteLine($"    Alerting: {m.IsAlertingEnabled}");
        ///         Console.WriteLine($"    Thresholds: W={m.WarningThreshold}, C={m.CriticalThreshold}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<List<ComponentFeature>> GetComponentsWithAllMetricsAsync(
            string collectionName,
            string componentType,
            string messageTemplate, int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            Log.Information("Getting component count for {Collection}/{Type}...", collectionName, componentType);

            var countRequest = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(collectionName, componentType, true, pageSize, 0);
            var countResponse = await _client.QueryCollectionsAsync(countRequest, cancellationToken).ConfigureAwait(false);
            var total = countResponse.Features.FirstOrDefault()?.Components.Count ?? 0;

            Log.Information("Total components to retrieve: {Total}", total);

            var components = new List<ComponentFeature>(Math.Max(total, 0));

            for(var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
            {
                Log.Information("Fetching components page: offset {Offset}, size {PageSize}", offset, pageSize);

                var request = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(collectionName, componentType, false, pageSize, offset);
                var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
                var pageCount = response.Features.SelectMany(f => f.Components.Items).Count();

                components.AddRange(response.Features.SelectMany(f => f.Components.Items));

                Log.Information(messageTemplate: messageTemplate, pageCount);

                if(total == 0)
                {
                    break;
                }
            }

            Log.Information("Completed fetching {Total} components with metrics", components.Count);

            return components;
        }

        /// <summary>
        /// Retrieves ALL components (without type filter) with their associated metrics, statistics, and alerts using automatic pagination.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all components across all collections.</param>
        /// <param name="fromUtc">Start date/time (UTC) for the statistics aggregation period and alert filtering.</param>
        /// <param name="toUtc">End date/time (UTC) for the statistics aggregation period and alert filtering.</param>
        /// <param name="pageSize">Number of records per page. Default is 100.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list containing all components (all types) with their nested metrics, aggregated statistics, and alerts.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves ALL components regardless of type, along with their metric definitions,
        /// aggregated statistics, and alerts. It does not filter by component type - use this when you want to:
        /// <list type="bullet">
        /// <item><description>Get all component types in a single query (more efficient than multiple calls)</description></item>
        /// <item><description>Filter by type locally after retrieval</description></item>
        /// <item><description>Query components across multiple or all collections</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Query Strategy:</b> This method uses two different query strategies depending on the collection parameter:
        /// </para>
        /// <para>
        /// <b>1. Direct Component Query</b> (when <paramref name="collectionName"/> is <c>null</c>, <c>""</c>, or <c>"*"</c>):
        /// <list type="bullet">
        /// <item><description>Uses <c>/monitoring/components/query</c> endpoint</description></item>
        /// <item><description>Filters by <c>state = 'monitored'</c></description></item>
        /// <item><description>More efficient for querying all components</description></item>
        /// <item><description>No deduplication needed (inherently unique)</description></item>
        /// <item><description>Returns components in their natural order</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>2. Collection-based Query</b> (when <paramref name="collectionName"/> is a specific collection name):
        /// <list type="bullet">
        /// <item><description>Uses <c>/monitoring/collections/query</c> endpoint</description></item>
        /// <item><description>Filters by collection name</description></item>
        /// <item><description>Retrieves all component types within the collection</description></item>
        /// <item><description>Returns components grouped by collection</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Both strategies:
        /// <list type="number">
        /// <item><description>Perform a count-only query to determine total records</description></item>
        /// <item><description>Fetch pages of components with metrics, statistics, and alerts until all are retrieved</description></item>
        /// <item><description>Log progress at debug level</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Performance consideration:</b> This may return a large dataset. Consider using
        /// a larger <paramref name="pageSize"/> (200-500) to reduce HTTP requests.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query all components from ALL collections (uses direct component query)
        /// var allComponents = await service.GetAllComponentsWithMetricsAsync(
        ///     collectionName: "*",  // or null or ""
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     pageSize: 200);
        /// 
        /// // Query all component types from a specific collection (uses collection query)
        /// var productionComponents = await service.GetAllComponentsWithMetricsAsync(
        ///     collectionName: "Production",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     pageSize: 200);
        /// 
        /// // Filter locally by type after retrieval
        /// var hosts = allComponents
        ///     .Where(c => c.Attributes.Type == "host")
        ///     .ToList();
        /// 
        /// var services = allComponents
        ///     .Where(c => c.Attributes.Type == "service")
        ///     .ToList();
        /// 
        /// var databases = allComponents
        ///     .Where(c => c.Attributes.Type == "database")
        ///     .ToList();
        /// 
        /// Console.WriteLine($"Retrieved {allComponents.Count} total components");
        /// Console.WriteLine($"Hosts: {hosts.Count}, Services: {services.Count}, Databases: {databases.Count}");
        /// 
        /// // Access metrics with statistics for each component
        /// foreach (var component in allComponents)
        /// {
        ///     Console.WriteLine($"{component.Attributes.Name} ({component.Attributes.Type})");
        ///     Console.WriteLine($"  Metrics: {component.Metrics?.Count ?? 0}");
        ///     
        ///     foreach (var metric in component.Metrics ?? [])
        ///     {
        ///         var stats = metric.MetricsData?.FirstOrDefault()?.Attributes;
        ///         if (stats != null)
        ///         {
        ///             Console.WriteLine($"    {metric.Attributes.Name}:");
        ///             Console.WriteLine($"      Avg: {stats.AvgValue:F2}, Max: {stats.MaxValue:F2}");
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<List<ComponentFeature>> GetAllComponentsWithMetricsAsync(
            string collectionName,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            // When querying all collections, use direct component query instead
            var queryComponents = string.IsNullOrEmpty(collectionName) || collectionName == "*";

            if(queryComponents)
            {
                Log.Information("Getting component count...");

                // Perform direct component query with state='monitored' filter
                var countRequest = MonitorQueryBuilders.AllComponentsWithMetrics(
                    where: "state = 'monitored'",
                    fromUtc: fromUtc,
                    toUtc: toUtc,
                    returnCountOnly: true,
                    resultRecordCount: pageSize,
                    resultOffset: 0);

                var countResponse = await _client.QueryComponentsAsync(
                    countRequest,
                    cancellationToken).ConfigureAwait(false);
                var total = countResponse.Count;

                Log.Information("Total components to retrieve : {Total}", total);

                var components = new List<ComponentFeature>(Math.Max(total, 0));

                for(var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
                {
                    Log.Information("Fetching components page: offset {Offset}, size {PageSize}", offset, pageSize);

                    var request = MonitorQueryBuilders.AllComponentsWithMetrics(
                        where: "state = 'monitored'",
                        fromUtc: fromUtc,
                        toUtc: toUtc,
                        returnCountOnly: false,
                        resultRecordCount: pageSize,
                        resultOffset: offset);

                    var response = await _client.QueryComponentsAsync(request, cancellationToken).ConfigureAwait(false);
                    components.AddRange(response.Features);

                    Log.Information("Retrieved {Count} components in this page", response.Features.Count);

                    if(total == 0)
                    {
                        break;
                    }
                }

                Log.Information("Completed fetching {Total} components", components.Count);
                return components;
            }
            else
            {
                // Query specific collection logic
                Log.Information("Getting component count for {Collection}...", collectionName);

                var countRequest = MonitorQueryBuilders.CollectionAllComponentsWithMetrics(
                    collectionName: collectionName,
                    fromUtc: fromUtc,
                    toUtc: toUtc,
                    returnCountOnly: true,
                    resultRecordCount: pageSize,
                    resultOffset: 0);
                var countResponse = await _client.QueryCollectionsAsync(
                    countRequest,
                    cancellationToken).ConfigureAwait(false);
                var total = countResponse.Features.Sum(f => f.Components?.Count ?? 0);

                Log.Information("Total components to retrieve : {Total}", total);

                var components = new List<ComponentFeature>(Math.Max(total, 0));

                for(var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
                {
                    Log.Information("Fetching components page : offset {Offset}, size {PageSize}", offset, pageSize);

                    var request = MonitorQueryBuilders.CollectionAllComponentsWithMetrics(
                        collectionName,
                        fromUtc,
                        toUtc,
                        false,
                        pageSize,
                        offset);

                    var response = await _client.QueryCollectionsAsync(
                        request,
                        cancellationToken).ConfigureAwait(false);
                    var pageCount = response.Features.SelectMany(f => f.Components.Items).Count();
                    components.AddRange(response.Features.SelectMany(f => f.Components.Items));

                    Log.Information("Retrieved {Count} components in this page", pageCount);

                    if(total == 0)
                    {
                        break;
                    }
                }

                Log.Information("Completed fetching {Total} components", components.Count);

                return components;
            }
        }

        /// <summary>
        /// Retrieves components filtered by metric name with aggregated statistics and alerts.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all collections.</param>
        /// <param name="componentType">The type of components to retrieve (e.g., "host", "service", "database").</param>
        /// <param name="metricNameLike">Metric name pattern for LIKE matching (e.g., "CPU" matches "CPU Utilized", "CPU %", etc.).</param>
        /// <param name="fromUtc">Start date/time (UTC) for the statistics aggregation period.</param>
        /// <param name="toUtc">End date/time (UTC) for the statistics aggregation period.</param>
        /// <param name="pageSize">Number of records per page. Default is 100.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list of components with matching metrics, including aggregated statistics and alerts.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves a rich dataset for each component:
        /// <list type="bullet">
        /// <item><description><b>Component metadata:</b> Name, type, labels, observer</description></item>
        /// <item><description><b>Matching metrics:</b> Only metrics where name starts with <paramref name="metricNameLike"/></description></item>
        /// <item><description><b>Aggregated statistics:</b> count, min, max, avg, stddev, percentile_95, sum for the time period</description></item>
        /// <item><description><b>Alerts:</b> All alerts that overlap with the specified time range</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The statistics are calculated server-side by ArcGIS Monitor using OutStatistics,
        /// providing a single aggregated value per metric for the entire period.
        /// </para>
        /// <para>
        /// <b>Pattern matching:</b> The <paramref name="metricNameLike"/> parameter uses SQL LIKE
        /// with an implicit wildcard at the end. For example, "CPU" will match:
        /// <list type="bullet">
        /// <item><description>"CPU Utilized"</description></item>
        /// <item><description>"CPU %"</description></item>
        /// <item><description>"CPU - System"</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b> Pass <c>null</c>, empty string, or <c>"*"</c> as <paramref name="collectionName"/>
        /// to retrieve components from all collections.
        /// </para>
        /// <para>
        /// This method handles pagination automatically and logs progress at debug level.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query specific collection
        /// var components = await service.GetComponentsWithMetricStatsAsync(
        ///     collectionName: "Production",
        ///     componentType: "host",
        ///     metricNameLike: "CPU",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     pageSize: 100);
        /// 
        /// // Query all collections
        /// var allComponents = await service.GetComponentsWithMetricStatsAsync(
        ///     collectionName: "*",  // or null or ""
        ///     componentType: "host",
        ///     metricNameLike: "CPU",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     pageSize: 100);
        /// 
        /// foreach (var component in components)
        /// {
        ///     Console.WriteLine($"Component: {component.Attributes.Name}");
        ///     
        ///     foreach (var metric in component.Metrics ?? [])
        ///     {
        ///         var m = metric.Attributes;
        ///         var stats = metric.MetricsData?.FirstOrDefault()?.Attributes;
        ///         
        ///         if (stats != null)
        ///         {
        ///             Console.WriteLine($"  Metric: {m.Name}");
        ///             Console.WriteLine($"    Avg: {stats.AvgValue:F2}");
        ///             Console.WriteLine($"    Max: {stats.MaxValue:F2}");
        ///             Console.WriteLine($"    Min: {stats.MinValue:F2}");
        ///             Console.WriteLine($"    P95: {stats.Percentile95Value:F2}");
        ///             Console.WriteLine($"    Count: {stats.CountValue}");
        ///         }
        ///         
        ///         foreach (var alert in metric.Alerts ?? [])
        ///         {
        ///             var a = alert.Attributes;
        ///             Console.WriteLine($"    Alert: {a.State} - Opened: {a.OpenedAt}");
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<List<ComponentFeature>> GetComponentsWithMetricStatsAsync(
            string collectionName,
            string componentType,
            string metricNameLike,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            Log.Information("Getting component count for {Collection}/{Type} with metric filter: {Metric}...",
                collectionName, componentType, metricNameLike);

            var countRequest = MonitorQueryBuilders.CollectionComponentsByMetricName(
                collectionName, componentType, metricNameLike, fromUtc, toUtc, true, pageSize, 0);

            var countResponse = await _client.QueryCollectionsAsync(countRequest, cancellationToken).ConfigureAwait(false);
            var total = countResponse.Features.FirstOrDefault()?.Components.Count ?? 0;

            Log.Information("Total components to retrieve: {Total}", total);

            var components = new List<ComponentFeature>(Math.Max(total, 0));

            for(var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
            {
                Log.Information("Fetching components page: offset {Offset}, size {PageSize}", offset, pageSize);

                var request = MonitorQueryBuilders.CollectionComponentsByMetricName(
                    collectionName, componentType, metricNameLike, fromUtc, toUtc, false, pageSize, offset);

                var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
                var pageCount = response.Features.SelectMany(f => f.Components.Items).Count();

                components.AddRange(response.Features.SelectMany(f => f.Components.Items));

                Log.Information("Retrieved {Count} components in this page", pageCount);

                if(total == 0)
                {
                    break;
                }
            }

            Log.Information("Completed fetching {Total} components with metric stats", components.Count);

            return components;
        }

        /// <summary>
        /// Retrieves time series data for multiple metrics with statistical aggregation over time buckets.
        /// </summary>
        /// <param name="metricIds">Collection of metric IDs to query.</param>
        /// <param name="fromUtc">Start date/time (UTC) for the time series data.</param>
        /// <param name="toUtc">End date/time (UTC) for the time series data.</param>
        /// <param name="bucket">Time bucket specification for grouping (e.g., "observed_at:15m" for 15-minute intervals). Default is "observed_at:15m".</param>
        /// <param name="batchSize">Number of metric IDs to process per batch. Default is 100.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A query response containing metrics with time-bucketed statistics.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves time series data aggregated into time buckets, where each
        /// bucket contains statistical aggregations (count, min, max, avg, stddev, percentile_95, sum)
        /// for the specified time interval.
        /// </para>
        /// <para>
        /// <b>Batch processing:</b> The method automatically splits large sets of metric IDs into
        /// batches of <paramref name="batchSize"/> to avoid overwhelming the API and stay within
        /// request size limits. Results from all batches are merged into a single response.
        /// </para>
        /// <para>
        /// <b>Bucket format:</b> "field:interval" where interval examples include:
        /// <list type="bullet">
        /// <item><description>"observed_at:5m" - 5-minute buckets</description></item>
        /// <item><description>"observed_at:15m" - 15-minute buckets (default)</description></item>
        /// <item><description>"observed_at:1h" - 1-hour buckets</description></item>
        /// <item><description>"observed_at:1d" - 1-day buckets</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The method logs the request and response details at debug level, including
        /// the number of metrics requested and the number of features retrieved.
        /// </para>
        /// <para>
        /// This is ideal for creating time series charts, analyzing metric trends over time,
        /// or exporting historical metric data for reporting.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var metricIds = new[] { 101, 102, 103 };
        /// 
        /// var response = await service.GetMetricTimeSeriesAsync(
        ///     metricIds: metricIds,
        ///     fromUtc: DateTimeOffset.UtcNow.AddHours(-24),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     bucket: "observed_at:1h");
        /// 
        /// foreach (var metric in response.Features)
        /// {
        ///     var m = metric.Attributes;
        ///     Console.WriteLine($"Metric: {m.Name} (Component: {m.ComponentName})");
        ///     Console.WriteLine($"Time series points: {metric.MetricsData?.Count ?? 0}");
        ///     
        ///     foreach (var dataPoint in metric.MetricsData ?? [])
        ///     {
        ///         var d = dataPoint.Attributes;
        ///         Console.WriteLine($"  {d.ObservedAt:yyyy-MM-dd HH:mm}: " +
        ///                         $"Avg={d.AvgValue:F2}, Max={d.MaxValue:F2}, Count={d.CountValue}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<QueryResponse<MetricFeature>> GetMetricTimeSeriesAsync(
            IEnumerable<long> metricIds,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            string bucket = "observed_at:15m",
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var idList = metricIds.ToList();
            Log.Information("Fetching time series for {Count} metrics with bucket: {Bucket}, batch size: {BatchSize}",
                idList.Count, bucket, batchSize);

            if(idList.Count == 0)
            {
                Log.Information("No metric IDs provided for time series query");
                return new QueryResponse<MetricFeature>
                {
                    Features = [],
                    Count = 0
                };
            }

            var allFeatures = new List<MetricFeature>();
            var totalBatches = (int)Math.Ceiling(idList.Count / (double)batchSize);

            for(var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var skip = batchIndex * batchSize;
                var batchIds = idList.Skip(skip).Take(batchSize).ToList();

                Log.Information("Processing batch {Current}/{Total}: {Count} metric IDs (starting at index {Skip})",
                    batchIndex + 1, totalBatches, batchIds.Count, skip);

                var request = MonitorQueryBuilders.MetricsTimeSeries(batchIds, fromUtc, toUtc, bucket);
                var response = await _client.QueryMetricsAsync(request, cancellationToken).ConfigureAwait(false);

                allFeatures.AddRange(response.Features);

                Log.Information("Retrieved {Count} metric features from batch {Current}/{Total}",
                    response.Features.Count, batchIndex + 1, totalBatches);
            }

            Log.Information("Completed fetching time series data for {Total} metrics across {Batches} batch(es). Total features: {Features}",
                idList.Count, totalBatches, allFeatures.Count);

            return new QueryResponse<MetricFeature>
            {
                Features = allFeatures,
                Count = allFeatures.Count
            };
        }

        /// <summary>
        /// Retrieves monitoring information from the ArcGIS Monitor /monitoring endpoint.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A MonitoringInfo object containing version and available resources.</returns>
        /// <remarks>
        /// This method queries the /monitoring endpoint to retrieve system information
        /// about ArcGIS Monitor, including version details and available API resources.
        /// Requires bearer token authentication.
        /// </remarks>
        /// <example>
        /// <code>
        /// var service = new ArcGisMonitorQueryService(client);
        /// var info = await service.GetMonitoringInfoAsync();
        /// Console.WriteLine($"ArcGIS Monitor Version: {info.Version}");
        /// </code>
        /// </example>
        public async Task<MonitoringInfo?> GetMonitoringInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Log.Information("Fetching monitoring information from /monitoring endpoint");
                var info = await _client.GetAsync<MonitoringInfo>("monitoring", requiresBearer: true, cancellationToken).ConfigureAwait(false);

                if(info != null && !string.IsNullOrEmpty(info.Version))
                {
                    Log.Information("Successfully retrieved monitoring information. Version: {Version}, Resources: {ResourceCount}", 
                        info.Version, info.Resources?.Count ?? 0);
                }

                return info;
            }
            catch(Exception ex)
            {
                Log.Information(ex, "Error retrieving monitoring information from /monitoring endpoint");
                return null;
            }
        }

        /// <summary>
        /// Retrieves field information for a specific resource from the /monitoring/{resource} endpoint.
        /// </summary>
        /// <param name="resourceName">The name of the resource (e.g., "metrics", "alerts", "components").</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A ResourceFieldInfo object containing field definitions for the resource, or null if retrieval fails.</returns>
        /// <remarks>
        /// This method queries the /monitoring/{resource} endpoint to retrieve schema information
        /// about a specific resource, including available fields and their definitions.
        /// Requires bearer token authentication.
        /// </remarks>
        /// <example>
        /// <code>
        /// var service = new ArcGisMonitorQueryService(client);
        /// var metricsFields = await service.GetResourceFieldsAsync("metrics");
        /// Console.WriteLine($"Metrics resource has {metricsFields?.Fields?.Count ?? 0} fields");
        /// </code>
        /// </example>
        public async Task<ResourceFieldInfo?> GetResourceFieldsAsync(
            string resourceName,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceName);

            try
            {
                Log.Debug("Fetching field information for resource: {ResourceName}", resourceName);
                var endpoint = $"monitoring/{resourceName}";
                var fields = await _client.GetAsync<ResourceFieldInfo>(endpoint, requiresBearer: true, cancellationToken).ConfigureAwait(false);

                if(fields != null && fields.Fields?.Count > 0)
                {
                    Log.Debug("Successfully retrieved field information for {ResourceName}. Fields: {FieldCount}", 
                        resourceName, fields.Fields.Count);
                }
                else
                {
                    Log.Debug("No field information found for resource: {ResourceName}", resourceName);
                }

                return fields;
            }
            catch(Exception ex)
            {
                Log.Debug(ex, "Error retrieving field information for resource: {ResourceName}", resourceName);
                return null;
            }
        }

        /// <summary>
        /// Retrieves component types information from the /monitoring/components endpoint.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>Component types information containing available component types and their fields.</returns>
        /// <remarks>
        /// This endpoint provides metadata about component types (e.g., "host", "database", "service", "storage")
        /// and the field names available for each type. This information is useful for displaying component-specific
        /// fields in the report and validating component queries.
        /// </remarks>
        public async Task<ComponentTypesInfo?> GetComponentTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var componentTypes = await _client.GetAsync<ComponentTypesInfo>("monitoring/components", requiresBearer: true, cancellationToken).ConfigureAwait(false);

                if (componentTypes?.Types?.Count > 0)
                {
                    Log.Information("Retrieved {Count} component types: {Types}",
                        componentTypes.Types.Count,
                        string.Join(", ", componentTypes.Types.Select(t => t.Name)));
                }
                else
                {
                    Log.Information("No component types found in monitoring/components endpoint");
                }

                return componentTypes;
            }
            catch(Exception ex)
            {
                Log.Information(ex, "Error retrieving component types from /monitoring/components");
                return null;
            }
        }

        /// <summary>
        /// Retrieves field information for multiple resources in parallel.
        /// </summary>
        /// <param name="resourceNames">The collection of resource names to query.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A dictionary mapping resource names to their field information.</returns>
        /// <remarks>
        /// This method efficiently retrieves field information for multiple resources by executing
        /// requests in parallel. It gracefully handles partial failures - if a resource fails to
        /// retrieve, that resource is simply omitted from the result.
        /// </remarks>
        public async Task<Dictionary<string, ResourceFieldInfo>> GetAllResourceFieldsAsync(
            IEnumerable<string> resourceNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceNames);

            var tasks = resourceNames
                .Select(resource => GetResourceFieldsAsync(resource, cancellationToken))
                .ToList();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var fieldsDictionary = new Dictionary<string, ResourceFieldInfo>();
            var resourceList = resourceNames.ToList();

            for(var i = 0; i < resourceList.Count && i < results.Length; i++)
            {
                if(results[i] != null)
                {
                    fieldsDictionary[resourceList[i]] = results[i] ?? new ResourceFieldInfo();
                }
            }

            Log.Debug("Retrieved field information for {SuccessCount} out of {TotalCount} resources",
                fieldsDictionary.Count, resourceList.Count);

            return fieldsDictionary;
        }

        /// <summary>
        /// Retrieves all agents using automatic pagination.
        /// </summary>
        /// <param name="pageSize">Number of records per page. Default is 100.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list containing all agents.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves agents from the /monitoring/agents/query endpoint using POST.
        /// Agents are software components that collect monitoring data from monitored machines.
        /// </para>
        /// <para>
        /// The method:
        /// <list type="number">
        /// <item><description>Performs a count-only query to determine total records</description></item>
        /// <item><description>Fetches pages of agents until all are retrieved</description></item>
        /// <item><description>Logs progress at debug level</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public async Task<List<AttributeFeature<AgentAttributes>>> GetAgentsAsync(
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            Log.Debug("Getting agent count...");

            var countRequest = new QueryRequest
            {
                ReturnCountOnly = true,
                ResultRecordCount = pageSize,
                ResultOffset = 0
            };

            var countResponse = await _client.QueryAgentsAsync(countRequest, cancellationToken).ConfigureAwait(false);

            var total = countResponse?.Features?.Count ?? 0;

            Log.Debug("Total agents to retrieve: {Total}", total);

            var agents = new List<AttributeFeature<AgentAttributes>>(Math.Max(total, 0));

            for (var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
            {
                Log.Debug("Fetching agents page: offset {Offset}, size {PageSize}", offset, pageSize);

                var request = new QueryRequest
                {
                    ReturnCountOnly = false,
                    ResultRecordCount = pageSize,
                    ResultOffset = offset
                };

                var response = await _client.QueryAgentsAsync(request, cancellationToken).ConfigureAwait(false);

                var pageCount = response?.Features?.Count ?? 0;

                if (response?.Features != null)
                {
                    agents.AddRange(response.Features);
                }

                Log.Debug("Retrieved {PageCount} agents", pageCount);

                if (total == 0)
                {
                    break;
                }
            }

            Log.Debug("Completed fetching {Total} agents", agents.Count);

            return agents;
        }

        /// <summary>
        /// Retrieves all labels using automatic pagination.
        /// </summary>
        /// <param name="pageSize">Number of records per page. Default is 100.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list containing all labels.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves labels from the /monitoring/labels/query endpoint using POST.
        /// Labels are tags that can be assigned to components and other resources for categorization.
        /// </para>
        /// <para>
        /// The method:
        /// <list type="number">
        /// <item><description>Performs a count-only query to determine total records</description></item>
        /// <item><description>Fetches pages of labels until all are retrieved</description></item>
        /// <item><description>Logs progress at debug level</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public async Task<List<AttributeFeature<LabelAttributes>>> GetLabelsAsync(
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            Log.Debug("Getting label count...");

            var countRequest = new QueryRequest
            {
                ReturnCountOnly = true,
                ResultRecordCount = pageSize,
                ResultOffset = 0
            };

            var countResponse = await _client.QueryLabelsAsync(countRequest, cancellationToken).ConfigureAwait(false);

            var total = countResponse?.Features?.Count ?? 0;

            Log.Debug("Total labels to retrieve: {Total}", total);

            var labels = new List<AttributeFeature<LabelAttributes>>(Math.Max(total, 0));

            for (var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
            {
                Log.Debug("Fetching labels page: offset {Offset}, size {PageSize}", offset, pageSize);

                var request = new QueryRequest
                {
                    ReturnCountOnly = false,
                    ResultRecordCount = pageSize,
                    ResultOffset = offset
                };

                var response = await _client.QueryLabelsAsync(request, cancellationToken).ConfigureAwait(false);

                var pageCount = response?.Features?.Count ?? 0;

                if (response?.Features != null)
                {
                    labels.AddRange(response.Features);
                }

                Log.Debug("Retrieved {PageCount} labels", pageCount);

                if (total == 0)
                {
                    break;
                }
            }

            Log.Debug("Completed fetching {Total} labels", labels.Count);

            return labels;
        }
    }
}

