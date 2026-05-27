using ArcGISMonitorExcelReporterLib.Builders;
using ArcGISMonitorExcelReporterLib.Models;

namespace ArcGISMonitorExcelReporterLib.Client;

public sealed class ArcGisMonitorQueryService
{
    private readonly ArcGisMonitorClient _client;

    public ArcGisMonitorQueryService(ArcGisMonitorClient client)
    {
        _client = client;
    }

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

        for (var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
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

            if (total == 0)
                break;
        }

        return components;
    }

    public async Task<List<ComponentFeature>> GetComponentsWithAllMetricsAsync(
        string collectionName,
        string componentType,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var countRequest = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(collectionName, componentType, true, pageSize, 0);
        var countResponse = await _client.QueryCollectionsAsync(countRequest, cancellationToken).ConfigureAwait(false);
        var total = countResponse.Features.FirstOrDefault()?.Components.Count ?? 0;
        var components = new List<ComponentFeature>(Math.Max(total, 0));

        for (var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
        {
            var request = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(collectionName, componentType, false, pageSize, offset);
            var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
            components.AddRange(response.Features.SelectMany(f => f.Components.Items));

            if (total == 0)
                break;
        }

        return components;
    }

    public async Task<List<ComponentFeature>> GetComponentsWithMetricStatsAsync(
        string collectionName,
        string componentType,
        string metricNameLike,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var countRequest = MonitorQueryBuilders.CollectionComponentsByMetricName(
            collectionName, componentType, metricNameLike, fromUtc, toUtc, true, pageSize, 0);

        var countResponse = await _client.QueryCollectionsAsync(countRequest, cancellationToken).ConfigureAwait(false);
        var total = countResponse.Features.FirstOrDefault()?.Components.Count ?? 0;
        var components = new List<ComponentFeature>(Math.Max(total, 0));

        for (var offset = 0; offset < Math.Max(total, 1); offset += pageSize)
        {
            var request = MonitorQueryBuilders.CollectionComponentsByMetricName(
                collectionName, componentType, metricNameLike, fromUtc, toUtc, false, pageSize, offset);

            var response = await _client.QueryCollectionsAsync(request, cancellationToken).ConfigureAwait(false);
            components.AddRange(response.Features.SelectMany(f => f.Components.Items));

            if (total == 0)
                break;
        }

        return components;
    }

    public async Task<QueryResponse<MetricFeature>> GetMetricTimeSeriesAsync(
        IEnumerable<int> metricIds,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        string bucket = "observed_at:15m",
        CancellationToken cancellationToken = default)
    {
        var request = MonitorQueryBuilders.MetricsTimeSeries(metricIds, fromUtc, toUtc, bucket);
        return await _client.QueryMetricsAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
