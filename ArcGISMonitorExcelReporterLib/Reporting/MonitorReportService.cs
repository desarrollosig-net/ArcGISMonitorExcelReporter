using ArcGISMonitorExcelReporterLib.Client;
using ArcGISMonitorExcelReporterLib.Models;

namespace ArcGISMonitorExcelReporterLib.Reporting;

public sealed class MonitorReportService
{
    private readonly ArcGisMonitorQueryService _queries;

    public MonitorReportService(ArcGisMonitorQueryService queries)
    {
        _queries = queries;
    }

    public async Task<MonitorExcelReport> BuildReportAsync(
        MonitorReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CollectionNames.Count == 0)
            throw new ArgumentException("Debe especificar al menos una colección.", nameof(request));
        if (request.ComponentTypes.Count == 0)
            throw new ArgumentException("Debe especificar al menos un tipo de componente.", nameof(request));
        if (request.FromUtc >= request.ToUtc)
            throw new ArgumentException("FromUtc debe ser menor que ToUtc.", nameof(request));

        var report = new MonitorExcelReport
        {
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc
        };

        foreach (var collectionName in request.CollectionNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var componentType in request.ComponentTypes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var components = new List<ComponentFeature>();

                if (request.MetricNameLikes.Count == 0)
                {
                    components.AddRange(await _queries.GetComponentsWithAllMetricsAsync(
                        collectionName,
                        componentType,
                        request.PageSize,
                        cancellationToken).ConfigureAwait(false));
                }
                else
                {
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

                MonitorReportMapper.AddComponentTree(report, collectionName, components);

                report.Collections.Add(new CollectionReportRow(
                    collectionName,
                    componentType,
                    components.Count,
                    components.SelectMany(c => c.Metrics ?? []).Count(),
                    components.SelectMany(c => c.Metrics ?? []).SelectMany(m => m.Alerts ?? []).Count()));
            }
        }

        ApplyMetricFilters(report, request);

        if (request.IncludeMetricTimeSeries && report.Metrics.Count > 0)
        {
            var metricIds = report.Metrics
                .Select(m => m.MetricId)
                .Where(id => id > 0)
                .Distinct()
                .Take(request.MaxMetricIdsForTimeSeries ?? int.MaxValue)
                .ToList();

            if (metricIds.Count > 0)
            {
                var series = await _queries.GetMetricTimeSeriesAsync(
                    metricIds,
                    request.FromUtc,
                    request.ToUtc,
                    request.MetricBucket,
                    cancellationToken).ConfigureAwait(false);

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
                            CountValue = d.CountValue,
                            AvgValue = d.AvgValue,
                            MinValue = d.MinValue,
                            MaxValue = d.MaxValue,
                            SumValue = d.SumValue,
                            StdDevValue = d.StdDevValue
                        });
                    }
                }
            }
        }

        return report;
    }

    public async Task BuildAndSaveExcelAsync(
        MonitorReportRequest request,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var report = await BuildReportAsync(request, cancellationToken).ConfigureAwait(false);
        new MonitorExcelReportWriter().Save(report, outputPath);
    }


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

    private static string ResolveCollectionName(MonitorExcelReport report, int metricId)
    {
        return report.Metrics.FirstOrDefault(m => m.MetricId == metricId)?.CollectionName ?? string.Empty;
    }

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
