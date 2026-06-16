// Ignore Spelling: Dev

using ArcGISMonitorExcelReporterLib.Models;

using System.Linq;

namespace ArcGISMonitorExcelReporterLib.Reporting
{
    /// <summary>
    /// Request parameters for building a monitor report.
    /// </summary>
    public sealed class MonitorReportRequest
    {
        /// <summary>
        /// Gets or sets the ArcGIS Monitor server URL (used for display in the report).
        /// </summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IANA or Windows timezone ID used to resolve local dates.
        /// </summary>
        public string Timezone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the number of past days used to define the time range.
        /// </summary>
        public int PastDays { get; set; }

        /// <summary>
        /// Gets or sets the number of past hours used to define the time range.
        /// </summary>
        public int PastHours { get; set; }

        /// <summary>
        /// Gets or sets the list of collection names to query.
        /// </summary>
        public List<string> CollectionNames { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of component types to include in the report.
        /// </summary>
        public List<string> ComponentTypes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of metric name patterns for filtering (uses LIKE matching).
        /// </summary>
        public List<string> MetricNameLikes { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of exact metric names to include.
        /// </summary>
        public List<string> IncludeOnlyMetricNames { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of metric name patterns to exclude.
        /// </summary>
        public List<string> ExcludeMetricNames { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether to include only metrics with alerting enabled.
        /// </summary>
        public bool AlertingOnOnly { get; set; }

        /// <summary>
        /// Gets or sets the start of the time range (UTC).
        /// </summary>
        public DateTimeOffset FromUtc { get; set; }

        /// <summary>
        /// Gets or sets the end of the time range (UTC).
        /// </summary>
        public DateTimeOffset ToUtc { get; set; }

        /// <summary>
        /// Gets or sets the start of the time range (local time).
        /// </summary>
        public DateTimeOffset FromLocal { get; set; }

        /// <summary>
        /// Gets or sets the end of the time range (local time).
        /// </summary>
        public DateTimeOffset ToLocal { get; set; }

        /// <summary>
        /// Gets or sets the page size for paginated queries. Default is 100.
        /// </summary>
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the time bucket for metric aggregation. Default is "observed_at:15m".
        /// </summary>
        public string MetricBucket { get; set; } = "observed_at:15m";

        /// <summary>
        /// Gets or sets a value indicating whether to include time series data for metrics.
        /// </summary>
        public bool IncludeMetricTimeSeries { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of metric IDs to fetch time series for. Default is null (unlimited).
        /// </summary>
        public int? MaxMetricIdsForTimeSeries { get; set; } = null;
    }

    /// <summary>
    /// Contains the complete report data from ArcGIS Monitor, organized into normalized tables.
    /// </summary>
    public sealed class MonitorExcelReport
    {
        /// <summary>
        /// Gets or sets the ArcGIS Monitor server URL.
        /// </summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the queried collection name, or null if all collections were queried.
        /// </summary>
        public string? CollectionName { get; set; }

        /// <summary>
        /// Gets or sets the IANA or Windows timezone ID used for local date display.
        /// </summary>
        public string Timezone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the number of past days used to define the time range.
        /// </summary>
        public int PastDays { get; set; }

        /// <summary>
        /// Gets or sets the number of past hours used to define the time range.
        /// </summary>
        public int PastHours { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this report was generated (UTC).
        /// </summary>
        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the start of the time range covered by this report (UTC).
        /// </summary>
        public DateTimeOffset FromUtc { get; set; }

        /// <summary>
        /// Gets or sets the end of the time range covered by this report (UTC).
        /// </summary>
        public DateTimeOffset ToUtc { get; set; }

        /// <summary>
        /// Gets or sets the collection summary rows.
        /// </summary>
        public List<CollectionReportRow> Collections { get; set; } = [];

        /// <summary>
        /// Gets or sets the component inventory rows.
        /// </summary>
        public List<ComponentReportRow> Components { get; set; } = [];

        /// <summary>
        /// Gets or sets the metrics catalog rows.
        /// </summary>
        public List<MetricReportRow> Metrics { get; set; } = [];

        /// <summary>
        /// Gets or sets the time bucket interval used for metric time series data (e.g. "15m", "1h").
        /// Set after downsampling is applied.
        /// </summary>
        public string MetricDataBucket { get; set; } = "15m";

        /// <summary>
        /// Gets or sets the metric period-aggregate summary rows (one row per metric, from component queries).
        /// </summary>
        public List<MetricDataReportRow> MetricData { get; set; } = [];

        /// <summary>
        /// Gets or sets the downsampled metric time series rows (multi-point, from the dedicated time series API).
        /// </summary>
        public List<MetricDataReportRow> TimeSeriesMetricData { get; set; } = [];

        /// <summary>
        /// Gets or sets the alert rows.
        /// </summary>
        public List<AlertReportRow> Alerts { get; set; } = [];

        /// <summary>
        /// Gets or sets the unique agents found in components.
        /// </summary>
        public List<AgentReportRow> Agents { get; set; } = [];

        /// <summary>
        /// Gets or sets the unique labels found in components.
        /// </summary>
        public List<LabelReportRow> Labels { get; set; } = [];

        /// <summary>
        /// Gets or sets the ArcGIS Monitor information (version and available resources).
        /// </summary>
        public MonitoringInfo? MonitoringInfo { get; set; }

        /// <summary>
        /// Gets or sets the resource field information dictionary (resource name -> fields).
        /// </summary>
        public Dictionary<string, ResourceFieldInfo> ResourceFields { get; set; } = [];

        /// <summary>
        /// Gets or sets the component types information (available component types and their fields).
        /// </summary>
        public ComponentTypesInfo? ComponentTypes { get; set; }

        /// <summary>
        /// Gets or sets the total execution time of the report generation process.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Represents a summary row for a collection and component type combination.
    /// </summary>
    /// <param name="CollectionName">The name of the collection.</param>
    /// <param name="ComponentType">The type of component.</param>
    /// <param name="ComponentCount">The number of components in this collection/type.</param>
    /// <param name="MetricCount">The number of metrics associated with these components.</param>
    /// <param name="AlertCount">The number of alerts associated with these metrics.</param>
    public sealed record CollectionReportRow(
        string CollectionName,
        string ComponentType,
        int ComponentCount,
        int MetricCount,
        int AlertCount);

    /// <summary>
    /// Represents a component (host, service, database, etc.) in the monitor report.
    /// </summary>
    public sealed class ComponentReportRow
    {
        /// <summary>
        /// Gets or sets the name of the collection this component belongs to.
        /// </summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique component identifier.
        /// </summary>
        public long ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the component was created (UTC).
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the system-specific identifier for the component.
        /// </summary>
        public string? SystemId { get; set; }

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the component.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the component type (e.g., "host", "service", "database").
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the component subtype for additional classification.
        /// </summary>
        public string? Subtype { get; set; }

        /// <summary>
        /// Gets or sets the internal address (hostname, IP, URL, etc.).
        /// </summary>
        public string? AddressInternal { get; set; }

        /// <summary>
        /// Gets or sets the component state (e.g., "running", "stopped").
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets the component version.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the location of the component.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the component was started (UTC).
        /// </summary>
        public DateTimeOffset? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the classification of the component.
        /// </summary>
        public string? Class { get; set; }

        /// <summary>
        /// Gets or sets the CPU model name (for host components).
        /// </summary>
        public string? CpuName { get; set; }

        /// <summary>
        /// Gets or sets the CPU clock speed in GHz (for host components).
        /// </summary>
        public double? CpuSpeed { get; set; }

        /// <summary>
        /// Gets or sets the number of physical CPU cores (for host components).
        /// </summary>
        public int? CpuCoresPhysical { get; set; }

        /// <summary>
        /// Gets or sets the number of logical CPU cores/threads (for host components).
        /// </summary>
        public int? CpuCoresLogical { get; set; }

        /// <summary>
        /// Gets or sets the total memory in bytes.
        /// </summary>
        public double? MemoryTotal { get; set; }

        /// <summary>
        /// Gets or sets the total page/swap file size in MB (for host components).
        /// </summary>
        public double? MemoryPageTotal { get; set; }

        /// <summary>
        /// Gets or sets the network interface speed in Mbps (for host components).
        /// </summary>
        public int? NetworkSpeed { get; set; }

        /// <summary>
        /// Gets or sets the ID of the connection used to monitor this component.
        /// </summary>
        public long? ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the SSL/TLS certificate expires (UTC).
        /// </summary>
        public DateTimeOffset? CertExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the license expires (UTC).
        /// </summary>
        public DateTimeOffset? LicenseExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the total storage capacity in GB (for host components).
        /// </summary>
        public double? StorageTotal { get; set; }

        /// <summary>
        /// Gets or sets the geodatabase version (for database components).
        /// </summary>
        public string? GdbVersion { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of shared instances (for service components).
        /// </summary>
        public int? InstancesSharedMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of shared instances (for service components).
        /// </summary>
        public int? InstancesSharedMax { get; set; }

        /// <summary>
        /// Gets or sets the system mode configuration.
        /// </summary>
        public string? SystemMode { get; set; }

        /// <summary>
        /// Gets or sets the system state information.
        /// </summary>
        public string? SystemState { get; set; }

        /// <summary>
        /// Gets or sets the instance type (e.g., "shared", "dedicated") for service components.
        /// </summary>
        public string? InstanceType { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of instances (for service components).
        /// </summary>
        public int? InstancesMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of instances (for service components).
        /// </summary>
        public int? InstancesMax { get; set; }

        /// <summary>
        /// Gets or sets the maximum wait time in seconds (for service components).
        /// </summary>
        public int? WaitTimeMax { get; set; }

        /// <summary>
        /// Gets or sets the maximum idle time in seconds (for service components).
        /// </summary>
        public int? IdleTimeMax { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether caching is enabled (for service components).
        /// </summary>
        public bool? IsCached { get; set; }

        /// <summary>
        /// Gets or sets the geometry type (for feature services).
        /// </summary>
        public string? GeometryType { get; set; }

        /// <summary>
        /// Gets or sets the versioning type (for geodatabase services).
        /// </summary>
        public string? VersionedType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data is archived (for geodatabase components).
        /// </summary>
        public bool? IsArchived { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last modification (UTC).
        /// </summary>
        public DateTimeOffset? LastModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last backup (UTC) for database components.
        /// </summary>
        public DateTimeOffset? LastBackupAt { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections (for database components).
        /// </summary>
        public int? ConnectionsMax { get; set; }

        /// <summary>
        /// Gets or sets the number of metrics associated with this component.
        /// </summary>
        public int MetricCount { get; set; }

        /// <summary>
        /// Gets or sets the number of alerts associated with this component.
        /// </summary>
        public int AlertCount { get; set; }
    }

    /// <summary>
    /// Represents a metric definition and its alerting configuration.
    /// </summary>
    public sealed class MetricReportRow
    {
        /// <summary>
        /// Gets or sets the name of the collection this metric belongs to.
        /// </summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier this metric is associated with.
        /// </summary>
        public long ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string? ComponentName { get; set; }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        public string? ComponentType { get; set; }

        /// <summary>
        /// Gets or sets the component subtype.
        /// </summary>
        public string? ComponentSubtype { get; set; }

        /// <summary>
        /// Gets or sets the unique metric identifier.
        /// </summary>
        public long MetricId { get; set; }

        /// <summary>
        /// Gets or sets the metric name (e.g., "CPU Utilized", "Memory Available").
        /// </summary>
        public string? MetricName { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public string? RId { get; set; }

        /// <summary>
        /// Gets or sets the base resource identifier.
        /// </summary>
        public string? BaseRId { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement (e.g., "%", "MB", "count").
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Gets or sets the metric status code.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether alerting is enabled for this metric.
        /// </summary>
        public bool? IsAlertingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the aggregation method (e.g., "avg", "max", "min").
        /// </summary>
        public string? Aggregation { get; set; }

        /// <summary>
        /// Gets or sets the comparison operator for alerting (e.g., ">", "<", ">=").
        /// </summary>
        public string? Operator { get; set; }

        /// <summary>
        /// Gets or sets the informational threshold value.
        /// </summary>
        public double? InfoThreshold { get; set; }

        /// <summary>
        /// Gets or sets the warning threshold value.
        /// </summary>
        public double? WarningThreshold { get; set; }

        /// <summary>
        /// Gets or sets the critical threshold value.
        /// </summary>
        public double? CriticalThreshold { get; set; }

        /// <summary>
        /// Gets or sets the number of samples used for threshold evaluation.
        /// </summary>
        public int? Samples { get; set; }
    }

    /// <summary>
    /// Represents metric time series data or aggregated statistics.
    /// Includes min, max, avg, stddev, percentile 95, sum, and count.
    /// </summary>
    public sealed class MetricDataReportRow
    {
        /// <summary>
        /// Gets or sets the name of the collection this metric data belongs to.
        /// </summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metric identifier.
        /// </summary>
        public long MetricId { get; set; }

        /// <summary>
        /// Gets or sets the metric name.
        /// </summary>
        public string? MetricName { get; set; }

        /// <summary>
        /// Gets or sets the component identifier.
        /// </summary>
        public long? ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string? ComponentName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this data was observed (UTC).
        /// </summary>
        public DateTimeOffset? ObservedAt { get; set; }

        /// <summary>
        /// Gets or sets the minimum value in the aggregation period.
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value in the aggregation period.
        /// </summary>
        public double? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the average (mean) value in the aggregation period.
        /// </summary>
        public double? AvgValue { get; set; }

        /// <summary>
        /// Gets or sets the standard deviation, measuring variability.
        /// </summary>
        public double? StdDevValue { get; set; }

        /// <summary>
        /// Gets or sets the 95th percentile value calculated using exact normal distribution statistics, constrained by the maximum observed value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value is calculated using the formula: <c>P95 = min(μ + z₀.₉₅ × σ, max)</c>
        /// </para>
        /// <para>
        /// Where:
        /// <list type="bullet">
        /// <item><description>μ (mu) = <see cref="AvgValue"/> (mean)</description></item>
        /// <item><description>σ (sigma) = <see cref="StdDevValue"/> (standard deviation)</description></item>
        /// <item><description>z₀.₉₅ = 1.6448536269514722 (exact z-score for 95th percentile)</description></item>
        /// <item><description>max = <see cref="MaxValue"/> (maximum observed value)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The calculated P95 is constrained to never exceed <see cref="MaxValue"/>, ensuring
        /// statistical consistency with the actual data distribution. This is critical because:
        /// <list type="bullet">
        /// <item><description>The 95th percentile cannot logically exceed the 100th percentile (maximum)</description></item>
        /// <item><description>Real data may not perfectly follow a normal distribution</description></item>
        /// <item><description>Small sample sizes can lead to theoretical estimates exceeding observed maxima</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public double? Percentile95Value { get; set; }

        /// <summary>
        /// Gets or sets the sum of all values in the aggregation period.
        /// </summary>
        public double? SumValue { get; set; }

        /// <summary>
        /// Gets or sets the number of observations in the aggregation period.
        /// </summary>
        public double? CountValue { get; set; }
    }

    /// <summary>
    /// Represents an alert generated by ArcGIS Monitor when a metric threshold is breached.
    /// </summary>
    public sealed class AlertReportRow
    {
        /// <summary>
        /// Gets or sets the name of the collection this alert belongs to.
        /// </summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique alert identifier.
        /// </summary>
        public long AlertId { get; set; }

        /// <summary>
        /// Gets or sets the metric identifier that triggered this alert.
        /// </summary>
        public long? MetricId { get; set; }

        /// <summary>
        /// Gets or sets the metric name.
        /// </summary>
        public string? MetricName { get; set; }

        /// <summary>
        /// Gets or sets the component identifier where the alert occurred.
        /// </summary>
        public long? ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string? ComponentName { get; set; }

        /// <summary>
        /// Gets or sets the alert state (e.g., "open", "closed").
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the alert status code.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the alert was opened (UTC).
        /// </summary>
        public DateTimeOffset? OpenedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the alert was closed (UTC), or null if still open.
        /// </summary>
        public DateTimeOffset? ClosedAt { get; set; }

        /// <summary>
        /// Gets or sets the comparison operator used for the threshold check.
        /// </summary>
        public string? Operator { get; set; }

        /// <summary>
        /// Gets or sets the informational threshold value.
        /// </summary>
        public double? InfoThreshold { get; set; }

        /// <summary>
        /// Gets or sets the warning threshold value.
        /// </summary>
        public double? WarningThreshold { get; set; }

        /// <summary>
        /// Gets or sets the critical threshold value.
        /// </summary>
        public double? CriticalThreshold { get; set; }

        /// <summary>
        /// Gets or sets the duration of the alert in milliseconds.
        /// </summary>
        public long? Duration { get; set; }
    }

    /// <summary>
    /// Represents an agent (monitoring collector) from ArcGIS Monitor.
    /// </summary>
    public sealed class AgentReportRow
    {
        /// <summary>
        /// Gets or sets the unique agent identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets when the agent was created (UTC).
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the agent name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the agent description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the agent software version.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the network address of the agent.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the operating system platform (Windows, Linux, etc.).
        /// </summary>
        public string? Platform { get; set; }

        /// <summary>
        /// Gets or sets whether the agent is currently connected.
        /// </summary>
        public bool? IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the ID of the connection through which this agent connects (null for direct connections).
        /// </summary>
        public long? ThroughConnectionId { get; set; }
    }

    /// <summary>
    /// Represents a label (tag) assigned to components in ArcGIS Monitor.
    /// </summary>
    public sealed class LabelReportRow
    {
        /// <summary>
        /// Gets or sets the unique label identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets when the label was created (UTC).
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the label name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the label description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the color code for the label (e.g., #FF5733).
        /// </summary>
        public string? Color { get; set; }
    }

    /// <summary>
    /// Provides methods for mapping ArcGIS Monitor response data to report models.
    /// </summary>
    public static class MonitorReportMapper
    {
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
        /// double? p95 = MonitorReportMapper.CalculateNormalP95(mean, stdDev, max);
        /// // Result: 120.0 (capped at max, since 100 + 1.6448536269514722*15 = 124.67 > 120)
        /// </code>
        /// </example>
        public static double? CalculateNormalP95(double? mean, double? stdDev, double? maxValue)
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
        /// Adds components, metrics, metric data, and alerts from the Monitor API response to the report.
        /// </summary>
        /// <param name="report">The report to populate.</param>
        /// <param name="collectionName">The name of the collection being processed.</param>
        /// <param name="components">The component features from the API response.</param>
        public static void AddComponentTree(MonitorExcelReport report, string collectionName, IEnumerable<ComponentFeature> components)
        {
            var componentList = components.ToList();

            foreach(var component in componentList)
            {
                var c = component.Attributes;
                var metrics = component.Metrics ?? [];
                var alerts = metrics.SelectMany(m => m.Alerts ?? []).ToList();

                report.Components.Add(new ComponentReportRow
                {
                    CollectionName = collectionName,
                    ComponentId = c.Id,
                    CreatedAt = c.CreatedAt,
                    SystemId = c.SystemId,
                    Name = c.Name,
                    Description = c.Description,
                    Type = c.Type,
                    Subtype = c.Subtype,
                    AddressInternal = c.AddressInternal,
                    State = c.State,
                    Status = c.Status,
                    Version = c.Version,
                    Location = c.Location,
                    StartedAt = c.StartedAt,
                    Class = c.Class,
                    CpuName = c.CpuName,
                    CpuSpeed = c.CpuSpeed,
                    CpuCoresPhysical = c.CpuCoresPhysical,
                    CpuCoresLogical = c.CpuCoresLogical,
                    MemoryTotal = c.MemoryTotal,
                    MemoryPageTotal = c.MemoryPageTotal,
                    NetworkSpeed = c.NetworkSpeed,
                    ConnectionId = c.ConnectionId,
                    CertExpiresAt = c.CertExpiresAt,
                    LicenseExpiresAt = c.LicenseExpiresAt,
                    StorageTotal = c.StorageTotal,
                    GdbVersion = c.GdbVersion,
                    InstancesSharedMin = c.InstancesSharedMin,
                    InstancesSharedMax = c.InstancesSharedMax,
                    SystemMode = c.SystemMode,
                    SystemState = c.SystemState,
                    InstanceType = c.InstanceType,
                    InstancesMin = c.InstancesMin,
                    InstancesMax = c.InstancesMax,
                    WaitTimeMax = c.WaitTimeMax,
                    IdleTimeMax = c.IdleTimeMax,
                    IsCached = c.IsCached,
                    GeometryType = c.GeometryType,
                    VersionedType = c.VersionedType,
                    IsArchived = c.IsArchived,
                    LastModifiedAt = c.LastModifiedAt,
                    LastBackupAt = c.LastBackupAt,
                    ConnectionsMax = c.ConnectionsMax,
                    MetricCount = metrics.Count,
                    AlertCount = alerts.Count
                });

                foreach(var metric in metrics)
                {
                    var m = metric.Attributes;
                    report.Metrics.Add(new MetricReportRow
                    {
                        CollectionName = collectionName,
                        ComponentId = c.Id,
                        ComponentName = c.Name,
                        ComponentType = c.Type,
                        ComponentSubtype = c.Subtype,
                        MetricId = m.Id,
                        MetricName = m.Name,
                        RId = m.RId,
                        BaseRId = m.BaseRId,
                        Unit = m.Unit,
                        Status = m.Status,
                        IsAlertingEnabled = m.IsAlertingEnabled,
                        Aggregation = m.Aggregation,
                        Operator = m.Operator,
                        InfoThreshold = m.InfoThreshold,
                        WarningThreshold = m.WarningThreshold,
                        CriticalThreshold = m.CriticalThreshold,
                        Samples = m.Samples
                    });

                    report.MetricData.AddRange(from data in metric.MetricsData ?? []
                                               let d = data.Attributes// Calculate exact Percentile 95 using normal distribution formula: P95 = min(μ + z₀.₉₅ × σ, max)
                                                                      // Where z₀.₉₅ = 1.6448536269514722 (exact z-score for 95th percentile)
                                                                      // The result is capped at the maximum observed value to ensure statistical consistency
                                               let percentile95 = MonitorReportMapper.CalculateNormalP95(d.AvgValue, d.StdDevValue, d.MaxValue)
                                               select new MetricDataReportRow
                                               {
                                                   CollectionName = collectionName,
                                                   MetricId = d.MetricId ?? m.Id,
                                                   MetricName = m.Name,
                                                   ComponentId = c.Id,
                                                   ComponentName = c.Name,
                                                   ObservedAt = d.ObservedAt,
                                                   MinValue = d.MinValue,
                                                   MaxValue = d.MaxValue,
                                                   AvgValue = d.AvgValue,
                                                   StdDevValue = d.StdDevValue,
                                                   Percentile95Value = percentile95,
                                                   SumValue = d.SumValue,
                                                   CountValue = d.CountValue
                                               });

                    report.Alerts.AddRange(from alert in metric.Alerts ?? []
                                           let a = alert.Attributes
                                           select new AlertReportRow
                                           {
                                               CollectionName = collectionName,
                                               AlertId = a.Id,
                                               MetricId = a.MetricId ?? m.Id,
                                               MetricName = a.MetricName ?? m.Name,
                                               ComponentId = a.ComponentId ?? c.Id,
                                               ComponentName = a.ComponentName ?? c.Name,
                                               State = a.State,
                                               Status = a.Status,
                                               OpenedAt = a.OpenedAt,
                                               ClosedAt = a.ClosedAt,
                                               Operator = a.Operator,
                                               InfoThreshold = a.InfoThreshold,
                                               WarningThreshold = a.WarningThreshold,
                                               CriticalThreshold = a.CriticalThreshold,
                                               Duration = a.Duration
                                           });
                }
            }
        }

        /// <summary>
        /// Maps an agent feature to an AgentReportRow.
        /// </summary>
        /// <param name="agentFeature">The agent feature from the API response.</param>
        /// <returns>An AgentReportRow with all agent attributes.</returns>
        public static AgentReportRow MapAgentToRow(AttributeFeature<AgentAttributes> agentFeature)
        {
            var a = agentFeature.Attributes;
            return new AgentReportRow
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                Name = a.Name,
                Description = a.Description,
                Version = a.Version,
                Address = a.Address,
                Platform = a.Platform,
                IsConnected = a.IsConnected,
                ThroughConnectionId = a.ThroughConnectionId
            };
        }

        /// <summary>
        /// Maps a label feature to a LabelReportRow.
        /// </summary>
        /// <param name="labelFeature">The label feature from the API response.</param>
        /// <returns>A LabelReportRow with all label attributes.</returns>
        public static LabelReportRow MapLabelToRow(AttributeFeature<LabelAttributes> labelFeature)
        {
            var l = labelFeature.Attributes;
            return new LabelReportRow
            {
                Id = l.Id,
                CreatedAt = l.CreatedAt,
                Name = l.Name,
                Description = l.Description,
                Color = l.Color
            };
        }
    }
}
