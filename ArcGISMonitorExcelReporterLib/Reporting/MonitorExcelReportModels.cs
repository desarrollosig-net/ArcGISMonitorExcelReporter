using ArcGISMonitorExcelReporterLib.Models;

namespace ArcGISMonitorExcelReporterLib.Reporting;

public sealed class MonitorReportRequest
{
    public List<string> CollectionNames { get; set; } = [];
    public List<string> ComponentTypes { get; set; } = [];
    public List<string> MetricNameLikes { get; set; } = [];
    public List<string> IncludeOnlyMetricNames { get; set; } = [];
    public List<string> ExcludeMetricNames { get; set; } = [];
    public bool AlertingOnOnly { get; set; }
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public int PageSize { get; set; } = 100;
    public string MetricBucket { get; set; } = "observed_at:15m";
    public bool IncludeMetricTimeSeries { get; set; } = true;
    public int? MaxMetricIdsForTimeSeries { get; set; } = 5000;
}

public sealed class MonitorExcelReport
{
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public List<CollectionReportRow> Collections { get; set; } = [];
    public List<ComponentReportRow> Components { get; set; } = [];
    public List<MetricReportRow> Metrics { get; set; } = [];
    public List<MetricDataReportRow> MetricData { get; set; } = [];
    public List<AlertReportRow> Alerts { get; set; } = [];
}

public sealed record CollectionReportRow(
    string CollectionName,
    string ComponentType,
    int ComponentCount,
    int MetricCount,
    int AlertCount);

public sealed class ComponentReportRow
{
    public string CollectionName { get; set; } = string.Empty;
    public int ComponentId { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Subtype { get; set; }
    public string? AddressInternal { get; set; }
    public string? State { get; set; }
    public int? Status { get; set; }
    public string? Version { get; set; }
    public double? MemoryTotal { get; set; }
    public int MetricCount { get; set; }
    public int AlertCount { get; set; }
}

public sealed class MetricReportRow
{
    public string CollectionName { get; set; } = string.Empty;
    public int ComponentId { get; set; }
    public string? ComponentName { get; set; }
    public string? ComponentType { get; set; }
    public string? ComponentSubtype { get; set; }
    public int MetricId { get; set; }
    public string? MetricName { get; set; }
    public string? RId { get; set; }
    public string? BaseRId { get; set; }
    public string? Unit { get; set; }
    public int? Status { get; set; }
    public bool? IsAlertingEnabled { get; set; }
    public string? Aggregation { get; set; }
    public string? Operator { get; set; }
    public double? InfoThreshold { get; set; }
    public double? WarningThreshold { get; set; }
    public double? CriticalThreshold { get; set; }
}

public sealed class MetricDataReportRow
{
    public string CollectionName { get; set; } = string.Empty;
    public int MetricId { get; set; }
    public string? MetricName { get; set; }
    public int? ComponentId { get; set; }
    public string? ComponentName { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public double? AvgValue { get; set; }
    public double? StdDevValue { get; set; }
    public double? Percentile95Value { get; set; }
    public double? SumValue { get; set; }
    public double? CountValue { get; set; }
}

public sealed class AlertReportRow
{
    public string CollectionName { get; set; } = string.Empty;
    public int AlertId { get; set; }
    public int? MetricId { get; set; }
    public string? MetricName { get; set; }
    public int? ComponentId { get; set; }
    public string? ComponentName { get; set; }
    public string? State { get; set; }
    public int? Status { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? Operator { get; set; }
    public double? InfoThreshold { get; set; }
    public double? WarningThreshold { get; set; }
    public double? CriticalThreshold { get; set; }
    public long? Duration { get; set; }
}

public static class MonitorReportMapper
{
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
                        Percentile95Value = d.Percentile95Value,
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
