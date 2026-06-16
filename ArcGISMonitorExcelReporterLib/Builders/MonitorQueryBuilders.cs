using ArcGISMonitorExcelReporterLib.Models;

namespace ArcGISMonitorExcelReporterLib.Builders
{
    /// <summary>
    /// Static factory class for building ArcGIS Monitor API query requests.
    /// Provides fluent methods to construct complex queries with proper filtering, pagination, and nested resource inclusion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This builder class encapsulates the complexity of creating properly formatted query requests
    /// for the ArcGIS Monitor REST API, including:
    /// <list type="bullet">
    /// <item><description>SQL-like WHERE clauses with proper escaping</description></item>
    /// <item><description>Timestamp filtering for time-based queries</description></item>
    /// <item><description>Nested resource inclusion (components → metrics → metrics_data)</description></item>
    /// <item><description>Statistical aggregations using OutStatistics</description></item>
    /// <item><description>Time-bucketed grouping for time series data</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// All methods handle SQL injection prevention through proper escaping and use
    /// the ISO 8601 timestamp format required by ArcGIS Monitor.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Query components with all metrics:
    /// </para>
    /// <code>
    /// var request = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(
    ///     collectionName: "Production",
    ///     componentType: "host",
    ///     returnCountOnly: false,
    ///     resultRecordCount: 100);
    /// </code>
    /// <para>
    /// Query metrics with statistics:
    /// </para>
    /// <code>
    /// var request = MonitorQueryBuilders.CollectionComponentsByMetricName(
    ///     collectionName: "Production",
    ///     componentType: "host",
    ///     metricNameLike: "CPU",
    ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
    ///     toUtc: DateTimeOffset.UtcNow,
    ///     returnCountOnly: false);
    /// </code>
    /// </example>
    public static class MonitorQueryBuilders
    {
        private const string ComponentsResource = "components";
        private const string MetricsResource = "metrics";

        /// <summary>
        /// Builds a query request for collection components with optional related resources.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all collections.</param>
        /// <param name="componentType">The type of components to filter (e.g., "host", "service", "database").</param>
        /// <param name="returnCountOnly">If <c>true</c>, returns only the count; otherwise returns full component data.</param>
        /// <param name="resultRecordCount">Maximum number of records to return (pagination size). Default is 100.</param>
        /// <param name="resultOffset">Offset for pagination. Default is 0.</param>
        /// <param name="fromUtc">Optional start date/time (UTC) for filtering time-based child resources like logs.</param>
        /// <param name="toUtc">Optional end date/time (UTC) for filtering time-based child resources like logs.</param>
        /// <param name="includeLogs">If <c>true</c>, includes component logs (requires <paramref name="fromUtc"/> and <paramref name="toUtc"/>). Default is <c>true</c>.</param>
        /// <param name="includeLabels">If <c>true</c>, includes component labels. Default is <c>true</c>.</param>
        /// <param name="includeParents">If <c>true</c>, includes parent components. Default is <c>true</c>.</param>
        /// <param name="includeAgents">If <c>true</c>, includes associated agents. Default is <c>true</c>.</param>
        /// <param name="includeMetricsObserver">If <c>true</c>, includes the Metrics observer. Default is <c>true</c>.</param>
        /// <returns>A configured <see cref="CollectionQueryRequest"/> ready to send to the API.</returns>
        /// <remarks>
        /// <para>
        /// This method is useful for retrieving basic component information along with related
        /// metadata such as labels, parents, agents, and logs.
        /// </para>
        /// <para>
        /// Note that logs are only included if both <paramref name="fromUtc"/> and <paramref name="toUtc"/>
        /// are provided and <paramref name="includeLogs"/> is <c>true</c>.
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b> Pass <c>null</c>, empty string, or <c>"*"</c> as <paramref name="collectionName"/>
        /// to query components from all collections without filtering by collection name.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query specific collection
        /// var request = MonitorQueryBuilders.CollectionComponents(
        ///     collectionName: "Production",
        ///     componentType: "host",
        ///     returnCountOnly: false,
        ///     resultRecordCount: 50,
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     includeLogs: true);
        /// 
        /// // Query all collections
        /// var allCollectionsRequest = MonitorQueryBuilders.CollectionComponents(
        ///     collectionName: "*",  // or null or ""
        ///     componentType: "host",
        ///     returnCountOnly: false);
        /// </code>
        /// </example>
        public static CollectionQueryRequest CollectionComponents(
            string collectionName,
            string componentType,
            bool returnCountOnly,
            int resultRecordCount = 100,
            int resultOffset = 0,
            DateTimeOffset? fromUtc = null,
            DateTimeOffset? toUtc = null,
            bool includeLogs = true,
            bool includeLabels = true,
            bool includeParents = true,
            bool includeAgents = true,
            bool includeMetricsObserver = true)
        {
            var childIncludes = new List<CollectionIncludeSpec>();

            if(includeLogs && fromUtc.HasValue && toUtc.HasValue)
            {
                childIncludes.Add(new CollectionIncludeSpec
                {
                    Resource = "components_logs",
                    Where = BetweenTimestamp("logged_at", fromUtc.Value, toUtc.Value)
                });
            }

            if(includeLabels)
            {
                childIncludes.Add(new CollectionIncludeSpec { Resource = "labels" });
            }

            if(includeParents)
            {
                childIncludes.Add(new CollectionIncludeSpec { Resource = "parents" });
            }

            if(includeAgents)
            {
                childIncludes.Add(new CollectionIncludeSpec { Resource = "agents" });
            }

            if(includeMetricsObserver)
            {
                childIncludes.Add(new CollectionIncludeSpec { Resource = "observers", Where = "name='Metrics'" });
            }

            return CollectionRequest(collectionName, new CollectionIncludeSpec
            {
                Resource = ComponentsResource,
                ReturnCountOnly = returnCountOnly,
                ResultRecordCount = resultRecordCount,
                ResultOffset = resultOffset,
                Where = $"type = '{EscapeSqlLiteral(componentType)}'",
                Including = childIncludes
            });
        }

        /// <summary>
        /// Builds a query request for collection components including all their metrics.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all collections.</param>
        /// <param name="componentType">The type of components to filter (e.g., "host", "service", "database").</param>
        /// <param name="returnCountOnly">If <c>true</c>, returns only the count; otherwise returns full data with metrics.</param>
        /// <param name="resultRecordCount">Maximum number of component records to return. Default is 100.</param>
        /// <param name="resultOffset">Offset for pagination. Default is 0.</param>
        /// <returns>A configured <see cref="CollectionQueryRequest"/> with nested metrics inclusion.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves components along with all their associated metrics,
        /// but does not include metric data points or statistics. Use this when you need
        /// metric definitions without time series data.
        /// </para>
        /// <para>
        /// For metric data with statistics, use <see cref="CollectionComponentsByMetricName"/> instead.
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b> Pass <c>null</c>, empty string, or <c>"*"</c> as <paramref name="collectionName"/>
        /// to query components from all collections without filtering by collection name.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query specific collection
        /// var request = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(
        ///     collectionName: "Production",
        ///     componentType: "host",
        ///     returnCountOnly: false);
        /// 
        /// // Query all collections
        /// var allRequest = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(
        ///     collectionName: "*",  // or null or ""
        ///     componentType: "host",
        ///     returnCountOnly: false);
        /// 
        /// var response = await client.QueryCollectionsAsync(request);
        /// foreach (var component in response.Features.SelectMany(f => f.Components.Items))
        /// {
        ///     Console.WriteLine($"Component: {component.Attributes.Name}");
        ///     foreach (var metric in component.Metrics ?? [])
        ///     {
        ///         Console.WriteLine($"  - Metric: {metric.Attributes.Name}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static CollectionQueryRequest CollectionComponentsWithAllMetrics(
            string collectionName,
            string componentType,
            bool returnCountOnly,
            int resultRecordCount = 100,
            int resultOffset = 0) => CollectionRequest(collectionName, new CollectionIncludeSpec
            {
                Resource = ComponentsResource,
                ReturnCountOnly = returnCountOnly,
                ResultRecordCount = resultRecordCount,
                ResultOffset = resultOffset,
                Where = $"type = '{EscapeSqlLiteral(componentType)}'",
                Including = [new CollectionIncludeSpec { Resource = MetricsResource }]
            });

        /// <summary>
        /// Builds a query request for ALL collection components (without type filter) including metrics with aggregated statistics and alerts.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all collections.</param>
        /// <param name="fromUtc">Start date/time (UTC) for the statistics aggregation period and alert filtering.</param>
        /// <param name="toUtc">End date/time (UTC) for the statistics aggregation period and alert filtering.</param>
        /// <param name="returnCountOnly">If <c>true</c>, returns only the count; otherwise returns full data with metrics, statistics, and alerts.</param>
        /// <param name="resultRecordCount">Maximum number of component records to return. Default is 100.</param>
        /// <param name="resultOffset">Offset for pagination. Default is 0.</param>
        /// <returns>A configured <see cref="CollectionQueryRequest"/> with nested metrics, aggregated statistics, and alerts, no type filter applied.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves ALL components (regardless of type) along with their metrics,
        /// aggregated statistics, and alerts. Use this when you want to query all component types
        /// in a single request and filter locally.
        /// </para>
        /// <para>
        /// The query includes:
        /// <list type="bullet">
        /// <item><description>All components from the collection (no type filter)</description></item>
        /// <item><description>All metrics for each component</description></item>
        /// <item><description>Aggregated statistics for metric data in the specified time range (count, min, max, avg, stddev, sum)</description></item>
        /// <item><description>Alerts that overlap with the specified time range</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Performance consideration:</b> This query may return a large dataset when used with
        /// all collections (<c>"*"</c>). Consider using appropriate pagination and filtering locally
        /// by component type after retrieval.
        /// </para>
        /// <para>
        /// <b>Use cases:</b>
        /// <list type="bullet">
        /// <item><description>When you need multiple component types from the same collection with statistics</description></item>
        /// <item><description>When you want to filter by types locally instead of multiple API calls</description></item>
        /// <item><description>When you need to deduplicate across all types</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query all component types from specific collection
        /// var request = MonitorQueryBuilders.CollectionAllComponentsWithMetrics(
        ///     collectionName: "Production",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     returnCountOnly: false,
        ///     resultRecordCount: 200);
        /// 
        /// // Query all components from all collections
        /// var allRequest = MonitorQueryBuilders.CollectionAllComponentsWithMetrics(
        ///     collectionName: "*",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     returnCountOnly: false);
        /// 
        /// // Then filter locally by type
        /// var response = await client.QueryCollectionsAsync(request);
        /// var hosts = response.Features
        ///     .SelectMany(f => f.Components.Items)
        ///     .Where(c => c.Attributes.Type == "host")
        ///     .ToList();
        /// 
        /// foreach (var component in hosts)
        /// {
        ///     foreach (var metric in component.Metrics ?? [])
        ///     {
        ///         var stats = metric.MetricsData?.FirstOrDefault()?.Attributes;
        ///         if (stats != null)
        ///         {
        ///             Console.WriteLine($"Component: {component.Attributes.Name}");
        ///             Console.WriteLine($"  Metric: {metric.Attributes.Name}");
        ///             Console.WriteLine($"    Avg: {stats.AvgValue}, Max: {stats.MaxValue}");
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public static CollectionQueryRequest CollectionAllComponentsWithMetrics(
            string collectionName,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            bool returnCountOnly = false,
            int resultRecordCount = 100,
            int resultOffset = 0) => CollectionRequest(collectionName, new CollectionIncludeSpec
            {
                Resource = ComponentsResource,
                ReturnCountOnly = returnCountOnly,
                ResultRecordCount = resultRecordCount,
                ResultOffset = resultOffset,
                Where = null, // No type filter - get ALL components
                Including =
                [
                    new CollectionIncludeSpec
                    {
                        Resource = "metrics",
                        Including =
                        [
                            new CollectionIncludeSpec
                            {
                                Resource = "metrics_data",
                                Where = BetweenTimestamp("observed_at", fromUtc, toUtc),
                                GroupByFieldsForStatistics = "metric_id",
                                OutStatistics =
                                [
                                    new OutStatistic
                                    {
                                        OnStatisticField = "value",
                                        StatisticType = ["count", "avg", "min", "max", "sum", "stddev"]
                                    }
                                ]
                            },
                            new CollectionIncludeSpec
                            {
                                Resource = "alerts",
                                Where = AlertOverlapsWhere(fromUtc, toUtc)
                            }
                        ]
                    }
                ]
            });

        /// <summary>
        /// Builds a query request for components filtered by metric name, including aggregated statistics and alerts.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. Use <c>null</c>, <c>""</c>, or <c>"*"</c> to query all collections.</param>
        /// <param name="componentType">The type of components to filter (e.g., "host", "service", "database").</param>
        /// <param name="metricNameLike">Metric name pattern for LIKE matching (e.g., "CPU" will match "CPU Utilized", "CPU %", etc.).</param>
        /// <param name="fromUtc">Start date/time (UTC) for the statistics aggregation period.</param>
        /// <param name="toUtc">End date/time (UTC) for the statistics aggregation period.</param>
        /// <param name="returnCountOnly">If <c>true</c>, returns only the count; otherwise returns full data with statistics.</param>
        /// <param name="resultRecordCount">Maximum number of component records to return. Default is 100.</param>
        /// <param name="resultOffset">Offset for pagination. Default is 0.</param>
        /// <returns>A configured <see cref="CollectionQueryRequest"/> with metrics, aggregated statistics, and alerts.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a complex query that retrieves:
        /// <list type="number">
        /// <item><description>Components of the specified type</description></item>
        /// <item><description>Metrics matching the name pattern</description></item>
        /// <item><description>Aggregated statistics for metric data in the specified time range (count, min, max, avg, stddev, percentile_95, sum)</description></item>
        /// <item><description>Alerts that overlap with the specified time range</description></item>
        /// <item><description>Component labels and Metrics observer</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The statistics are calculated server-side using OutStatistics aggregation,
        /// providing a single aggregated value per metric for the entire time period.
        /// </para>
        /// <para>
        /// The <paramref name="metricNameLike"/> parameter uses SQL LIKE matching with an implicit
        /// wildcard at the end (e.g., "CPU" becomes "name like 'CPU%'").
        /// </para>
        /// <para>
        /// <b>Collection filtering:</b> Pass <c>null</c>, empty string, or <c>"*"</c> as <paramref name="collectionName"/>
        /// to query components from all collections without filtering by collection name.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Query specific collection
        /// var request = MonitorQueryBuilders.CollectionComponentsByMetricName(
        ///     collectionName: "Production",
        ///     componentType: "host",
        ///     metricNameLike: "CPU",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     returnCountOnly: false);
        /// 
        /// // Query all collections
        /// var allRequest = MonitorQueryBuilders.CollectionComponentsByMetricName(
        ///     collectionName: "*",  // or null or ""
        ///     componentType: "host",
        ///     metricNameLike: "CPU",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     returnCountOnly: false);
        /// 
        /// var response = await client.QueryCollectionsAsync(request);
        /// foreach (var component in response.Features.SelectMany(f => f.Components.Items))
        /// {
        ///     foreach (var metric in component.Metrics ?? [])
        ///     {
        ///         var stats = metric.MetricsData?.FirstOrDefault()?.Attributes;
        ///         if (stats != null)
        ///         {
        ///             Console.WriteLine($"Metric: {metric.Attributes.Name}");
        ///             Console.WriteLine($"  Avg: {stats.AvgValue}, Max: {stats.MaxValue}");
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public static CollectionQueryRequest CollectionComponentsByMetricName(
            string collectionName,
            string componentType,
            string metricNameLike,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            bool returnCountOnly,
            int resultRecordCount = 100,
            int resultOffset = 0) => CollectionRequest(collectionName, new CollectionIncludeSpec
            {
                Resource = ComponentsResource,
                ReturnCountOnly = returnCountOnly,
                ResultRecordCount = resultRecordCount,
                ResultOffset = resultOffset,
                Where = $"type = '{EscapeSqlLiteral(componentType)}'",
                Including =
                [
                    new CollectionIncludeSpec
                    {
                        Resource = MetricsResource,
                        Where = $"name like '{EscapeSqlLiteral(metricNameLike)}%'",
                        Including =
                        [
                            new CollectionIncludeSpec
                            {
                                Resource = "metrics_data",
                                Where = BetweenTimestamp("observed_at", fromUtc, toUtc),
                                GroupByFieldsForStatistics = "metric_id",
                                OutStatistics =
                                [
                                    new OutStatistic
                                    {
                                        OnStatisticField = "value",
                                        StatisticType = ["count", "min", "max", "avg", "stddev", "sum"]
                                    }
                                ]
                            },
                            new CollectionIncludeSpec
                            {
                                Resource = "alerts",
                                Where = AlertOverlapsWhere(fromUtc, toUtc)
                            }
                        ]
                    },
                    new CollectionIncludeSpec { Resource = "labels" },
                    new CollectionIncludeSpec { Resource = "observers", Where = "name='Metrics'" }
                ]
            });

        /// <summary>
        /// Builds a query request for metric time series data with statistical aggregation over time buckets.
        /// </summary>
        /// <param name="metricIds">Collection of metric IDs to query.</param>
        /// <param name="fromUtc">Start date/time (UTC) for the time series data.</param>
        /// <param name="toUtc">End date/time (UTC) for the time series data.</param>
        /// <param name="bucket">Time bucket specification for grouping (e.g., "observed_at:15m" for 15-minute intervals). Default is "observed_at:15m".</param>
        /// <returns>A configured <see cref="MetricQueryRequest"/> with time-bucketed statistics.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="metricIds"/> is empty.</exception>
        /// <remarks>
        /// <para>
        /// This method creates a metrics query that retrieves time series data aggregated into time buckets.
        /// Each bucket contains statistical aggregations (count, min, max, avg, stddev, percentile_95, sum)
        /// for the specified time interval.
        /// </para>
        /// <para>
        /// <b>Bucket format:</b> "field:interval" where interval can be:
        /// <list type="bullet">
        /// <item><description>"observed_at:5m" - 5-minute intervals</description></item>
        /// <item><description>"observed_at:15m" - 15-minute intervals (default)</description></item>
        /// <item><description>"observed_at:1h" - 1-hour intervals</description></item>
        /// <item><description>"observed_at:1d" - 1-day intervals</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// This is useful for creating time series charts or analyzing metric trends over time.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var metricIds = new[] { 101, 102, 103 };
        /// var request = MonitorQueryBuilders.MetricsTimeSeries(
        ///     metricIds: metricIds,
        ///     fromUtc: DateTimeOffset.UtcNow.AddHours(-24),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     bucket: "observed_at:1h");
        /// 
        /// var response = await client.QueryMetricsAsync(request);
        /// foreach (var metric in response.Features)
        /// {
        ///     Console.WriteLine($"Metric: {metric.Attributes.Name}");
        ///     foreach (var dataPoint in metric.MetricsData ?? [])
        ///     {
        ///         var attrs = dataPoint.Attributes;
        ///         Console.WriteLine($"  {attrs.ObservedAt}: Avg={attrs.AvgValue}, Max={attrs.MaxValue}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static MetricQueryRequest MetricsTimeSeries(
            IEnumerable<long> metricIds,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            string bucket = "observed_at:15m")
        {
            var ids = string.Join(", ", metricIds.Distinct().OrderBy(x => x));
            return string.IsNullOrWhiteSpace(ids)
                ? throw new ArgumentException("Must specify at least one metricId.", nameof(metricIds))
                : new MetricQueryRequest
                {
                    Where = $"id in ({ids})",
                    Including =
                [
                    new MetricIncludeSpec
                    {
                        Resource = "metrics_data",
                        Where = BetweenTimestamp("observed_at", fromUtc, toUtc),
                        GroupByFieldsForStatistics = ["metric_id", bucket],
                        OutStatistics =
                        [
                            new OutStatistic
                            {
                                OnStatisticField = "value",
                                StatisticType = ["count", "min", "max", "avg", "stddev", "sum"]
                            }
                        ]
                    }
                ]
                };
        }

        /// <summary>
        /// Creates a base collection query request with a single include specification.
        /// </summary>
        /// <param name="collectionName">The name of the collection to query. If null, empty, or "*", queries all collections without filtering.</param>
        /// <param name="include">The include specification defining what child resources to retrieve.</param>
        /// <returns>A <see cref="CollectionQueryRequest"/> filtered by collection name (or unfiltered if collectionName is null/empty/"*").</returns>
        /// <remarks>
        /// <para>
        /// This is a helper method used internally by other builder methods to create
        /// the base query structure with collection filtering.
        /// </para>
        /// <para>
        /// Special collection name handling:
        /// <list type="bullet">
        /// <item><description><c>null</c> or <c>""</c>: Queries all collections (no WHERE clause)</description></item>
        /// <item><description><c>"*"</c>: Queries all collections (no WHERE clause)</description></item>
        /// <item><description>Any other value: Filters by exact collection name</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        private static CollectionQueryRequest CollectionRequest(string collectionName, CollectionIncludeSpec include)
        {
            // If collectionName is null, empty, or "*", query all collections without filtering
            var isAllCollections = string.IsNullOrWhiteSpace(collectionName) || collectionName.Trim() == "*";

            return new CollectionQueryRequest
            {
                Where = isAllCollections ? null : $"(name = '{EscapeSqlLiteral(collectionName)}')",
                Including = [include]
            };
        }

        /// <summary>
        /// Generates a SQL BETWEEN clause for timestamp filtering.
        /// </summary>
        /// <param name="fieldName">The name of the timestamp field to filter.</param>
        /// <param name="fromUtc">Start date/time (UTC).</param>
        /// <param name="toUtc">End date/time (UTC).</param>
        /// <returns>A SQL WHERE clause string in the format: "(fieldName BETWEEN TIMESTAMP 'start' AND TIMESTAMP 'end')".</returns>
        /// <remarks>
        /// <para>
        /// The timestamps are formatted using <see cref="FormatMonitorTimestamp"/> to match
        /// ArcGIS Monitor's expected ISO 8601 format with microsecond precision.
        /// </para>
        /// <para>
        /// The BETWEEN clause is inclusive on both ends.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var clause = MonitorQueryBuilders.BetweenTimestamp(
        ///     "observed_at",
        ///     DateTimeOffset.Parse("2025-01-20T00:00:00Z"),
        ///     DateTimeOffset.Parse("2025-01-27T00:00:00Z"));
        /// // Returns: "(observed_at BETWEEN TIMESTAMP '2025-01-20T00:00:00.000000Z' AND TIMESTAMP '2025-01-27T00:00:00.000000Z')"
        /// </code>
        /// </example>
        public static string BetweenTimestamp(string fieldName, DateTimeOffset fromUtc, DateTimeOffset toUtc)
            => $"({fieldName} BETWEEN TIMESTAMP '{FormatMonitorTimestamp(fromUtc)}'  AND TIMESTAMP '{FormatMonitorTimestamp(toUtc)}')";

        /// <summary>
        /// Generates a SQL WHERE clause for finding alerts that overlap with a specified time range.
        /// </summary>
        /// <param name="fromUtc">Start date/time (UTC) of the time range.</param>
        /// <param name="toUtc">End date/time (UTC) of the time range.</param>
        /// <returns>A SQL WHERE clause string that matches alerts with any overlap in the specified range.</returns>
        /// <remarks>
        /// <para>
        /// This method generates a complex WHERE clause that captures all alerts that have
        /// any overlap with the specified time range, including:
        /// <list type="bullet">
        /// <item><description>Alerts that started before and ended during the range</description></item>
        /// <item><description>Alerts that started and ended within the range</description></item>
        /// <item><description>Alerts that started during and ended after the range</description></item>
        /// <item><description>Alerts that are still open (closed_at IS NULL) and started before the end of the range</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// This ensures that no overlapping alerts are missed in the query results.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var clause = MonitorQueryBuilders.AlertOverlapsWhere(
        ///     DateTimeOffset.Parse("2025-01-20T00:00:00Z"),
        ///     DateTimeOffset.Parse("2025-01-27T00:00:00Z"));
        /// // Returns a complex OR clause covering all overlap scenarios
        /// </code>
        /// </example>
        public static string AlertOverlapsWhere(DateTimeOffset fromUtc, DateTimeOffset toUtc)
        {
            var from = FormatMonitorTimestamp(fromUtc);
            var to = FormatMonitorTimestamp(toUtc);
            return $"(opened_at <= TIMESTAMP '{from}' and closed_at >= TIMESTAMP '{from}') " +
                   $"or (opened_at >= TIMESTAMP '{from}' and closed_at <= TIMESTAMP '{to}') " +
                   $"or (opened_at <= TIMESTAMP '{to}' and closed_at >= TIMESTAMP '{to}') " +
                   $"or (opened_at <= TIMESTAMP '{to}' and closed_at IS NULL)";
        }

        /// <summary>
        /// Formats a <see cref="DateTimeOffset"/> value to ArcGIS Monitor's timestamp format.
        /// </summary>
        /// <param name="value">The date/time value to format.</param>
        /// <returns>A timestamp string in ISO 8601 format with microsecond precision: "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'".</returns>
        /// <remarks>
        /// <para>
        /// ArcGIS Monitor expects timestamps in ISO 8601 format with:
        /// <list type="bullet">
        /// <item><description>UTC timezone (indicated by 'Z' suffix)</description></item>
        /// <item><description>Microsecond precision (6 decimal places for seconds)</description></item>
        /// <item><description>'T' separator between date and time</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The input value is automatically converted to UTC if it isn't already.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var timestamp = MonitorQueryBuilders.FormatMonitorTimestamp(
        ///     DateTimeOffset.Parse("2025-01-27T14:30:45.123456Z"));
        /// // Returns: "2025-01-27T14:30:45.123456Z"
        /// </code>
        /// </example>
        public static string FormatMonitorTimestamp(DateTimeOffset value)
            => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'");

        /// <summary>
        /// Builds a query request to retrieve components directly with metrics, aggregated statistics, and alerts using the /monitoring/components/query endpoint.
        /// </summary>
        /// <param name="where">SQL-like where clause (e.g., "state = 'monitored'").</param>
        /// <param name="fromUtc">Start date/time (UTC) for the statistics aggregation period and alert filtering.</param>
        /// <param name="toUtc">End date/time (UTC) for the statistics aggregation period and alert filtering.</param>
        /// <param name="returnCountOnly">If true, only returns the count of components.</param>
        /// <param name="resultRecordCount">Maximum number of records to return per page. Default is 100.</param>
        /// <param name="resultOffset">Offset for pagination. Default is 0.</param>
        /// <returns>A component query request with nested metrics, aggregated statistics, and alerts.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a direct component query that bypasses collection filtering.
        /// It's useful when you need to query all components across all collections efficiently.
        /// </para>
        /// <para>
        /// The query includes:
        /// <list type="bullet">
        /// <item><description>Components matching the WHERE clause</description></item>
        /// <item><description>All metrics for each component</description></item>
        /// <item><description>Aggregated statistics for metric data in the specified time range (count, min, max, avg, stddev, sum)</description></item>
        /// <item><description>Alerts that overlap with the specified time range</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Common use cases:
        /// <list type="bullet">
        /// <item><description>Query all monitored components: where: "state = 'monitored'"</description></item>
        /// <item><description>Query by component type: where: "type = 'host' AND state = 'monitored'"</description></item>
        /// <item><description>Query by multiple criteria: where: "state = 'monitored' AND type IN ('host', 'service')"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var request = MonitorQueryBuilders.AllComponentsWithMetrics(
        ///     where: "state = 'monitored'",
        ///     fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
        ///     toUtc: DateTimeOffset.UtcNow,
        ///     returnCountOnly: false,
        ///     resultRecordCount: 200);
        /// 
        /// var response = await client.QueryComponentsAsync(request);
        /// foreach (var component in response.Features)
        /// {
        ///     Console.WriteLine($"Component: {component.Attributes.Name}");
        ///     foreach (var metric in component.Metrics ?? [])
        ///     {
        ///         var stats = metric.MetricsData?.FirstOrDefault()?.Attributes;
        ///         if (stats != null)
        ///         {
        ///             Console.WriteLine($"  Metric: {metric.Attributes.Name}");
        ///             Console.WriteLine($"    Avg: {stats.AvgValue}, Max: {stats.MaxValue}");
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public static ComponentQueryRequest AllComponentsWithMetrics(
            string where,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            bool returnCountOnly = false,
            int resultRecordCount = 100,
            int resultOffset = 0) => new()
            {
                Where = where,
                ReturnCountOnly = returnCountOnly,
                ResultRecordCount = resultRecordCount,
                ResultOffset = resultOffset,
                Including =
                [
                    new ComponentIncludeSpec
                    {
                        Resource = "metrics",
                        Including =
                        [
                            new ComponentIncludeSpec
                            {
                                Resource = "metrics_data",
                                Where = BetweenTimestamp("observed_at", fromUtc, toUtc),
                                GroupByFieldsForStatistics = "metric_id",
                                OutStatistics =
                                [
                                    new OutStatistic
                                    {
                                        OnStatisticField = "value",
                                        StatisticType = ["count", "avg", "min", "max", "sum", "stddev"]
                                    }
                                ]
                            },
                            new ComponentIncludeSpec
                            {
                                Resource = "alerts",
                                Where = AlertOverlapsWhere(fromUtc, toUtc)
                            }
                        ]
                    }
                ]
            };

        /// <summary>
        /// Escapes single quotes in a string for safe use in SQL string literals.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        /// <returns>The escaped string with single quotes doubled.</returns>
        /// <remarks>
        /// <para>
        /// This method prevents SQL injection by escaping single quotes in user-provided strings
        /// that will be used in SQL WHERE clauses.
        /// </para>
        /// <para>
        /// In SQL, single quotes are escaped by doubling them: <c>'</c> becomes <c>''</c>.
        /// </para>
        /// <para>
        /// All methods that build WHERE clauses use this internally to ensure safe query construction.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var escaped = MonitorQueryBuilders.EscapeSqlLiteral("O'Connor's Server");
        /// // Returns: "O''Connor''s Server"
        /// // Safe to use: WHERE name = 'O''Connor''s Server'
        /// </code>
        /// </example>
        private static string EscapeSqlLiteral(string value)
            => value.Replace("'", "''", StringComparison.Ordinal);
    }
}
