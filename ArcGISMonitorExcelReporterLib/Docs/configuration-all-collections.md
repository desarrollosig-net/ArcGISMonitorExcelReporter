# Configuration Guide - Query All Collections

## Overview

The configuration file now supports querying **all collections** without specifying individual collection names. This simplifies configuration for enterprise-wide reports.

---

## Configuration File Structure

### Basic Configuration (Specific Collection)

```json
{
  "server": {
    "url": "https://monitor.example.com:30443/arcgis",
    "username": "admin",
    "password": "password123",
    "password_encoding": false,
    "ignore_ssl_errors": true,
    "timeout_seconds": 300
  },
  "report": {
    "collection": "Production",
    "timezone": "America/Bogota",
    "end_time": {
      "now": true
    },
    "past_days": 7,
    "past_hours": 0,
    "types": ["host", "service"],
    "metrics": {
      "alerting_on_only": false,
      "include_only": ["CPU", "Memory"],
      "exclude_metrics": []
    },
    "page_size": 100,
    "metric_bucket": "observed_at:15m",
    "include_metric_time_series": true,
    "max_metric_ids_for_time_series": 5000
  }
}
```

### Query All Collections

To query all collections, use one of these values for `"collection"`:

#### Option 1: Wildcard "*"
```json
{
  "report": {
    "collection": "*",
    "types": ["host"]
  }
}
```

#### Option 2: Empty String
```json
{
  "report": {
    "collection": "",
    "types": ["host"]
  }
}
```

#### Option 3: Null (omit the property - not recommended in JSON)
JSON doesn't support explicit `null` easily, so use `""` or `"*"` instead.

---

## Configuration Fields

### Server Configuration

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `url` | string | ✅ Yes | - | ArcGIS Monitor base URL |
| `username` | string | ✅ Yes | - | Username for authentication |
| `password` | string | ✅ Yes | - | Password (plain text or Base64) |
| `password_encoding` | boolean | ❌ No | `false` | If `true`, password is Base64-encoded |
| `ignore_ssl_errors` | boolean | ❌ No | `true` | Ignore SSL certificate errors |
| `timeout_seconds` | integer | ❌ No | `300` | HTTP request timeout (use `-1` for infinite) |

### Report Configuration

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `collection` | string | ❌ No | `"*"` | Collection name. Use `"*"` or `""` for all collections |
| `timezone` | string | ❌ No | `"UTC"` | Timezone ID (e.g., `"America/Bogota"`, `"UTC"`) |
| `end_time` | object | ✅ Yes | - | Report end time configuration |
| `past_days` | integer | ✅ Yes | `0` | Number of days to look back |
| `past_hours` | integer | ❌ No | `0` | Additional hours to look back |
| `types` | string[] | ✅ Yes | - | Component types (e.g., `["host", "service"]`) |
| `metrics` | object | ❌ No | `{}` | Metric filtering configuration |
| `page_size` | integer | ❌ No | `100` | Number of records per page (100-500 recommended) |
| `metric_bucket` | string | ❌ No | `"observed_at:15m"` | Time bucket for time series (e.g., `"observed_at:1h"`) |
| `include_metric_time_series` | boolean | ❌ No | `true` | Include time series data in report |
| `max_metric_ids_for_time_series` | integer | ❌ No | `5000` | Maximum metrics to fetch time series for |

### End Time Configuration

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `now` | boolean | ❌ No | `false` | If `true`, use current date/time |
| `year` | integer | * | - | Year (required if `now = false`) |
| `month` | integer | * | - | Month (1-12, required if `now = false`) |
| `day` | integer | * | - | Day (1-31, required if `now = false`) |
| `hour` | integer | ❌ No | `0` | Hour (0-23) |
| `minute` | integer | ❌ No | `0` | Minute (0-59) |
| `second` | integer | ❌ No | `0` | Second (0-59) |

\* Required only when `now = false`

### Metrics Configuration

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `alerting_on_only` | boolean | ❌ No | `false` | Only include metrics with alerting enabled |
| `include_only` | string[] | ❌ No | `[]` | Metric name patterns to include (e.g., `["CPU", "Memory"]`) |
| `exclude_metrics` | string[] | ❌ No | `[]` | Metric name patterns to exclude |

---

## Collection Field Behavior

| Value | Behavior | Use Case |
|-------|----------|----------|
| `"Production"` | Query only "Production" collection | Single environment report |
| `"*"` | Query **all** collections | Enterprise-wide report |
| `""` (empty) | Query **all** collections | Enterprise-wide report |
| `null` | Query **all** collections | Not recommended (use `"*"` instead) |

---

## Examples

### Example 1: Query All Collections with CPU and Memory Metrics

```json
{
  "server": {
    "url": "https://monitor.example.com:30443/arcgis",
    "username": "admin",
    "password": "cGFzc3dvcmQxMjM=",
    "password_encoding": true,
    "timeout_seconds": 600
  },
  "report": {
    "collection": "*",
    "timezone": "America/Bogota",
    "end_time": {
      "now": true
    },
    "past_days": 7,
    "past_hours": 0,
    "types": ["host"],
    "metrics": {
      "alerting_on_only": false,
      "include_only": ["CPU", "Memory"],
      "exclude_metrics": []
    },
    "page_size": 500,
    "metric_bucket": "observed_at:1h",
    "include_metric_time_series": true,
    "max_metric_ids_for_time_series": 500
  }
}
```

**Description:** Queries all collections for host components, including only CPU and Memory metrics from the last 7 days.

---

### Example 2: Query Specific Collection with All Metrics

```json
{
  "server": {
    "url": "https://monitor.example.com:30443/arcgis",
    "username": "admin",
    "password": "password123",
    "password_encoding": false,
    "timeout_seconds": 300
  },
  "report": {
    "collection": "Production",
    "timezone": "UTC",
    "end_time": {
      "now": false,
      "year": 2025,
      "month": 1,
      "day": 27,
      "hour": 23,
      "minute": 59,
      "second": 59
    },
    "past_days": 1,
    "past_hours": 0,
    "types": ["host", "service", "database"],
    "metrics": {
      "alerting_on_only": false,
      "include_only": [],
      "exclude_metrics": ["Test", "Debug"]
    },
    "page_size": 100,
    "metric_bucket": "observed_at:15m",
    "include_metric_time_series": true,
    "max_metric_ids_for_time_series": 5000
  }
}
```

**Description:** Queries the "Production" collection for all component types, excluding Test and Debug metrics from a specific 24-hour period.

---

### Example 3: Health Check Report (Last Hour, All Collections)

```json
{
  "server": {
    "url": "https://monitor.example.com:30443/arcgis",
    "username": "health_check_user",
    "password": "password123",
    "timeout_seconds": 120
  },
  "report": {
    "collection": "*",
    "timezone": "UTC",
    "end_time": {
      "now": true
    },
    "past_days": 0,
    "past_hours": 1,
    "types": ["host"],
    "metrics": {
      "alerting_on_only": true,
      "include_only": ["CPU Utilized", "Memory Available"],
      "exclude_metrics": []
    },
    "page_size": 200,
    "metric_bucket": "observed_at:5m",
    "include_metric_time_series": false,
    "max_metric_ids_for_time_series": 0
  }
}
```

**Description:** Quick health check across all collections, showing only alerting-enabled CPU and Memory metrics from the last hour.

---

## Validation Rules

The configuration is validated when loaded:

### ✅ Valid Configurations

```json
// Query all collections
{ "collection": "*" }
{ "collection": "" }

// Query specific collection
{ "collection": "Production" }
```

### ❌ Invalid Configurations

```json
// Missing required fields
{ "types": [] }  // Must have at least one component type

// Invalid time range
{ "past_days": -1 }  // Cannot be negative

// Invalid URL
{ "url": "not-a-valid-url" }  // Must be absolute URL

// Invalid end_time
{
  "end_time": {
    "now": false
    // Missing year, month, day
  }
}
```

---

## Migration from Old Configuration

### Before (Single Collection)
```json
{
  "report": {
    "collection": "Production"
  }
}
```

### After (All Collections)
```json
{
  "report": {
    "collection": "*"
  }
}
```

**Benefits:**
- ✅ No need to know collection names in advance
- ✅ Automatically includes new collections
- ✅ Single comprehensive report
- ✅ Simpler configuration maintenance

---

## Performance Tips

When querying all collections in large deployments:

1. **Increase Page Size**
   ```json
   "page_size": 500
   ```

2. **Filter Component Types**
   ```json
   "types": ["host"]  // Instead of ["host", "service", "database"]
   ```

3. **Filter Metrics**
   ```json
   "metrics": {
     "include_only": ["CPU", "Memory"]
   }
   ```

4. **Disable Time Series if Not Needed**
   ```json
   "include_metric_time_series": false
   ```

5. **Increase Timeout**
   ```json
   "timeout_seconds": 900
   ```

6. **Limit Time Series Metrics**
   ```json
   "max_metric_ids_for_time_series": 500
   ```

---

## Timezone Examples

### Common Timezones

| Region | Timezone ID | UTC Offset |
|--------|-------------|------------|
| UTC | `"UTC"` | UTC+0 |
| Colombia | `"America/Bogota"` | UTC-5 |
| New York | `"America/New_York"` | UTC-5 (EST) / UTC-4 (EDT) |
| Los Angeles | `"America/Los_Angeles"` | UTC-8 (PST) / UTC-7 (PDT) |
| London | `"Europe/London"` | UTC+0 (GMT) / UTC+1 (BST) |
| Tokyo | `"Asia/Tokyo"` | UTC+9 |

### Windows vs. Linux Timezones

**Windows:**
```json
{ "timezone": "SA Pacific Standard Time" }
```

**Linux:**
```json
{ "timezone": "America/Bogota" }
```

The library automatically handles `"America/Bogota"` on Windows by mapping it to `"SA Pacific Standard Time"`.

---

## Troubleshooting

### Error: "report.collection is required"

**Old Behavior (Before Update):**
Empty collection was invalid.

**New Behavior (After Update):**
Empty collection or `"*"` queries all collections.

**Solution:**
Update to the latest version of the library.

---

### Error: Configuration Validation Failed

Check that:
- ✅ `server.url` is a valid absolute URL
- ✅ `server.username` is not empty
- ✅ `server.password` is not empty
- ✅ `report.types` has at least one type
- ✅ `past_days` and `past_hours` are non-negative

---

### Report Takes Too Long

**Solution:** Optimize your configuration:
```json
{
  "report": {
    "collection": "*",
    "types": ["host"],  // Limit to one type
    "metrics": {
      "include_only": ["CPU", "Memory"]  // Filter metrics
    },
    "page_size": 500,  // Increase page size
    "include_metric_time_series": false,  // Disable if not needed
    "past_days": 1  // Reduce time range
  }
}
```

---

## Sample Files

The library includes sample configuration files:

| File | Description |
|------|-------------|
| `agm2023x.sample.json` | Basic configuration with specific collection |
| `all-collections.sample.json` | Full configuration querying all collections |
| `all-collections-simple.sample.json` | Simplified configuration for all collections |

---

## Related Documentation

- [Query All Collections Guide](query-all-collections.md) - Detailed API and code examples
- [API Documentation](api-documentation.md) - Complete API reference
- [Troubleshooting](troubleshooting.md) - Common issues and solutions
- [Performance Tuning](performance-tuning.md) - Optimization tips

---

## Summary

| Configuration | Behavior | Command Line Example |
|---------------|----------|---------------------|
| `"collection": "Production"` | Query single collection | `ArcGISMonitorExcelReporter -f prod-config.json` |
| `"collection": "*"` | Query all collections | `ArcGISMonitorExcelReporter -f all-config.json` |
| `"collection": ""` | Query all collections | `ArcGISMonitorExcelReporter -f all-config.json` |

The configuration system is now fully compatible with the new **Query All Collections** feature! 🎉
