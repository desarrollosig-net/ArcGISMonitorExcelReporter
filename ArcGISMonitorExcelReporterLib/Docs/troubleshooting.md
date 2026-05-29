# Troubleshooting Guide

This guide covers common issues and their solutions when using the ArcGIS Monitor Excel Reporter.

## Table of Contents

- [Timeout Errors](#timeout-errors)
- [Authentication Issues](#authentication-issues)
- [SSL Certificate Errors](#ssl-certificate-errors)
- [Memory Issues](#memory-issues)
- [Excel File Generation Issues](#excel-file-generation-issues)

---

## Timeout Errors

### Symptom

```
System.Threading.Tasks.TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.
```

or

```
TimeoutException: The operation was canceled.
```

### Causes

1. **Large dataset queries**: Querying many components, metrics, or time series data
2. **Slow network**: Network latency between the client and ArcGIS Monitor server
3. **Server load**: ArcGIS Monitor server under heavy load
4. **Time series data**: Fetching time series data for many metrics can be slow

### Solutions

#### 1. Increase Timeout (Recommended)

Add or modify `timeout_seconds` in your configuration JSON:

```json
{
  "server": {
    "url": "https://monitor.example.com:30443/arcgis",
    "username": "user",
    "password": "password",
    "timeout_seconds": 600
  },
  "report": { ... }
}
```

**Recommended values:**
- **Small datasets** (< 50 components): `300` seconds (default)
- **Medium datasets** (50-200 components): `600` seconds (10 minutes)
- **Large datasets** (> 200 components): `1200` seconds (20 minutes)
- **Very large datasets** or slow networks: `-1` (infinite timeout - use with caution)

#### 2. Disable Time Series Data

If you don't need detailed time series, disable it to speed up queries:

```json
{
  "report": {
    "include_metric_time_series": false
  }
}
```

This will only fetch consolidated statistics, not individual data points.

#### 3. Limit Metrics for Time Series

Reduce the number of metrics fetched for time series:

```json
{
  "report": {
    "include_metric_time_series": true,
    "max_metric_ids_for_time_series": 500
  }
}
```

Default is 5000. Try lower values like 500, 1000, or 2000.

#### 4. Use Metric Filters

Filter metrics to reduce data volume:

```json
{
  "report": {
    "metrics": {
      "alerting_on_only": true,
      "include_only": ["CPU", "Memory"],
      "exclude_metrics": ["Process"]
    }
  }
}
```

#### 5. Query Fewer Component Types

Instead of querying all types at once:

```json
{
  "report": {
    "types": ["host", "storage", "service", "database"]
  }
}
```

Split into separate reports:

```json
{
  "report": {
    "types": ["host"]
  }
}
```

#### 6. Reduce Time Range

Shorten the query period:

```json
{
  "report": {
    "past_days": 1,
    "past_hours": 0
  }
}
```

#### 7. Increase Page Size

Larger pages mean fewer round trips (but more memory per request):

```json
{
  "report": {
    "page_size": 500
  }
}
```

Default is 100. Try 200, 300, or 500. Don't exceed 1000.

---

## Authentication Issues

### Symptom

```
InvalidOperationException: ArcGIS Monitor did not return a valid access_token.
```

### Solutions

1. **Verify credentials**: Check username and password in configuration
2. **Check URL**: Ensure the server URL is correct and accessible
3. **Base64 encoding**: If using `password_encoding: true`, ensure password is properly encoded:

```bash
# PowerShell
$password = "YourPassword"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($password)
$encoded = [Convert]::ToBase64String($bytes)
Write-Host $encoded
```

4. **Network connectivity**: Verify the server is reachable from your network

---

## SSL Certificate Errors

### Symptom

```
HttpRequestException: The SSL connection could not be established
```

### Solutions

#### For Development/Testing (Not Recommended for Production)

Enable SSL error bypass in configuration:

```json
{
  "server": {
    "ignore_ssl_errors": true
  }
}
```

#### For Production

1. **Install proper SSL certificate** on ArcGIS Monitor server
2. **Trust the certificate** on the client machine
3. Set `ignore_ssl_errors: false` in configuration

---

## Memory Issues

### Symptom

```
OutOfMemoryException
```

or application becomes very slow

### Solutions

1. **Disable time series data**: Use `"include_metric_time_series": false`
2. **Reduce `max_metric_ids_for_time_series`**: Lower from 5000 to 500-1000
3. **Query fewer components**: Use filters or split into multiple reports
4. **Reduce time range**: Query shorter periods
5. **Increase available memory**: 
   - Close other applications
   - Run on a machine with more RAM
   - For console apps: Use 64-bit runtime

---

## Excel File Generation Issues

### Symptom

Excel file is too large or contains too many sheets

### Solutions

1. **Sheet name truncation**: Excel limits sheet names to 31 characters. The library handles this automatically.

2. **Too many sheets**: If you have hundreds of metrics, consider:
   - Using metric filters to reduce the number
   - Splitting into multiple reports by component type

3. **File size**: For very large reports (> 100 MB):
   - Disable time series: `"include_metric_time_series": false`
   - Use shorter time ranges
   - Query specific metric patterns with `include_only`

---

## Best Practices

### Performance Optimization

1. **Start small**: Begin with a short time range and few component types
2. **Use filters**: Always filter metrics to what you actually need
3. **Monitor logs**: Watch the console/log output to see where time is spent
4. **Incremental approach**: If you need multiple component types, generate separate reports

### Example: Optimized Configuration

```json
{
  "server": {
    "url": "https://monitor.example.com:30443/arcgis",
    "username": "user",
    "password": "password",
    "timeout_seconds": 600,
    "ignore_ssl_errors": false
  },
  "report": {
    "collection": "Production",
    "timezone": "America/New_York",
    "end_time": { "now": true },
    "past_days": 1,
    "past_hours": 0,
    "types": ["host"],
    "metrics": {
      "alerting_on_only": true,
      "include_only": ["CPU", "Memory"],
      "exclude_metrics": []
    },
    "page_size": 200,
    "metric_bucket": "observed_at:15m",
    "include_metric_time_series": false,
    "max_metric_ids_for_time_series": 500
  }
}
```

This configuration:
- ✅ Sets reasonable timeout (10 minutes)
- ✅ Queries only 1 day of data
- ✅ Focuses on one component type
- ✅ Only includes alerting-enabled metrics
- ✅ Filters to CPU and Memory metrics only
- ✅ Disables time series for faster performance
- ✅ Uses larger page size for efficiency

---

## Getting Help

If you continue experiencing issues:

1. **Check logs**: Review `logs/arcgis-monitor-reporter-{date}.log`
2. **Enable debug logging**: See [Logging Guide](logging.md)
3. **Review configuration**: Validate your JSON against the [Configuration Guide](configuration.md)
4. **Check ArcGIS Monitor**: Verify the Monitor server is healthy and responsive

---

## Common Error Messages Reference

| Error | Meaning | Solution |
|-------|---------|----------|
| `TaskCanceledException` | Request timeout | Increase `timeout_seconds` |
| `InvalidOperationException: No token configured` | Authentication not called | Check auth credentials |
| `InvalidOperationException: Token is expired` | Token expired during execution | Increase timeout or reduce query scope |
| `HttpRequestException: 401` | Authentication failed | Verify username/password |
| `HttpRequestException: 404` | Collection/component not found | Check collection name and component types |
| `HttpRequestException: 500` | Server error | Check ArcGIS Monitor server health |
| `JsonException` | Invalid JSON response | Check network/SSL configuration |
| `OutOfMemoryException` | Insufficient memory | Reduce data volume or increase RAM |

---

**Last Updated**: 2025-01-27
