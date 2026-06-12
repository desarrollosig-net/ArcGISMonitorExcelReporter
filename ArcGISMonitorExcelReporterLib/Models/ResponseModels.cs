using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models
{
    /// <summary>
    /// Generic response wrapper for ArcGIS Monitor query results.
    /// </summary>
    /// <typeparam name="TFeature">The type of features returned in the response.</typeparam>
    /// <remarks>
    /// This class represents the standard response structure from ArcGIS Monitor API endpoints.
    /// It contains a list of features and metadata about the query execution.
    /// </remarks>
    public sealed class QueryResponse<TFeature>
    {
        /// <summary>
        /// List of features returned by the query.
        /// </summary>
        [JsonPropertyName("features")]
        public List<TFeature> Features { get; set; } = [];

        /// <summary>
        /// Indicates whether the response exceeded the transfer limit.
        /// </summary>
        /// <remarks>
        /// When true, the response contains a partial result set and additional queries
        /// with pagination may be needed to retrieve all results.
        /// </remarks>
        [JsonPropertyName("exceededTransferLimit")]
        public bool ExceededTransferLimit { get; set; }

        /// <summary>
        /// Total count of features matching the query criteria.
        /// </summary>
        /// <remarks>
        /// Only populated when returnCountOnly is true in the query request.
        /// </remarks>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Generic feature wrapper that contains typed attributes.
    /// </summary>
    /// <typeparam name="TAttributes">The type of attributes contained in this feature.</typeparam>
    /// <remarks>
    /// This class represents a standard feature structure where the actual data
    /// is contained in a nested "attributes" object.
    /// </remarks>
    public sealed class AttributeFeature<TAttributes>
    {
        /// <summary>
        /// The attributes object containing the feature's data.
        /// </summary>
        [JsonPropertyName("attributes")]
        public TAttributes Attributes { get; set; } = default!;
    }

    /// <summary>
    /// Represents a collection feature from ArcGIS Monitor.
    /// </summary>
    /// <remarks>
    /// Collections are groupings of components in ArcGIS Monitor.
    /// This class contains collection metadata and optionally nested components.
    /// </remarks>
    public sealed class CollectionFeature
    {
        /// <summary>
        /// Collection metadata and properties.
        /// </summary>
        [JsonPropertyName("attributes")]
        public CollectionAttributes Attributes { get; set; } = new();

        /// <summary>
        /// Components belonging to this collection.
        /// </summary>
        /// <remarks>
        /// The JSON representation can be either:
        /// - A count object: { "count": 10 } when returnCountOnly is true
        /// - An array of components when returnCountOnly is false
        /// The custom converter handles both formats transparently.
        /// </remarks>
        [JsonPropertyName("components")]
        [JsonConverter(typeof(ComponentsResultJsonConverter))]
        public ComponentsResult Components { get; set; } = new();
    }

    /// <summary>
    /// Represents components result that can be either a count or a list of components.
    /// </summary>
    /// <remarks>
    /// This class handles the dual nature of the components response:
    /// - When querying with returnCountOnly=true, it contains a Count
    /// - When querying with returnCountOnly=false, it contains Items
    /// The <see cref="ComponentsResultJsonConverter"/> handles deserialization.
    /// </remarks>
    public sealed class ComponentsResult
    {
        /// <summary>
        /// Number of components (populated when returnCountOnly is true).
        /// </summary>
        public int? Count { get; set; }

        /// <summary>
        /// List of component features (populated when returnCountOnly is false).
        /// </summary>
        public List<ComponentFeature> Items { get; set; } = [];
    }

    /// <summary>
    /// Custom JSON converter for <see cref="ComponentsResult"/> that handles both count objects and component arrays.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This converter handles the polymorphic JSON response for components:
    /// <list type="bullet">
    /// <item><description>When returnCountOnly=true: { "count": 10 }</description></item>
    /// <item><description>When returnCountOnly=false: [ComponentFeature, ComponentFeature, ...]</description></item>
    /// <item><description>When components is null: null or empty object</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class ComponentsResultJsonConverter : JsonConverter<ComponentsResult>
    {
        /// <summary>
        /// Reads and deserializes JSON to a <see cref="ComponentsResult"/>.
        /// </summary>
        public override ComponentsResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var result = new ComponentsResult();
                if(doc.RootElement.TryGetProperty("count", out var count) && count.ValueKind == JsonValueKind.Number)
                {
                    result.Count = count.GetInt32();
                }

                return result;
            }

            if(reader.TokenType == JsonTokenType.StartArray)
            {
                var items = JsonSerializer.Deserialize<List<ComponentFeature>>(ref reader, options) ?? [];
                return new ComponentsResult { Items = items };
            }

            return reader.TokenType == JsonTokenType.Null
                ? new ComponentsResult()
                : throw new JsonException($"No se puede convertir token {reader.TokenType} a ComponentsResult.");
        }

        /// <summary>
        /// Writes a <see cref="ComponentsResult"/> to JSON.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, ComponentsResult value, JsonSerializerOptions options)
        {
            if(value.Items.Count > 0)
            {
                JsonSerializer.Serialize(writer, value.Items, options);
                return;
            }

            writer.WriteStartObject();
            if(value.Count.HasValue)
            {
                writer.WriteNumber("count", value.Count.Value);
            }

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Represents a component (monitored resource) in ArcGIS Monitor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Components are the core monitored entities in ArcGIS Monitor.
    /// They can represent:
    /// <list type="bullet">
    /// <item><description>Hosts (servers, machines)</description></item>
    /// <item><description>Services (ArcGIS Server services, web services)</description></item>
    /// <item><description>Databases (SQL Server, PostgreSQL, etc.)</description></item>
    /// <item><description>Other monitored resources</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Each component can have associated metrics, labels, parent relationships, agents, logs, and observers.
    /// </para>
    /// </remarks>
    public sealed class ComponentFeature
    {
        /// <summary>
        /// Component metadata and properties.
        /// </summary>
        [JsonPropertyName("attributes")]
        public ComponentAttributes Attributes { get; set; } = new();

        /// <summary>
        /// Metrics associated with this component.
        /// </summary>
        /// <remarks>
        /// Only populated when the query includes metrics in the "including" specification.
        /// </remarks>
        [JsonPropertyName("metrics")]
        public List<MetricFeature>? Metrics { get; set; }

        /// <summary>
        /// Labels (tags) assigned to this component.
        /// </summary>
        [JsonPropertyName("labels")]
        public List<AttributeFeature<LabelAttributes>>? Labels { get; set; }

        /// <summary>
        /// Parent components in the component hierarchy.
        /// </summary>
        [JsonPropertyName("parents")]
        public List<AttributeFeature<ComponentAttributes>>? Parents { get; set; }

        /// <summary>
        /// Agents that monitor this component.
        /// </summary>
        [JsonPropertyName("agents")]
        public List<AttributeFeature<AgentAttributes>>? Agents { get; set; }

        /// <summary>
        /// Log entries for this component.
        /// </summary>
        [JsonPropertyName("components_logs")]
        public List<AttributeFeature<ComponentLogAttributes>>? ComponentLogs { get; set; }

        /// <summary>
        /// Observers monitoring this component.
        /// </summary>
        [JsonPropertyName("observers")]
        public List<AttributeFeature<ObserverAttributes>>? Observers { get; set; }

        /// <summary>
        /// Count of components (used when returnCountOnly is true).
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Represents a metric (performance counter or measurement) in ArcGIS Monitor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Metrics are measurements collected from components.
    /// Examples include:
    /// <list type="bullet">
    /// <item><description>CPU Utilization %</description></item>
    /// <item><description>Memory Used (MB)</description></item>
    /// <item><description>Requests per Second</description></item>
    /// <item><description>Response Time (ms)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Each metric can have associated time series data (metrics_data) and alerts.
    /// </para>
    /// </remarks>
    public sealed class MetricFeature
    {
        /// <summary>
        /// Metric metadata and configuration.
        /// </summary>
        [JsonPropertyName("attributes")]
        public MetricAttributes Attributes { get; set; } = new();

        /// <summary>
        /// Time series data points for this metric.
        /// </summary>
        /// <remarks>
        /// Contains aggregated statistics (count, min, max, avg, stddev, percentile_95, sum)
        /// when queried with OutStatistics.
        /// </remarks>
        [JsonPropertyName("metrics_data")]
        public List<AttributeFeature<MetricDataAttributes>>? MetricsData { get; set; }

        /// <summary>
        /// Alerts triggered by this metric.
        /// </summary>
        [JsonPropertyName("alerts")]
        public List<AttributeFeature<AlertAttributes>>? Alerts { get; set; }
    }

    /// <summary>
    /// Attributes and metadata for a collection.
    /// </summary>
    /// <remarks>
    /// Collections are logical groupings of components in ArcGIS Monitor.
    /// They can be defined by resource expressions that dynamically include components
    /// based on criteria.
    /// </remarks>
    public sealed class CollectionAttributes
    {
        /// <summary>Unique identifier for the collection.</summary>
        [JsonPropertyName("id")] public int Id { get; set; }

        /// <summary>Date and time when the collection was created (UTC).</summary>
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>Display name of the collection.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }

        /// <summary>Description of the collection.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }

        /// <summary>Status code of the collection.</summary>
        [JsonPropertyName("status")] public int? Status { get; set; }

        /// <summary>Indicates whether service monitoring is enabled for this collection.</summary>
        [JsonPropertyName("is_service_enabled")] public bool? IsServiceEnabled { get; set; }

        /// <summary>Indicates whether security features are enabled for this collection.</summary>
        [JsonPropertyName("is_security_enabled")] public bool? IsSecurityEnabled { get; set; }

        /// <summary>Resource expression defining which components belong to this collection.</summary>
        [JsonPropertyName("expression")] public ResourceExpression? Expression { get; set; }

        /// <summary>URL of the service endpoint for this collection (if applicable).</summary>
        [JsonPropertyName("service_url")] public string? ServiceUrl { get; set; }
    }

    /// <summary>
    /// Represents a resource expression used to define collection membership.
    /// </summary>
    /// <remarks>
    /// Resource expressions use a query language to dynamically include components
    /// in a collection based on their properties.
    /// </remarks>
    public sealed class ResourceExpression
    {
        /// <summary>
        /// The resource expression query string.
        /// </summary>
        [JsonPropertyName("resource")]
        public string? Resource { get; set; }
    }

    /// <summary>
    /// Attributes and metadata for a component (monitored resource).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Components represent the actual monitored entities in ArcGIS Monitor.
    /// This class contains extensive metadata covering different component types:
    /// <list type="bullet">
    /// <item><description><b>Common:</b> id, name, type, state, status</description></item>
    /// <item><description><b>Hosts:</b> CPU info, memory, storage, network speed</description></item>
    /// <item><description><b>Services:</b> version, instance configuration, caching</description></item>
    /// <item><description><b>Databases:</b> version, connection limits, backup info</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Not all properties are populated for all component types. Properties are
    /// populated based on the component's type and what information is available.
    /// </para>
    /// </remarks>
    public sealed class ComponentAttributes
    {
        /// <summary>Unique identifier for the component.</summary>
        [JsonPropertyName("id")] public int Id { get; set; }

        /// <summary>Date and time when the component was created (UTC).</summary>
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>System-specific identifier for the component.</summary>
        [JsonPropertyName("system_id")] public string? SystemId { get; set; }

        /// <summary>Display name of the component.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }

        /// <summary>Description of the component.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }

        /// <summary>Type of component (e.g., "host", "service", "database").</summary>
        [JsonPropertyName("type")] public string? Type { get; set; }

        /// <summary>Subtype providing more specific categorization.</summary>
        [JsonPropertyName("subtype")] public string? Subtype { get; set; }

        /// <summary>Internal network address of the component.</summary>
        [JsonPropertyName("address_internal")] public string? AddressInternal { get; set; }

        /// <summary>Status code of the component.</summary>
        [JsonPropertyName("status")] public int? Status { get; set; }

        /// <summary>Current state of the component (e.g., "monitored", "stopped").</summary>
        [JsonPropertyName("state")] public string? State { get; set; }

        /// <summary>ID of the connection used to monitor this component.</summary>
        [JsonPropertyName("connection_id")] public int? ConnectionId { get; set; }

        /// <summary>Version number of the component software.</summary>
        [JsonPropertyName("version")] public string? Version { get; set; }

        /// <summary>Physical or logical location of the component.</summary>
        [JsonPropertyName("location")] public string? Location { get; set; }

        /// <summary>Date and time when the component was started (UTC).</summary>
        [JsonPropertyName("started_at")] public DateTimeOffset? StartedAt { get; set; }

        /// <summary>Date and time when the SSL/TLS certificate expires (UTC).</summary>
        [JsonPropertyName("cert_expires_at")] public DateTimeOffset? CertExpiresAt { get; set; }

        /// <summary>Date and time when the license expires (UTC).</summary>
        [JsonPropertyName("license_expires_at")] public DateTimeOffset? LicenseExpiresAt { get; set; }

        /// <summary>Total physical memory in MB (for host components).</summary>
        [JsonPropertyName("memory_total")] public double? MemoryTotal { get; set; }

        /// <summary>Classification of the component.</summary>
        [JsonPropertyName("class")] public string? Class { get; set; }

        /// <summary>CPU model name (for host components).</summary>
        [JsonPropertyName("cpu_name")] public string? CpuName { get; set; }

        /// <summary>CPU clock speed in GHz (for host components).</summary>
        [JsonPropertyName("cpu_speed")] public double? CpuSpeed { get; set; }

        /// <summary>Number of physical CPU cores (for host components).</summary>
        [JsonPropertyName("cpu_cores_physical")] public int? CpuCoresPhysical { get; set; }

        /// <summary>Number of logical CPU cores/threads (for host components).</summary>
        [JsonPropertyName("cpu_cores_logical")] public int? CpuCoresLogical { get; set; }

        /// <summary>Network interface speed in Mbps (for host components).</summary>
        [JsonPropertyName("network_speed")] public int? NetworkSpeed { get; set; }

        /// <summary>Total page/swap file size in MB (for host components).</summary>
        [JsonPropertyName("memory_page_total")] public double? MemoryPageTotal { get; set; }

        /// <summary>Total storage capacity in GB (for host components).</summary>
        [JsonPropertyName("storage_total")] public double? StorageTotal { get; set; }

        /// <summary>Geodatabase version (for database components).</summary>
        [JsonPropertyName("gdb_version")] public string? GdbVersion { get; set; }

        /// <summary>Minimum number of shared instances (for service components).</summary>
        [JsonPropertyName("instances_shared_min")] public int? InstancesSharedMin { get; set; }

        /// <summary>Maximum number of shared instances (for service components).</summary>
        [JsonPropertyName("instances_shared_max")] public int? InstancesSharedMax { get; set; }

        /// <summary>System mode configuration.</summary>
        [JsonPropertyName("system_mode")] public string? SystemMode { get; set; }

        /// <summary>System state information.</summary>
        [JsonPropertyName("system_state")] public string? SystemState { get; set; }

        /// <summary>Instance type (e.g., "shared", "dedicated") for service components.</summary>
        [JsonPropertyName("instance_type")] public string? InstanceType { get; set; }

        /// <summary>Minimum number of instances (for service components).</summary>
        [JsonPropertyName("instances_min")] public int? InstancesMin { get; set; }

        /// <summary>Maximum number of instances (for service components).</summary>
        [JsonPropertyName("instances_max")] public int? InstancesMax { get; set; }

        /// <summary>Maximum wait time in seconds (for service components).</summary>
        [JsonPropertyName("wait_time_max")] public int? WaitTimeMax { get; set; }

        /// <summary>Maximum idle time in seconds (for service components).</summary>
        [JsonPropertyName("idle_time_max")] public int? IdleTimeMax { get; set; }

        /// <summary>Indicates whether caching is enabled (for service components).</summary>
        [JsonPropertyName("is_cached")] public bool? IsCached { get; set; }

        /// <summary>Geometry type (for feature services).</summary>
        [JsonPropertyName("geometry_type")] public string? GeometryType { get; set; }

        /// <summary>Versioning type (for geodatabase services).</summary>
        [JsonPropertyName("versioned_type")] public string? VersionedType { get; set; }

        /// <summary>Indicates whether the data is archived (for geodatabase components).</summary>
        [JsonPropertyName("is_archived")] public bool? IsArchived { get; set; }

        /// <summary>Date and time of last modification (UTC).</summary>
        [JsonPropertyName("last_modified_at")] public DateTimeOffset? LastModifiedAt { get; set; }

        /// <summary>Date and time of last backup (UTC) for database components.</summary>
        [JsonPropertyName("last_backup_at")] public DateTimeOffset? LastBackupAt { get; set; }

        /// <summary>Maximum number of concurrent connections (for database components).</summary>
        [JsonPropertyName("connections_max")] public int? ConnectionsMax { get; set; }
    }

    /// <summary>
    /// Attributes and metadata for a metric (performance measurement).
    /// </summary>
    /// <remarks>
    /// Metrics represent specific measurements collected from components,
    /// such as CPU usage, memory consumption, request counts, etc.
    /// Each metric includes alerting configuration and component relationship information.
    /// </remarks>
    public sealed class MetricAttributes
    {
        /// <summary>Unique identifier for the metric.</summary>
        [JsonPropertyName("id")] public int Id { get; set; }

        /// <summary>Date and time when the metric was created (UTC).</summary>
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>Resource identifier for the metric.</summary>
        [JsonPropertyName("r_id")] public string? RId { get; set; }

        /// <summary>Display name of the metric.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }

        /// <summary>Base resource identifier.</summary>
        [JsonPropertyName("base_r_id")] public string? BaseRId { get; set; }

        /// <summary>ID of the component this metric belongs to.</summary>
        [JsonPropertyName("component_id")] public int? ComponentId { get; set; }

        /// <summary>ID of the observer collecting this metric.</summary>
        [JsonPropertyName("observer_id")] public int? ObserverId { get; set; }

        /// <summary>Status code of the metric.</summary>
        [JsonPropertyName("status")] public int? Status { get; set; }

        /// <summary>Indicates whether alerting is enabled for this metric.</summary>
        [JsonPropertyName("is_alerting_enabled")] public bool? IsAlertingEnabled { get; set; }

        /// <summary>Aggregation method (e.g., "avg", "max", "min", "sum").</summary>
        [JsonPropertyName("aggregation")] public string? Aggregation { get; set; }

        /// <summary>Comparison operator for threshold evaluation (e.g., ">", "<", ">=", "<=").</summary>
        [JsonPropertyName("operator")] public string? Operator { get; set; }

        /// <summary>Number of samples to consider for threshold evaluation.</summary>
        [JsonPropertyName("samples")] public int? Samples { get; set; }

        /// <summary>Information level threshold value.</summary>
        [JsonPropertyName("info_threshold")] public double? InfoThreshold { get; set; }

        /// <summary>Warning level threshold value.</summary>
        [JsonPropertyName("warning_threshold")] public double? WarningThreshold { get; set; }

        /// <summary>Critical level threshold value.</summary>
        [JsonPropertyName("critical_threshold")] public double? CriticalThreshold { get; set; }

        /// <summary>Description of the metric.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }

        /// <summary>Unit of measurement (e.g., "%", "MB", "ms", "count").</summary>
        [JsonPropertyName("unit")] public string? Unit { get; set; }

        /// <summary>Name of the component this metric belongs to.</summary>
        [JsonPropertyName("component_name")] public string? ComponentName { get; set; }

        /// <summary>Internal address of the component this metric belongs to.</summary>
        [JsonPropertyName("component_address_internal")] public string? ComponentAddressInternal { get; set; }

        /// <summary>Type of the component this metric belongs to.</summary>
        [JsonPropertyName("component_type")] public string? ComponentType { get; set; }

        /// <summary>Subtype of the component this metric belongs to.</summary>
        [JsonPropertyName("component_subtype")] public string? ComponentSubtype { get; set; }
    }

    /// <summary>
    /// Attributes for metric data points with statistical aggregations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class represents aggregated metric data over a time period.
    /// When querying with OutStatistics, each data point contains statistical
    /// summaries (count, min, max, avg, stddev, percentile_95, sum) for the
    /// specified time bucket.
    /// </para>
    /// <para>
    /// The ObservedAt timestamp represents either:
    /// <list type="bullet">
    /// <item><description>The exact observation time for raw data points</description></item>
    /// <item><description>The bucket start time for aggregated data</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class MetricDataAttributes
    {
        /// <summary>ID of the metric this data belongs to.</summary>
        [JsonPropertyName("metric_id")] public int? MetricId { get; set; }

        /// <summary>Timestamp when the metric was observed or the start of the aggregation bucket (UTC).</summary>
        [JsonPropertyName("observed_at")] public DateTimeOffset? ObservedAt { get; set; }

        /// <summary>Number of data points in this aggregation.</summary>
        [JsonPropertyName("COUNT_value")] public double? CountValue { get; set; }

        /// <summary>Average value in this aggregation.</summary>
        [JsonPropertyName("AVG_value")] public double? AvgValue { get; set; }

        /// <summary>Minimum value in this aggregation.</summary>
        [JsonPropertyName("MIN_value")] public double? MinValue { get; set; }

        /// <summary>Maximum value in this aggregation.</summary>
        [JsonPropertyName("MAX_value")] public double? MaxValue { get; set; }

        /// <summary>Sum of all values in this aggregation.</summary>
        [JsonPropertyName("SUM_value")] public double? SumValue { get; set; }

        /// <summary>Standard deviation of values in this aggregation.</summary>
        [JsonPropertyName("STDDEV_value")] public double? StdDevValue { get; set; }

        /// <summary>95th percentile value in this aggregation.</summary>
        [JsonPropertyName("PERCENTILE_95_value")] public double? Percentile95Value { get; set; }
    }

    /// <summary>
    /// Attributes for an alert triggered by a metric threshold violation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Alerts are created when a metric value crosses a configured threshold.
    /// They track:
    /// <list type="bullet">
    /// <item><description>When the alert opened (threshold was breached)</description></item>
    /// <item><description>When the alert closed (metric returned to normal)</description></item>
    /// <item><description>The severity state (info, warning, critical)</description></item>
    /// <item><description>The threshold configuration that triggered the alert</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Open alerts have ClosedAt = null. Duration is calculated from OpenedAt to
    /// either ClosedAt (for closed alerts) or the current time (for open alerts).
    /// </para>
    /// </remarks>
    public sealed class AlertAttributes
    {
        /// <summary>Unique identifier for the alert.</summary>
        [JsonPropertyName("id")] public int Id { get; set; }

        /// <summary>Date and time when the alert record was created (UTC).</summary>
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>Date and time when the alert opened/triggered (UTC).</summary>
        [JsonPropertyName("opened_at")] public DateTimeOffset? OpenedAt { get; set; }

        /// <summary>Date and time when the alert closed (UTC). Null if still open.</summary>
        [JsonPropertyName("closed_at")] public DateTimeOffset? ClosedAt { get; set; }

        /// <summary>ID of the metric that triggered this alert.</summary>
        [JsonPropertyName("metric_id")] public int? MetricId { get; set; }

        /// <summary>ID of the observer monitoring the metric.</summary>
        [JsonPropertyName("observer_id")] public int? ObserverId { get; set; }

        /// <summary>ID of the component the metric belongs to.</summary>
        [JsonPropertyName("component_id")] public int? ComponentId { get; set; }

        /// <summary>Alert state/severity level (e.g., "info", "warning", "critical").</summary>
        [JsonPropertyName("state")] public string? State { get; set; }

        /// <summary>Status code of the alert.</summary>
        [JsonPropertyName("status")] public int? Status { get; set; }

        /// <summary>Aggregation method used for threshold evaluation.</summary>
        [JsonPropertyName("aggregation")] public string? Aggregation { get; set; }

        /// <summary>Comparison operator used for threshold evaluation.</summary>
        [JsonPropertyName("operator")] public string? Operator { get; set; }

        /// <summary>Number of samples considered for threshold evaluation.</summary>
        [JsonPropertyName("samples")] public int? Samples { get; set; }

        /// <summary>Information threshold value.</summary>
        [JsonPropertyName("info_threshold")] public double? InfoThreshold { get; set; }

        /// <summary>Warning threshold value.</summary>
        [JsonPropertyName("warning_threshold")] public double? WarningThreshold { get; set; }

        /// <summary>Critical threshold value.</summary>
        [JsonPropertyName("critical_threshold")] public double? CriticalThreshold { get; set; }

        /// <summary>Name of the component that triggered the alert.</summary>
        [JsonPropertyName("component_name")] public string? ComponentName { get; set; }

        /// <summary>Internal address of the component that triggered the alert.</summary>
        [JsonPropertyName("component_address_internal")] public string? ComponentAddressInternal { get; set; }

        /// <summary>Type of the component that triggered the alert.</summary>
        [JsonPropertyName("component_type")] public string? ComponentType { get; set; }

        /// <summary>Subtype of the component that triggered the alert.</summary>
        [JsonPropertyName("component_subtype")] public string? ComponentSubtype { get; set; }

        /// <summary>Name of the metric that triggered the alert.</summary>
        [JsonPropertyName("metric_name")] public string? MetricName { get; set; }

        /// <summary>Resource identifier of the metric that triggered the alert.</summary>
        [JsonPropertyName("metric_r_id")] public string? MetricRId { get; set; }

        /// <summary>Unit of measurement for the metric.</summary>
        [JsonPropertyName("metric_unit")] public string? MetricUnit { get; set; }

        /// <summary>Duration of the alert in milliseconds.</summary>
        [JsonPropertyName("duration")] public long? Duration { get; set; }
    }

    /// <summary>
    /// Attributes for a label (tag) that can be assigned to components.
    /// </summary>
    /// <remarks>
    /// Labels are used to categorize and organize components in ArcGIS Monitor.
    /// They support color-coding for visual identification in the UI.
    /// </remarks>
    public sealed class LabelAttributes
    {
        /// <summary>Unique identifier for the label.</summary>
        [JsonPropertyName("id")] public int Id { get; set; }

        /// <summary>Date and time when the label was created (UTC).</summary>
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>Display name of the label.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }

        /// <summary>Description of the label.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }

        /// <summary>Color code for the label (e.g., "#FF5733").</summary>
        [JsonPropertyName("color")] public string? Color { get; set; }
    }

    /// <summary>
    /// Attributes for an ArcGIS Monitor agent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Agents are software components that collect monitoring data from components.
    /// They run on monitored machines and report data back to the Monitor server.
    /// </para>
    /// <para>
    /// Agents can connect:
    /// <list type="bullet">
    /// <item><description>Directly to the Monitor server</description></item>
    /// <item><description>Through another agent (specified by ThroughConnectionId)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class AgentAttributes
    {
        /// <summary>Unique identifier for the agent.</summary>
        [JsonPropertyName("id")] public int Id { get; set; }

        /// <summary>Date and time when the agent was registered (UTC).</summary>
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>Display name of the agent.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }

        /// <summary>Description of the agent.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }

        /// <summary>Version number of the agent software.</summary>
        [JsonPropertyName("version")] public string? Version { get; set; }

        /// <summary>Network address of the agent.</summary>
        [JsonPropertyName("address")] public string? Address { get; set; }

        /// <summary>Operating system platform (e.g., "Windows", "Linux").</summary>
        [JsonPropertyName("platform")] public string? Platform { get; set; }

        /// <summary>Indicates whether the agent is currently connected to the Monitor server.</summary>
        [JsonPropertyName("is_connected")] public bool? IsConnected { get; set; }

        /// <summary>ID of the connection through which this agent connects (null for direct connections).</summary>
        [JsonPropertyName("through_connection_id")] public int? ThroughConnectionId { get; set; }
    }

    /// <summary>
    /// Attributes for component log entries.
    /// </summary>
    /// <remarks>
    /// This class uses JsonExtensionData to handle log entry fields dynamically,
    /// as the structure may vary or be extended in future versions.
    /// </remarks>
    public sealed class ComponentLogAttributes
    {
        /// <summary>
        /// Additional properties from the JSON that aren't explicitly mapped.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
    }

    /// <summary>
    /// Attributes for observer configurations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Observers are monitoring modules that collect specific types of metrics.
    /// Examples include:
    /// <list type="bullet">
    /// <item><description>Metrics Observer - collects performance metrics</description></item>
    /// <item><description>Logs Observer - collects log entries</description></item>
    /// <item><description>Custom Observers - collect domain-specific data</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This class uses JsonExtensionData to handle observer fields dynamically,
    /// as different observer types may have different configuration properties.
    /// </para>
    /// </remarks>
    public sealed class ObserverAttributes
    {
        /// <summary>
        /// Additional properties from the JSON that aren't explicitly mapped.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
    }
}
