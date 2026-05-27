using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models;

public static class MonitorJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
