# Query All Collections Feature

## Overview

The ArcGIS Monitor Excel Reporter now supports querying components from **all collections** without specifying individual collection names. This is useful when you want a comprehensive report across your entire ArcGIS Monitor deployment.

## How to Use

### Option 1: Empty Collection List
```csharp
var request = new MonitorReportRequest
{
    CollectionNames = [],  // Empty list = all collections
    ComponentTypes = ["host"],
    FromUtc = DateTimeOffset.UtcNow.AddDays(-7),
    ToUtc = DateTimeOffset.UtcNow
};
```

### Option 2: Wildcard "*"
```csharp
var request = new MonitorReportRequest
{
    CollectionNames = ["*"],  // Wildcard = all collections
    ComponentTypes = ["host", "service"],
    FromUtc = DateTimeOffset.UtcNow.AddDays(-7),
    ToUtc = DateTimeOffset.UtcNow
};
```

### Option 3: Empty String
```csharp
var request = new MonitorReportRequest
{
    CollectionNames = [""],  // Empty string = all collections
    ComponentTypes = ["host"],
    FromUtc = DateTimeOffset.UtcNow.AddDays(-7),
    ToUtc = DateTimeOffset.UtcNow
};
```

---

## JSON Configuration

In your JSON configuration file:

### Query All Collections
```json
{
  "server": {
    "baseUrl": "https://monitor.example.com:30443",
    "username": "admin",
    "password": "password123"
  },
  "report": {
    "collectionNames": ["*"],
    "componentTypes": ["host", "service", "database"],
    "fromUtc": "2025-01-20T00:00:00Z",
    "toUtc": "2025-01-27T00:00:00Z",
    "metricNameLikes": ["CPU", "Memory", "Disk"]
  }
}
```

### Query Specific Collections
```json
{
  "report": {
    "collectionNames": ["Production", "Staging"],
    "componentTypes": ["host"],
    "fromUtc": "2025-01-20T00:00:00Z",
    "toUtc": "2025-01-27T00:00:00Z"
  }
}
```

---

## Behavior

When querying all collections:

| Scenario | Collection Filter | Behavior |
|----------|------------------|----------|
| `CollectionNames = []` | None | Queries **all** collections |
| `CollectionNames = ["*"]` | None | Queries **all** collections |
| `CollectionNames = [""]` | None | Queries **all** collections |
| `CollectionNames = ["Production"]` | `name = 'Production'` | Queries only "Production" |
| `CollectionNames = ["Production", "Staging"]` | `name = 'Production'` OR `name = 'Staging'` | Queries multiple collections |

---

## API Query Details

### Before (Specific Collection)
```sql
-- SQL WHERE clause in API request
WHERE name = 'Production'
```

### After (All Collections)
```sql
-- No WHERE clause for collection filtering
-- All collections are returned
```

---

## Excel Report Output

When querying all collections, the Excel report:

- **Summary Sheet**: Shows "All Collections" as the collection name
- **Components Sheet**: Groups components by their actual collection name
- **Metrics Sheets**: Organized by component type (e.g., `Components_host`, `Components_service`)
- **Alerts Sheet**: Includes collection name for each alert

### Example Summary Sheet

| Collection | Component Type | Components | Metrics | Alerts |
|------------|---------------|------------|---------|--------|
| All Collections | host | 45 | 450 | 12 |
| All Collections | service | 23 | 184 | 5 |

---

## Performance Considerations

### ⚠️ Large Deployments
When querying **all collections** in large ArcGIS Monitor deployments:

1. **Increase Page Size**: Use `pageSize: 500` for better performance
2. **Limit Metrics**: Specify `metricNameLikes` to filter only needed metrics
3. **Disable Time Series**: Set `includeMetricTimeSeries: false` if not needed
4. **Increase Timeout**: Set `timeoutSeconds: 600` or higher for large queries

### Example: Optimized Configuration
```csharp
var request = new MonitorReportRequest
{
    CollectionNames = ["*"],  // All collections
    ComponentTypes = ["host"],  // Only hosts
    MetricNameLikes = ["CPU", "Memory"],  // Only CPU and Memory metrics
    FromUtc = DateTimeOffset.UtcNow.AddDays(-1),  // Last 24 hours
    ToUtc = DateTimeOffset.UtcNow,
    PageSize = 500,  // Larger page size
    IncludeMetricTimeSeries = false  // Skip time series data
};
```

---

## Use Cases

### 1. Enterprise Dashboard
Create a single report for all monitored systems across all collections.

```csharp
var request = new MonitorReportRequest
{
    CollectionNames = ["*"],
    ComponentTypes = ["host", "service", "database"],
    MetricNameLikes = ["CPU", "Memory", "Disk"],
    FromUtc = DateTimeOffset.UtcNow.AddDays(-30),
    ToUtc = DateTimeOffset.UtcNow,
    AlertingOnOnly = true  // Only metrics with alerting enabled
};
```

### 2. Health Check Report
Monitor critical metrics across all collections.

```csharp
var request = new MonitorReportRequest
{
    CollectionNames = [],  // All collections
    ComponentTypes = ["host"],
    MetricNameLikes = ["CPU Utilized", "Memory Available"],
    IncludeOnlyMetricNames = ["CPU Utilized", "Memory Available"],
    FromUtc = DateTimeOffset.UtcNow.AddHours(-1),
    ToUtc = DateTimeOffset.UtcNow
};
```

### 3. Audit Report
Generate a comprehensive inventory of all monitored components.

```csharp
var request = new MonitorReportRequest
{
    CollectionNames = ["*"],
    ComponentTypes = ["host", "service", "database", "webserver"],
    FromUtc = DateTimeOffset.UtcNow.AddDays(-1),
    ToUtc = DateTimeOffset.UtcNow,
    IncludeMetricTimeSeries = false  // Just inventory, no metrics data
};
```

---

## Logging

The application logs the query mode:

### Specific Collections
```
[12:34:56 INF] Building report for 2 collections and 1 component types
[12:34:56 INF] Querying collection: Production, component type: host
[12:34:56 INF] Querying collection: Staging, component type: host
```

### All Collections
```
[12:34:56 INF] Building report for all collections and 1 component types
[12:34:56 INF] Querying collection: All Collections, component type: host
[12:34:56 INF] Retrieved 45 components for All Collections/host
```

---

## API Reference

### MonitorQueryBuilders

All query builder methods now support `null`, `""`, or `"*"` for the `collectionName` parameter:

```csharp
// Query all collections
var request = MonitorQueryBuilders.CollectionComponentsWithAllMetrics(
    collectionName: "*",  // or null or ""
    componentType: "host",
    returnCountOnly: false);
```

### ArcGisMonitorQueryService

All service methods support querying all collections:

```csharp
// Count components from all collections
var count = await service.CountComponentsAsync(
    collectionName: "*",
    componentType: "host",
    fromUtc: DateTimeOffset.UtcNow.AddDays(-7),
    toUtc: DateTimeOffset.UtcNow);

// Get components with metrics from all collections
var components = await service.GetComponentsWithAllMetricsAsync(
    collectionName: "*",
    componentType: "host",
    pageSize: 500);

// Get components with metric stats from all collections
var componentsWithStats = await service.GetComponentsWithMetricStatsAsync(
    collectionName: "*",
    componentType: "host",
    metricNameLike: "CPU",
    fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
    toUtc: DateTimeOffset.UtcNow,
    pageSize: 500);
```

---

## Troubleshooting

### Issue: Query Takes Too Long

**Solution**: Optimize your query:
- Use specific `componentTypes` instead of all types
- Specify `metricNameLikes` to filter metrics
- Increase `pageSize` to 500
- Set `includeMetricTimeSeries: false`
- Reduce the time range (e.g., last 24 hours instead of 7 days)

### Issue: Out of Memory

**Solution**: For very large deployments:
- Query one component type at a time
- Use shorter time ranges
- Set `maxMetricIdsForTimeSeries` to a lower value
- Disable time series data collection

### Issue: Timeout Error

**Solution**: Increase the timeout:
```json
{
  "server": {
    "baseUrl": "https://monitor.example.com:30443",
    "username": "admin",
    "password": "password123",
    "timeoutSeconds": 900
  }
}
```

---

## Migration Guide

### Before (Specific Collections)
```json
{
  "report": {
    "collectionNames": ["Production", "Staging", "Development"],
    "componentTypes": ["host"]
  }
}
```

### After (All Collections)
```json
{
  "report": {
    "collectionNames": ["*"],
    "componentTypes": ["host"]
  }
}
```

**Benefits:**
- ✅ No need to maintain collection names list
- ✅ Automatically includes new collections
- ✅ Simpler configuration
- ✅ Single comprehensive report

---

## Best Practices

1. **Start Small**: Test with a single component type and short time range
2. **Monitor Performance**: Check logs for query duration
3. **Use Filters**: Apply `metricNameLikes` to reduce data volume
4. **Optimize Page Size**: Use 200-500 for large datasets
5. **Schedule Reports**: Run during off-peak hours for large deployments
6. **Validate Results**: Compare component counts with Monitor UI

---

## Examples

### Minimal Configuration
```json
{
  "server": {
    "baseUrl": "https://monitor.example.com:30443",
    "username": "admin",
    "password": "password123"
  },
  "report": {
    "collectionNames": ["*"],
    "componentTypes": ["host"],
    "fromUtc": "2025-01-26T00:00:00Z",
    "toUtc": "2025-01-27T00:00:00Z"
  }
}
```

### Full Configuration
```json
{
  "server": {
    "baseUrl": "https://monitor.example.com:30443",
    "username": "admin",
    "password": "password123",
    "timeoutSeconds": 600
  },
  "report": {
    "collectionNames": ["*"],
    "componentTypes": ["host", "service"],
    "metricNameLikes": ["CPU", "Memory"],
    "includeOnlyMetricNames": ["CPU Utilized", "Memory Available"],
    "excludeMetricNames": [],
    "alertingOnOnly": false,
    "fromUtc": "2025-01-20T00:00:00Z",
    "toUtc": "2025-01-27T00:00:00Z",
    "includeMetricTimeSeries": true,
    "metricBucket": "hour",
    "maxMetricIdsForTimeSeries": 500,
    "pageSize": 500
  }
}
```

---

## Related Documentation

- [Configuration Guide](../Configuration/README.md)
- [API Documentation](api-documentation.md)
- [Troubleshooting](troubleshooting.md)
- [Performance Tuning](performance-tuning.md)
