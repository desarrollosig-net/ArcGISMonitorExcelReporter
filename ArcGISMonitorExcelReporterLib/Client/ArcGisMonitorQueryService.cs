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
        /// Counts the number of components matching the specified criteria.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to count components from all collections.</param>
        /// <param name="componentType">The type of components to count (e.g., "host", "service", "database").</param>
        /// <param name="fromUtc">Start date/time (UTC) for filtering time-based resources.</param>
        /// <param name="toUtc">End date/time (UTC) for filtering time-based resources.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The total count of components matching the criteria.</returns>
        /// <remarks>
        /// <para>
        /// This method performs an efficient count-only query without retrieving full component data.
        /// Use this to determine the total number of records before fetching them.
        /// </para>
        /// <para>
        /// The time range parameters affect child resources like logs, but the component count
        /// itself is not filtered by these dates.
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b> Pass <c>null</c>, empty string, or <c>"*"</c> as <paramref name="collectionName"/>
        /// to count components from all collections.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Count from specific collection
        /// var count = await service.CountComponentsAsync(
        ///     collectionName: "Production",
        ///     componentType: "host",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow);
        /// 
        /// // Count from all collections
        /// var allCount = await service.CountComponentsAsync(
        ///     collectionName: "*",  // or null or ""
        ///     componentType: "host",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow);
        /// 
        /// Console.WriteLine($"Total hosts: {count}");
        /// Console.WriteLine($"All hosts: {allCount}");
        /// </code>
        /// </example>
        public async Task<int> CountComponentsAsync(
            string collectionName,
            string componentType,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            var request = MonitorQueryBuilders.CollectionComponents(
                collectionName,
                componentType,
                returnCountOnly: true,
                fromUtc: fromUtc,
                toUtc: toUtc);

            var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
            return response.Features.FirstOrDefault()?.Components.Count ?? 0;
        }

        /// <summary>
        /// Retrieves all components of a specified type with automatic pagination.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all collections.</param>
        /// <param name="componentType">The type of components to retrieve (e.g., "host", "service", "database").</param>
        /// <param name="fromUtc">Start date/time (UTC) for filtering child resources like logs.</param>
        /// <param name="toUtc">End date/time (UTC) for filtering child resources like logs.</param>
        /// <param name="pageSize">Number of records per page. Default is 100.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A list containing all components matching the criteria.</returns>
        /// <remarks>
        /// <para>
        /// This method automatically handles pagination, first counting the total records
        /// and then fetching all pages until the complete result set is retrieved.
        /// </para>
        /// <para>
        /// Components include related resources: logs (within date range), labels, parents,
        /// agents, and the Metrics observer.
        /// </para>
        /// <para>
        /// For large result sets, consider using a larger <paramref name="pageSize"/>
        /// (e.g., 200-500) to reduce the number of HTTP requests.
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b> Pass <c>null</c>, empty string, or <c>"*"</c> as <paramref name="collectionName"/>
        /// to retrieve components from all collections.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query specific collection
        /// var components = await service.GetComponentsAsync(
        ///     collectionName: "Production",
        ///     componentType: "host",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     pageSize: 200);
        /// 
        /// // Query all collections
        /// var allComponents = await service.GetComponentsAsync(
        ///     collectionName: "*",  // or null or ""
        ///     componentType: "host",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     pageSize: 200);
        /// 
        /// foreach (var component in components)
        /// {
        ///     Console.WriteLine($"Component: {component.Attributes.Name}");
        /// }
        /// </code>
        /// </example>
        public async Task<List<ComponentFeature>> GetComponentsAsync(
            string collectionName,
            string componentType,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            var total = await CountComponentsAsync(collectionName, componentType, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
            var components = new List<ComponentFeature>(Math.Max(total, 0));

            for(var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
            {
                var request = MonitorQueryBuilders.CollectionComponents(
                    collectionName,
                    componentType,
                    returnCountOnly: false,
                    resultRecordCount: pageSize,
                    resultOffset: offset,
                    fromUtc: fromUtc,
                    toUtc: toUtc);

                var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
                components.AddRange(response.Features.SelectMany(f => f.Components.Items));

                if(total == 0)
                    break;
            }

            return components;
        }

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
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            Log.Debug("Getting component count for {Collection}/{Type}...", collectionName, componentType);

            var countRequest = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(collectionName, componentType, true, pageSize, 0);
            var countResponse = await _client.QueryCollectionsAsync(countRequest, cancellationToken).ConfigureAwait(false);
            var total = countResponse.Features.FirstOrDefault()?.Components.Count ?? 0;

            Log.Debug("Total components to retrieve: {Total}", total);

            var components = new List<ComponentFeature>(Math.Max(total, 0));

            for(var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
            {
                Log.Debug("Fetching components page: offset {Offset}, size {PageSize}", offset, pageSize);

                var request = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(collectionName, componentType, false, pageSize, offset);
                var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
                var pageCount = response.Features.SelectMany(f => f.Components.Items).Count();

                components.AddRange(response.Features.SelectMany(f => f.Components.Items));

                Log.Debug("Retrieved {Count} components in this page", pageCount);

                if(total == 0)
                    break;
            }

            Log.Debug("Completed fetching {Total} components with metrics", components.Count);

            return components;
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
            Log.Debug("Getting component count for {Collection}/{Type} with metric filter: {Metric}...",
                collectionName, componentType, metricNameLike);

            var countRequest = MonitorQueryBuilders.CollectionComponentsByMetricName(
                collectionName, componentType, metricNameLike, fromUtc, toUtc, true, pageSize, 0);

            var countResponse = await _client.QueryCollectionsAsync(countRequest, cancellationToken).ConfigureAwait(false);
            var total = countResponse.Features.FirstOrDefault()?.Components.Count ?? 0;

            Log.Debug("Total components to retrieve: {Total}", total);

            var components = new List<ComponentFeature>(Math.Max(total, 0));

            for(var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
            {
                Log.Debug("Fetching components page: offset {Offset}, size {PageSize}", offset, pageSize);

                var request = MonitorQueryBuilders.CollectionComponentsByMetricName(
                    collectionName, componentType, metricNameLike, fromUtc, toUtc, false, pageSize, offset);

                var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
                var pageCount = response.Features.SelectMany(f => f.Components.Items).Count();

                components.AddRange(response.Features.SelectMany(f => f.Components.Items));

                Log.Debug("Retrieved {Count} components in this page", pageCount);

                if(total == 0)
                    break;
            }

            Log.Debug("Completed fetching {Total} components with metric stats", components.Count);

            return components;
        }

        /// <summary>
        /// Retrieves time series data for multiple metrics with statistical aggregation over time buckets.
        /// </summary>
        /// <param name="metricIds">Collection of metric IDs to query.</param>
        /// <param name="fromUtc">Start date/time (UTC) for the time series data.</param>
        /// <param name="toUtc">End date/time (UTC) for the time series data.</param>
        /// <param name="bucket">Time bucket specification for grouping (e.g., "observed_at:15m" for 15-minute intervals). Default is "observed_at:15m".</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A query response containing metrics with time-bucketed statistics.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves time series data aggregated into time buckets, where each
        /// bucket contains statistical aggregations (count, min, max, avg, stddev, percentile_95, sum)
        /// for the specified time interval.
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
            IEnumerable<int> metricIds,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            string bucket = "observed_at:15m",
            CancellationToken cancellationToken = default)
        {
            var idList = metricIds.ToList();
            Log.Debug("Fetching time series for {Count} metrics with bucket: {Bucket}", idList.Count, bucket);

            var request = MonitorQueryBuilders.MetricsTimeSeries(idList, fromUtc, toUtc, bucket);
            var response = await _client.QueryMetricsAsync(request, cancellationToken).ConfigureAwait(false);

            Log.Debug("Retrieved time series data for {Count} metrics", response.Features.Count);

            return response;
        }
    }
}

