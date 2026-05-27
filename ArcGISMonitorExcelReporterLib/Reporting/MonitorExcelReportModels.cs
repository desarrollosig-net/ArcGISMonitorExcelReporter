using ArcGISMonitorExcelReporterLib.Models;

namespace ArcGISMonitorExcelReporterLib.Reporting;

/// <summary>
/// Request parameters for building a monitor report.
/// </summary>
public sealed class MonitorReportRequest
{
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
    /// Gets or sets the maximum number of metric IDs to fetch time series for. Default is 5000.
    /// </summary>
    public int? MaxMetricIdsForTimeSeries { get; set; } = 5000;
}

/// <summary>
/// Contains the complete report data from ArcGIS Monitor, organized into normalized tables.
/// </summary>
public sealed class MonitorExcelReport
{
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
    /// Gets or sets the metric time series or aggregated data rows.
    /// </summary>
    public List<MetricDataReportRow> MetricData { get; set; } = [];

    /// <summary>
    /// Gets or sets the alert rows.
    /// </summary>
    public List<AlertReportRow> Alerts { get; set; } = [];
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
    public int ComponentId { get; set; }

    /// <summary>
    /// Gets or sets the component name.
    /// </summary>
    public string? Name { get; set; }

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
    /// Gets or sets the total memory in bytes.
    /// </summary>
    public double? MemoryTotal { get; set; }

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
    public int ComponentId { get; set; }

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
    public int MetricId { get; set; }

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
    public int MetricId { get; set; }

    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string? MetricName { get; set; }

    /// <summary>
    /// Gets or sets the component identifier.
    /// </summary>
    public int? ComponentId { get; set; }

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
    /// Gets or sets the 95th percentile value (if supported by ArcGIS Monitor).
    /// </summary>
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
    public int AlertId { get; set; }

    /// <summary>
    /// Gets or sets the metric identifier that triggered this alert.
    /// </summary>
    public int? MetricId { get; set; }

    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string? MetricName { get; set; }

    /// <summary>
    /// Gets or sets the component identifier where the alert occurred.
    /// </summary>
    public int? ComponentId { get; set; }

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
/// Provides methods for mapping ArcGIS Monitor response data to report models.
/// </summary>
public static class MonitorReportMapper
{
    /// <summary>
    /// Adds components, metrics, metric data, and alerts from the Monitor API response to the report.
    /// </summary>
    /// <param name="report">The report to populate.</param>
    /// <param name="collectionName">The name of the collection being processed.</param>
    /// <param name="components">The component features from the API response.</param>
    public static void AddComponentTree(MonitorExcelReport report, string collectionName, IEnumerable<ComponentFeature> components)
    {
        var componentList = components.ToList();

        foreach (var component in componentList)
        {
            var c = component.Attributes;
            var metrics = component.Metrics ?? [];
            var alerts = metrics.SelectMany(m => m.Alerts ?? []).ToList();

            report.Components.Add(new ComponentReportRow
            {
                CollectionName = collectionName,
                ComponentId = c.Id,
                Name = c.Name,
                Type = c.Type,
                Subtype = c.Subtype,
                AddressInternal = c.AddressInternal,
                State = c.State,
                Status = c.Status,
                Version = c.Version,
                MemoryTotal = c.MemoryTotal,
                MetricCount = metrics.Count,
                AlertCount = alerts.Count
            });

            foreach (var metric in metrics)
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
                    CriticalThreshold = m.CriticalThreshold
                });

                foreach (var data in metric.MetricsData ?? [])
                {
                    var d = data.Attributes;

                    // Calculate Percentile 95: avg + 1.645 * stddev (if count >= 30, otherwise null)
                    double? percentile95 = null;
                    if (d.CountValue.HasValue && d.CountValue.Value >= 30 && 
                        d.AvgValue.HasValue && d.StdDevValue.HasValue)
                    {
                        percentile95 = d.AvgValue.Value + (1.645 * d.StdDevValue.Value);
                    }

                    report.MetricData.Add(new MetricDataReportRow
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
                }

                foreach (var alert in metric.Alerts ?? [])
                {
                    var a = alert.Attributes;
                    report.Alerts.Add(new AlertReportRow
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
    }
}
