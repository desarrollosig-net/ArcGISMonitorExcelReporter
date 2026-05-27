# Technical Extraction from ExcelReporter.saz

## Analyzed Sessions

250 HTTP sessions from the Fiddler `.saz` file were processed.

| Endpoint | Method | Sessions | Inferred Usage |
|---|---:|---:|---|
| `/arcgis/auth/token` | POST | 51 | Bearer token issuance. |
| `/arcgis/monitoring/collections/query` | POST | 168 | Query for collections, components, aggregated metrics, alerts, and relationships. |
| `/arcgis/monitoring/metrics/query` | POST | 31 | Query for time series aggregated by `metric_id` and time window. |

## Detected Query Patterns

1. Authentication against `/arcgis/auth/token` with JSON `{ username, password, refresh_token, issue_refresh_token, exchange_refresh_token }`.
2. Component query by collection using `where = (name = 'Sample Collection')` and included resource `components`.
3. Pagination using `resultRecordCount = 100` and `resultOffset = 0 / 100`.
4. Count with `returnCountOnly = true` before retrieving records with `returnCountOnly = false`.
5. Child resource inclusion: `metrics`, `metrics_data`, `alerts`, `labels`, `parents`, `agents`, `components_logs`, `observers`.
6. Metric aggregation with `outStatistics` on the `value` field.
7. Time series query with `metric_id` and `observed_at:15m` grouping.

## Observed Component Types

- `host`
- `database`
- `service`
- `storage`

## Metrics Detected in `name like '<metric>%'` Filters

- CPU Cores Utilized
- CPU Utilized
- Cache Hit
- Connectivity
- Database Sessions
- GDB Branch Version Conflicts
- GDB Branch Version Locks
- GDB Branch Versions
- GDB Connections
- GDB Connections - editors
- GDB Connections - viewers
- GDB Default Version Depth
- GDB Shared Locks
- GDB State Lineages
- GDB Version States
- GDB Versions
- Instances Saturation Percent
- Instances Used
- Memory Available
- Memory Used
- Memory Utilized
- Network Incoming
- Network Incoming Utilized
- Network Outgoing
- Network Outgoing Utilized
- Open Cursors
- Pagefile Available
- Pagefile Used
- Pagefile Utilized
- Process CPU
- Process Instances
- Process Memory
- Request Rate
- Request Response Time Avg
- Request Response Time Max
- Requests Error Percent
- Requests Failed
- Requests Received
- Requests Timed Out
- Storage Capacity Available
- Storage Capacity Used
- Storage Capacity Utilized
- Storage Read Rate
- Storage Write Rate
- Terminal Sessions
- Terminal Sessions - active
- Terminal Sessions - inactive

## Serialization Considerations

- Responses use the `features[] -> attributes` pattern.
- In `/collections/query`, `components` can come as an object `{ "count": n }` when using `returnCountOnly`, or as an array when returning records. That's why `ComponentsResultJsonConverter` was generated.
- Some numeric fields appear sometimes as integer and other times as decimal; for safety, they were modeled as `double?` when there was a mix.
- `components_logs` and `observers` appeared empty in the capture; they were modeled with `JsonExtensionData` to tolerate future fields without breaking deserialization.
- Dates were modeled as `DateTimeOffset?`.

## Security

The capture contains credentials and tokens. The generated code does not include those values. The captured password should be rotated and any associated token should be revoked if the environment is still accessible.
