using ArcGISMonitorExcelReporterLib.Models;

namespace ArcGISMonitorExcelReporterLib.Builders;

public static class MonitorQueryBuilders
{
    public static MonitoringQueryRequest CollectionComponents(
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
        var childIncludes = new List<IncludeSpec>();

        if (includeLogs && fromUtc.HasValue && toUtc.HasValue)
        {
            childIncludes.Add(new IncludeSpec
            {
                Resource = "components_logs",
                Where = BetweenTimestamp("logged_at", fromUtc.Value, toUtc.Value)
            });
        }

        if (includeLabels) childIncludes.Add(new IncludeSpec { Resource = "labels" });
        if (includeParents) childIncludes.Add(new IncludeSpec { Resource = "parents" });
        if (includeAgents) childIncludes.Add(new IncludeSpec { Resource = "agents" });
        if (includeMetricsObserver) childIncludes.Add(new IncludeSpec { Resource = "observers", Where = "name='Metrics'" });

        return CollectionRequest(collectionName, new IncludeSpec
        {
            Resource = "components",
            ReturnCountOnly = returnCountOnly,
            ResultRecordCount = resultRecordCount,
            ResultOffset = resultOffset,
            Where = $"type = '{EscapeSqlLiteral(componentType)}'",
            Including = childIncludes
        });
    }

    public static MonitoringQueryRequest CollectionComponentsWithAllMetrics(
        string collectionName,
        string componentType,
        bool returnCountOnly,
        int resultRecordCount = 100,
        int resultOffset = 0)
    {
        return CollectionRequest(collectionName, new IncludeSpec
        {
            Resource = "components",
            ReturnCountOnly = returnCountOnly,
            ResultRecordCount = resultRecordCount,
            ResultOffset = resultOffset,
            Where = $"type = '{EscapeSqlLiteral(componentType)}'",
            Including = [new IncludeSpec { Resource = "metrics" }]
        });
    }

    public static MonitoringQueryRequest CollectionComponentsByMetricName(
        string collectionName,
        string componentType,
        string metricNameLike,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        bool returnCountOnly,
        int resultRecordCount = 100,
        int resultOffset = 0)
    {
        return CollectionRequest(collectionName, new IncludeSpec
        {
            Resource = "components",
            ReturnCountOnly = returnCountOnly,
            ResultRecordCount = resultRecordCount,
            ResultOffset = resultOffset,
            Where = $"type = '{EscapeSqlLiteral(componentType)}'",
            Including =
            [
                new IncludeSpec
                {
                    Resource = "metrics",
                    Where = $"name like '{EscapeSqlLiteral(metricNameLike)}%'",
                    Including =
                    [
                        new IncludeSpec
                        {
                            Resource = "metrics_data",
                            Where = BetweenTimestamp("observed_at", fromUtc, toUtc),
                            GroupbyFieldsForStatistics = "metric_id",
                            OutStatistics =
                            [
                                new OutStatistic
                                {
                                    OnStatisticField = "value",
                                    StatisticType = ["count", "avg", "min", "max", "sum", "stddev"]
                                }
                            ]
                        },
                        new IncludeSpec
                        {
                            Resource = "alerts",
                            Where = AlertOverlapsWhere(fromUtc, toUtc)
                        }
                    ]
                },
                new IncludeSpec { Resource = "labels" },
                new IncludeSpec { Resource = "observers", Where = "name='Metrics'" }
            ]
        });
    }

    public static MonitoringQueryRequest MetricsTimeSeries(
        IEnumerable<int> metricIds,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        string bucket = "observed_at:15m")
    {
        var ids = string.Join(", ", metricIds.Distinct().OrderBy(x => x));
        if (string.IsNullOrWhiteSpace(ids))
            throw new ArgumentException("Debe indicar al menos un metricId.", nameof(metricIds));

        return new MonitoringQueryRequest
        {
            Where = $"id in ({ids})",
            Including =
            [
                new IncludeSpec
                {
                    Resource = "metrics_data",
                    Where = BetweenTimestamp("observed_at", fromUtc, toUtc),
                    GroupByFieldsForStatistics = ["metric_id", bucket],
                    OutStatistics =
                    [
                        new OutStatistic
                        {
                            OnStatisticField = "value",
                            StatisticType = ["avg", "max", "sum"]
                        }
                    ]
                }
            ]
        };
    }

    private static MonitoringQueryRequest CollectionRequest(string collectionName, IncludeSpec include)
    {
        return new MonitoringQueryRequest
        {
            Where = $"(name = '{EscapeSqlLiteral(collectionName)}')",
            Including = [include]
        };
    }

    public static string BetweenTimestamp(string fieldName, DateTimeOffset fromUtc, DateTimeOffset toUtc)
        => $"({fieldName} BETWEEN TIMESTAMP '{FormatMonitorTimestamp(fromUtc)}'  AND TIMESTAMP '{FormatMonitorTimestamp(toUtc)}')";

    public static string AlertOverlapsWhere(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        var from = FormatMonitorTimestamp(fromUtc);
        var to = FormatMonitorTimestamp(toUtc);
        return $"(opened_at <= TIMESTAMP '{from}' and closed_at >= TIMESTAMP '{from}') " +
               $"or (opened_at >= TIMESTAMP '{from}' and closed_at <= TIMESTAMP '{to}') " +
               $"or (opened_at <= TIMESTAMP '{to}' and closed_at >= TIMESTAMP '{to}') " +
               $"or (opened_at <= TIMESTAMP '{to}' and closed_at IS NULL)";
    }

    public static string FormatMonitorTimestamp(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'");

    private static string EscapeSqlLiteral(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);
}
