using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models;

public sealed class QueryResponse<TFeature>
{
    [JsonPropertyName("features")]
    public List<TFeature> Features { get; set; } = [];

    [JsonPropertyName("exceededTransferLimit")]
    public bool ExceededTransferLimit { get; set; }
}

public sealed class AttributeFeature<TAttributes>
{
    [JsonPropertyName("attributes")]
    public TAttributes Attributes { get; set; } = default!;
}

public sealed class CollectionFeature
{
    [JsonPropertyName("attributes")]
    public CollectionAttributes Attributes { get; set; } = new();

    [JsonPropertyName("components")]
    [JsonConverter(typeof(ComponentsResultJsonConverter))]
    public ComponentsResult Components { get; set; } = new();
}

public sealed class ComponentsResult
{
    public int? Count { get; set; }
    public List<ComponentFeature> Items { get; set; } = [];
}

public sealed class ComponentsResultJsonConverter : JsonConverter<ComponentsResult>
{
    public override ComponentsResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var result = new ComponentsResult();
            if (doc.RootElement.TryGetProperty("count", out var count) && count.ValueKind == JsonValueKind.Number)
                result.Count = count.GetInt32();
            return result;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var items = JsonSerializer.Deserialize<List<ComponentFeature>>(ref reader, options) ?? [];
            return new ComponentsResult { Items = items };
        }

        if (reader.TokenType == JsonTokenType.Null)
            return new ComponentsResult();

        throw new JsonException($"No se puede convertir token {reader.TokenType} a ComponentsResult.");
    }

    public override void Write(Utf8JsonWriter writer, ComponentsResult value, JsonSerializerOptions options)
    {
        if (value.Items.Count > 0)
        {
            JsonSerializer.Serialize(writer, value.Items, options);
            return;
        }

        writer.WriteStartObject();
        if (value.Count.HasValue)
            writer.WriteNumber("count", value.Count.Value);
        writer.WriteEndObject();
    }
}

public sealed class ComponentFeature
{
    [JsonPropertyName("attributes")]
    public ComponentAttributes Attributes { get; set; } = new();

    [JsonPropertyName("metrics")]
    public List<MetricFeature>? Metrics { get; set; }

    [JsonPropertyName("labels")]
    public List<AttributeFeature<LabelAttributes>>? Labels { get; set; }

    [JsonPropertyName("parents")]
    public List<AttributeFeature<ComponentAttributes>>? Parents { get; set; }

    [JsonPropertyName("agents")]
    public List<AttributeFeature<AgentAttributes>>? Agents { get; set; }

    [JsonPropertyName("components_logs")]
    public List<AttributeFeature<ComponentLogAttributes>>? ComponentLogs { get; set; }

    [JsonPropertyName("observers")]
    public List<AttributeFeature<ObserverAttributes>>? Observers { get; set; }
}

public sealed class MetricFeature
{
    [JsonPropertyName("attributes")]
    public MetricAttributes Attributes { get; set; } = new();

    [JsonPropertyName("metrics_data")]
    public List<AttributeFeature<MetricDataAttributes>>? MetricsData { get; set; }

    [JsonPropertyName("alerts")]
    public List<AttributeFeature<AlertAttributes>>? Alerts { get; set; }
}

public sealed class CollectionAttributes
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("is_service_enabled")] public bool? IsServiceEnabled { get; set; }
    [JsonPropertyName("is_security_enabled")] public bool? IsSecurityEnabled { get; set; }
    [JsonPropertyName("expression")] public ResourceExpression? Expression { get; set; }
    [JsonPropertyName("service_url")] public string? ServiceUrl { get; set; }
}

public sealed class ResourceExpression
{
    [JsonPropertyName("resource")]
    public string? Resource { get; set; }
}

public sealed class ComponentAttributes
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("system_id")] public string? SystemId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("subtype")] public string? Subtype { get; set; }
    [JsonPropertyName("address_internal")] public string? AddressInternal { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("connection_id")] public int? ConnectionId { get; set; }
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("location")] public string? Location { get; set; }
    [JsonPropertyName("started_at")] public DateTimeOffset? StartedAt { get; set; }
    [JsonPropertyName("cert_expires_at")] public DateTimeOffset? CertExpiresAt { get; set; }
    [JsonPropertyName("license_expires_at")] public DateTimeOffset? LicenseExpiresAt { get; set; }
    [JsonPropertyName("memory_total")] public double? MemoryTotal { get; set; }
    [JsonPropertyName("class")] public string? Class { get; set; }
    [JsonPropertyName("cpu_name")] public string? CpuName { get; set; }
    [JsonPropertyName("cpu_speed")] public double? CpuSpeed { get; set; }
    [JsonPropertyName("cpu_cores_physical")] public int? CpuCoresPhysical { get; set; }
    [JsonPropertyName("cpu_cores_logical")] public int? CpuCoresLogical { get; set; }
    [JsonPropertyName("network_speed")] public int? NetworkSpeed { get; set; }
    [JsonPropertyName("memory_page_total")] public double? MemoryPageTotal { get; set; }
    [JsonPropertyName("storage_total")] public double? StorageTotal { get; set; }
    [JsonPropertyName("gdb_version")] public string? GdbVersion { get; set; }
    [JsonPropertyName("instances_shared_min")] public int? InstancesSharedMin { get; set; }
    [JsonPropertyName("instances_shared_max")] public int? InstancesSharedMax { get; set; }
    [JsonPropertyName("system_mode")] public string? SystemMode { get; set; }
    [JsonPropertyName("system_state")] public string? SystemState { get; set; }
    [JsonPropertyName("instance_type")] public string? InstanceType { get; set; }
    [JsonPropertyName("instances_min")] public int? InstancesMin { get; set; }
    [JsonPropertyName("instances_max")] public int? InstancesMax { get; set; }
    [JsonPropertyName("wait_time_max")] public int? WaitTimeMax { get; set; }
    [JsonPropertyName("idle_time_max")] public int? IdleTimeMax { get; set; }
    [JsonPropertyName("is_cached")] public bool? IsCached { get; set; }
    [JsonPropertyName("geometry_type")] public string? GeometryType { get; set; }
    [JsonPropertyName("versioned_type")] public string? VersionedType { get; set; }
    [JsonPropertyName("is_archived")] public bool? IsArchived { get; set; }
    [JsonPropertyName("last_modified_at")] public DateTimeOffset? LastModifiedAt { get; set; }
    [JsonPropertyName("last_backup_at")] public DateTimeOffset? LastBackupAt { get; set; }
    [JsonPropertyName("connections_max")] public int? ConnectionsMax { get; set; }
}

public sealed class MetricAttributes
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("r_id")] public string? RId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("base_r_id")] public string? BaseRId { get; set; }
    [JsonPropertyName("component_id")] public int? ComponentId { get; set; }
    [JsonPropertyName("observer_id")] public int? ObserverId { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("is_alerting_enabled")] public bool? IsAlertingEnabled { get; set; }
    [JsonPropertyName("aggregation")] public string? Aggregation { get; set; }
    [JsonPropertyName("operator")] public string? Operator { get; set; }
    [JsonPropertyName("samples")] public int? Samples { get; set; }
    [JsonPropertyName("info_threshold")] public double? InfoThreshold { get; set; }
    [JsonPropertyName("warning_threshold")] public double? WarningThreshold { get; set; }
    [JsonPropertyName("critical_threshold")] public double? CriticalThreshold { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("unit")] public string? Unit { get; set; }
    [JsonPropertyName("component_name")] public string? ComponentName { get; set; }
    [JsonPropertyName("component_address_internal")] public string? ComponentAddressInternal { get; set; }
    [JsonPropertyName("component_type")] public string? ComponentType { get; set; }
    [JsonPropertyName("component_subtype")] public string? ComponentSubtype { get; set; }
}

public sealed class MetricDataAttributes
{
    [JsonPropertyName("metric_id")] public int? MetricId { get; set; }
    [JsonPropertyName("observed_at")] public DateTimeOffset? ObservedAt { get; set; }
    [JsonPropertyName("COUNT_value")] public double? CountValue { get; set; }
    [JsonPropertyName("AVG_value")] public double? AvgValue { get; set; }
    [JsonPropertyName("MIN_value")] public double? MinValue { get; set; }
    [JsonPropertyName("MAX_value")] public double? MaxValue { get; set; }
    [JsonPropertyName("SUM_value")] public double? SumValue { get; set; }
    [JsonPropertyName("STDDEV_value")] public double? StdDevValue { get; set; }
    [JsonPropertyName("PERCENTILE_95_value")] public double? Percentile95Value { get; set; }
}

public sealed class AlertAttributes
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("opened_at")] public DateTimeOffset? OpenedAt { get; set; }
    [JsonPropertyName("closed_at")] public DateTimeOffset? ClosedAt { get; set; }
    [JsonPropertyName("metric_id")] public int? MetricId { get; set; }
    [JsonPropertyName("observer_id")] public int? ObserverId { get; set; }
    [JsonPropertyName("component_id")] public int? ComponentId { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("aggregation")] public string? Aggregation { get; set; }
    [JsonPropertyName("operator")] public string? Operator { get; set; }
    [JsonPropertyName("samples")] public int? Samples { get; set; }
    [JsonPropertyName("info_threshold")] public double? InfoThreshold { get; set; }
    [JsonPropertyName("warning_threshold")] public double? WarningThreshold { get; set; }
    [JsonPropertyName("critical_threshold")] public double? CriticalThreshold { get; set; }
    [JsonPropertyName("component_name")] public string? ComponentName { get; set; }
    [JsonPropertyName("component_address_internal")] public string? ComponentAddressInternal { get; set; }
    [JsonPropertyName("component_type")] public string? ComponentType { get; set; }
    [JsonPropertyName("component_subtype")] public string? ComponentSubtype { get; set; }
    [JsonPropertyName("metric_name")] public string? MetricName { get; set; }
    [JsonPropertyName("metric_r_id")] public string? MetricRId { get; set; }
    [JsonPropertyName("metric_unit")] public string? MetricUnit { get; set; }
    [JsonPropertyName("duration")] public long? Duration { get; set; }
}

public sealed class LabelAttributes
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("color")] public string? Color { get; set; }
}

public sealed class AgentAttributes
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("platform")] public string? Platform { get; set; }
    [JsonPropertyName("is_connected")] public bool? IsConnected { get; set; }
    [JsonPropertyName("through_connection_id")] public int? ThroughConnectionId { get; set; }
}

public sealed class ComponentLogAttributes
{
    // En el SAZ analizado la colección components_logs aparece vacía. Se deja JsonExtensionData para tolerar campos futuros.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class ObserverAttributes
{
    // En el SAZ analizado observers aparece vacío. Se deja JsonExtensionData para tolerar campos futuros.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
