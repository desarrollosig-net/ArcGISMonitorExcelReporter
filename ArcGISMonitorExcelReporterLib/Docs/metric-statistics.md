# Metric Statistics

## Overview

The ArcGIS Monitor Excel Reporter retrieves comprehensive statistical data for each metric. All requested statistics are included in the `Metric_Data` sheet of the generated Excel report.

## Available Statistics

The following statistics are collected for each metric:

| Statistic | Column Name | Description |
|-----------|-------------|-------------|
| **Min** | `MinValue` | Minimum value observed in the time period |
| **Max** | `MaxValue` | Maximum value observed in the time period |
| **Avg** | `AvgValue` | Average (mean) value across all observations |
| **StdDev** | `StdDevValue` | Standard deviation, measuring variability |
| **Percentile 95** | `Percentile95Value` | 95th percentile value (if available from ArcGIS Monitor) |
| **Sum** | `SumValue` | Sum of all values in the time period |
| **Count** | `CountValue` | Number of observations/data points |

## Column Order in Excel

The columns appear in the Excel output in this logical order:

1. `CollectionName` - Collection the metric belongs to
2. `MetricId` - Unique metric identifier
3. `MetricName` - Metric name
4. `ComponentId` - Component identifier
5. `ComponentName` - Component name
6. `ObservedAt` - Timestamp of observation
7. **`MinValue`** - Minimum value
8. **`MaxValue`** - Maximum value
9. **`AvgValue`** - Average value
10. **`StdDevValue`** - Standard deviation
11. **`Percentile95Value`** - 95th percentile
12. **`SumValue`** - Sum
13. **`CountValue`** - Count

## Query Configuration

The statistics are requested from ArcGIS Monitor using the `outStatistics` parameter:

```json
{
  "outStatistics": [
    {
      "statisticType": ["count", "min", "max", "avg", "stddev", "percentile_95", "sum"],
      "onStatisticField": "value"
    }
  ]
}
```

### Component Queries
When querying components with metrics by name pattern, the query includes:
- `count`, `min`, `max`, `avg`, `stddev`, `percentile_95`, `sum`
- Grouped by `metric_id`

### Time Series Queries
When fetching metric time series data, the query includes:
- `count`, `min`, `max`, `avg`, `stddev`, `percentile_95`, `sum`
- Grouped by `metric_id` and time bucket (e.g., `observed_at:15m`)

## Percentile 95 Notes

**What is Percentile 95?**
The 95th percentile (P95) is a statistical measure indicating that 95% of all observations fall below this value. It's useful for:
- Identifying outliers and peak usage patterns
- Setting capacity planning thresholds
- Understanding worst-case scenarios while ignoring the top 5% extremes

**Availability:**
- If ArcGIS Monitor supports and returns the `PERCENTILE_95_value` field, it will appear in the report
- If not supported or not returned by the server, the `Percentile95Value` column will be empty (null)
- This is a server-side calculation; the client library doesn't calculate it locally

## Example Use Cases

### Performance Analysis
- **Min/Max**: Identify lowest and highest resource utilization
- **Avg**: Understand typical resource consumption
- **StdDev**: Measure consistency (low = stable, high = variable)
- **P95**: Plan for peak capacity needs

### Capacity Planning
- **Max**: Determine absolute peak usage
- **P95**: Plan for expected peaks (ignoring rare spikes)
- **Avg**: Understand normal operating levels

### Alerting Thresholds
- **Avg + StdDev**: Set dynamic thresholds based on typical behavior
- **P95**: Set warning thresholds for high usage
- **Max**: Set critical thresholds for absolute limits

## Example Data

Sample output from the `Metric_Data` sheet:

| MetricName | ObservedAt | MinValue | MaxValue | AvgValue | StdDevValue | Percentile95Value | SumValue | CountValue |
|------------|------------|----------|----------|----------|-------------|-------------------|----------|------------|
| CPU Utilized | 2025-01-08 10:00 | 15.2 | 89.5 | 45.8 | 12.3 | 78.2 | 9160 | 200 |
| Memory Utilized | 2025-01-08 10:00 | 42.1 | 95.8 | 68.5 | 8.7 | 91.2 | 13700 | 200 |
| Network Incoming | 2025-01-08 10:00 | 1.2 | 245.8 | 52.3 | 45.6 | 198.5 | 10460 | 200 |

## Implementation Details

### Response Model
The `MetricDataAttributes` class captures the statistics from ArcGIS Monitor:

```csharp
public sealed class MetricDataAttributes
{
    [JsonPropertyName("metric_id")] public int? MetricId { get; set; }
    [JsonPropertyName("observed_at")] public DateTimeOffset? ObservedAt { get; set; }
    [JsonPropertyName("COUNT_value")] public double? CountValue { get; set; }
    [JsonPropertyName("MIN_value")] public double? MinValue { get; set; }
    [JsonPropertyName("MAX_value")] public double? MaxValue { get; set; }
    [JsonPropertyName("AVG_value")] public double? AvgValue { get; set; }
    [JsonPropertyName("STDDEV_value")] public double? StdDevValue { get; set; }
    [JsonPropertyName("PERCENTILE_95_value")] public double? Percentile95Value { get; set; }
    [JsonPropertyName("SUM_value")] public double? SumValue { get; set; }
}
```

### Report Model
The `MetricDataReportRow` class structures the data for Excel output:

```csharp
public sealed class MetricDataReportRow
{
    public string CollectionName { get; set; }
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
```

## Excel Formatting

In the generated Excel file:
- All numeric values use appropriate number formatting
- Date/time values use ISO format: `yyyy-mm-dd hh:mm:ss`
- Null values appear as empty cells
- Column headers use the property names from the model
- The sheet supports Excel's table features (filtering, sorting)

## Troubleshooting

### Missing Percentile 95 Values
If the `Percentile95Value` column is empty:
1. Verify your ArcGIS Monitor version supports percentile statistics
2. Check the Monitor API documentation for supported statistic types
3. Review the logs for any warnings about unsupported statistics
4. The field may be server-specific; older versions might not support it

### Unexpected Values
- **All nulls**: No data available for the time range
- **Zero counts**: No observations recorded
- **NaN or Infinity**: Mathematical error in calculations (division by zero, etc.)

## References

- [ArcGIS Monitor API Documentation](https://developers.arcgis.com/monitor/)
- [Excel Export Details](excel-export.md)
- [Configuration Guide](configuration.md)
