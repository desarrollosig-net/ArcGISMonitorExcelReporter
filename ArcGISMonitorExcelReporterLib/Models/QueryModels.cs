using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models
{
    /// <summary>
    /// Base class for ArcGIS Monitor query requests.
    /// </summary>
    /// <remarks>
    /// Provides common properties for filtering, pagination, and count-only queries
    /// across different Monitor API endpoints.
    /// </remarks>
    public class QueryRequest
    {
        /// <summary>
        /// SQL-like WHERE clause for filtering results.
        /// </summary>
        [JsonPropertyName("where")]
        public string? Where { get; set; }

        /// <summary>
        /// List of related resources to include in the response.
        /// </summary>
        [JsonPropertyName("including")]
        public List<IncludeSpec>? Including { get; set; }

        /// <summary>
        /// If true, returns only the count of matching records without retrieving full data.
        /// </summary>
        [JsonPropertyName("returnCountOnly")]
        public bool? ReturnCountOnly { get; set; }

        /// <summary>
        /// Maximum number of records to return per page (pagination size).
        /// </summary>
        [JsonPropertyName("resultRecordCount")]
        public int? ResultRecordCount { get; set; }

        /// <summary>
        /// Offset for pagination (number of records to skip).
        /// </summary>
        [JsonPropertyName("resultOffset")]
        public int? ResultOffset { get; set; }
    }

    /// <summary>
    /// Query request for the /monitoring/collections/query endpoint.
    /// </summary>
    /// <remarks>
    /// Used to query collections with their nested components, metrics, and related resources.
    /// </remarks>
    public sealed class CollectionQueryRequest : QueryRequest
    {
        /// <summary>
        /// List of collection-specific related resources to include in the response.
        /// </summary>
        [JsonPropertyName("including")]
        public new List<CollectionIncludeSpec>? Including { get; set; }
    }

    /// <summary>
    /// Query request for the /monitoring/metrics/query endpoint.
    /// </summary>
    /// <remarks>
    /// Used to query metrics with their time series data and statistical aggregations.
    /// </remarks>
    public sealed class MetricQueryRequest : QueryRequest
    {
        /// <summary>
        /// List of metric-specific related resources to include in the response.
        /// </summary>
        [JsonPropertyName("including")]
        public new List<MetricIncludeSpec>? Including { get; set; }
    }

    /// <summary>
    /// Base specification for including related resources in a query response.
    /// </summary>
    public class IncludeSpec
    {
        /// <summary>
        /// Name of the related resource to include (e.g., "metrics", "components", "alerts").
        /// </summary>
        [JsonPropertyName("resource")]
        public string Resource { get; set; } = string.Empty;

        /// <summary>
        /// SQL-like WHERE clause for filtering this related resource.
        /// </summary>
        [JsonPropertyName("where")]
        public string? Where { get; set; }

        /// <summary>
        /// If true, returns only the count of this related resource.
        /// </summary>
        [JsonPropertyName("returnCountOnly")]
        public bool? ReturnCountOnly { get; set; }

        /// <summary>
        /// Maximum number of records to return for this related resource.
        /// </summary>
        [JsonPropertyName("resultRecordCount")]
        public int? ResultRecordCount { get; set; }

        /// <summary>
        /// Offset for pagination of this related resource.
        /// </summary>
        [JsonPropertyName("resultOffset")]
        public int? ResultOffset { get; set; }

        /// <summary>
        /// Statistical aggregations to perform on this related resource.
        /// </summary>
        [JsonPropertyName("outStatistics")]
        public List<OutStatistic>? OutStatistics { get; set; }
    }

    /// <summary>
    /// Include specification for collection-related resources.
    /// </summary>
    /// <remarks>
    /// Supports nested inclusion of components, metrics, and other collection-related resources.
    /// </remarks>
    public sealed class CollectionIncludeSpec : IncludeSpec
    {
        /// <summary>
        /// Nested list of related resources to include within this resource.
        /// </summary>
        [JsonPropertyName("including")]
        public List<CollectionIncludeSpec>? Including { get; set; }

        /// <summary>
        /// Field name(s) to group by when calculating statistics (used by /collections/query).
        /// </summary>
        /// <remarks>
        /// This is a string format used specifically by the collections endpoint.
        /// </remarks>
        [JsonPropertyName("groupbyFieldsForStatistics")]
        public string? GroupbyFieldsForStatistics { get; set; }
    }

    /// <summary>
    /// Include specification for metric-related resources.
    /// </summary>
    /// <remarks>
    /// Supports nested inclusion of metrics_data, alerts, and other metric-related resources.
    /// </remarks>
    public sealed class MetricIncludeSpec : IncludeSpec
    {
        /// <summary>
        /// Nested list of related resources to include within this resource.
        /// </summary>
        [JsonPropertyName("including")]
        public List<MetricIncludeSpec>? Including { get; set; }

        /// <summary>
        /// Field name(s) to group by when calculating statistics (used by /metrics/query).
        /// </summary>
        /// <remarks>
        /// This is an array format used specifically by the metrics endpoint.
        /// Supports time buckets like ["metric_id", "observed_at:15m"].
        /// </remarks>
        [JsonPropertyName("groupByFieldsForStatistics")]
        public List<string>? GroupByFieldsForStatistics { get; set; }
    }

    /// <summary>
    /// Statistical aggregation specification for metric data.
    /// </summary>
    /// <remarks>
    /// Defines which statistical operations to perform on a field (e.g., count, min, max, avg).
    /// </remarks>
    public sealed class OutStatistic
    {
        /// <summary>
        /// Types of statistics to calculate.
        /// </summary>
        /// <remarks>
        /// Supported values: "count", "min", "max", "avg", "stddev", "percentile_95", "sum".
        /// </remarks>
        [JsonPropertyName("statisticType")]
        public List<string> StatisticType { get; set; } = [];

        /// <summary>
        /// Name of the field to calculate statistics on.
        /// </summary>
        /// <remarks>
        /// Typically "value" for metric data.
        /// </remarks>
        [JsonPropertyName("onStatisticField")]
        public string OnStatisticField { get; set; } = "value";
    }

    /// <summary>
    /// Include specification for component-related resources.
    /// </summary>
    /// <remarks>
    /// Used when querying components directly via the /monitoring/components/query endpoint.
    /// Supports nested inclusion of metrics and other component-related resources.
    /// </remarks>
    public class ComponentIncludeSpec: IncludeSpec
    {
        /// <summary>
        /// Nested list of related resources to include within this component resource.
        /// </summary>
        /// <remarks>
        /// Commonly used to include metrics: new ComponentIncludeSpec { Resource = "metrics" }.
        /// </remarks>
        [JsonPropertyName("including")]
        public List<ComponentIncludeSpec>? Including { get; set; }

        /// <summary>
        /// Field name(s) to group by when calculating statistics (used by /components/query).
        /// </summary>
        /// <remarks>
        /// This is a string format. Example: "metric_id" or "component_id".
        /// </remarks>
        [JsonPropertyName("groupbyFieldsForStatistics")]
        public string? GroupByFieldsForStatistics { get; set; }
    }

    /// <summary>
    /// Query request for the /monitoring/components/query endpoint.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used to query components directly without going through collections.
    /// This is more efficient when querying all components across all collections.
    /// </para>
    /// <para>
    /// Common use case: Query all monitored components with WHERE clause "state = 'monitored'".
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = new ComponentQueryRequest
    /// {
    ///     Where = "state = 'monitored'",
    ///     ReturnCountOnly = false,
    ///     ResultRecordCount = 200,
    ///     Including = new List&lt;ComponentIncludeSpec&gt;
    ///     {
    ///         new ComponentIncludeSpec { Resource = "metrics" }
    ///     }
    /// };
    /// </code>
    /// </example>
    public sealed class ComponentQueryRequest : QueryRequest
    {
        /// <summary>
        /// List of component-specific related resources to include in the response.
        /// </summary>
        /// <remarks>
        /// Commonly includes "metrics" to retrieve component metrics.
        /// </remarks>
        [JsonPropertyName("including")]
        public new List<ComponentIncludeSpec>? Including { get; set; }
    }
}
