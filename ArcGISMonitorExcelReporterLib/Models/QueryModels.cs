using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models;

public sealed class MonitoringQueryRequest
{
    [JsonPropertyName("where")]
    public string? Where { get; set; }

    [JsonPropertyName("including")]
    public List<IncludeSpec>? Including { get; set; }
}

public sealed class IncludeSpec
{
    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    [JsonPropertyName("where")]
    public string? Where { get; set; }

    [JsonPropertyName("returnCountOnly")]
    public bool? ReturnCountOnly { get; set; }

    [JsonPropertyName("resultRecordCount")]
    public int? ResultRecordCount { get; set; }

    [JsonPropertyName("resultOffset")]
    public int? ResultOffset { get; set; }

    [JsonPropertyName("including")]
    public List<IncludeSpec>? Including { get; set; }

    // Usado por /collections/query en el SAZ: groupbyFieldsForStatistics como string.
    [JsonPropertyName("groupbyFieldsForStatistics")]
    public string? GroupbyFieldsForStatistics { get; set; }

    // Usado por /metrics/query en el SAZ: groupByFieldsForStatistics como arreglo.
    [JsonPropertyName("groupByFieldsForStatistics")]
    public List<string>? GroupByFieldsForStatistics { get; set; }

    [JsonPropertyName("outStatistics")]
    public List<OutStatistic>? OutStatistics { get; set; }
}

public sealed class OutStatistic
{
    [JsonPropertyName("statisticType")]
    public List<string> StatisticType { get; set; } = [];

    [JsonPropertyName("onStatisticField")]
    public string OnStatisticField { get; set; } = "value";
}
