# Extracción técnica desde ExcelReporter.saz

## Sesiones analizadas

Se procesaron 250 sesiones HTTP del archivo Fiddler `.saz`.

| Endpoint | Método | Sesiones | Uso inferido |
|---|---:|---:|---|
| `/arcgis/auth/token` | POST | 51 | Emisión de token Bearer. |
| `/arcgis/monitoring/collections/query` | POST | 168 | Consulta de colecciones, componentes, métricas agregadas, alertas y relaciones. |
| `/arcgis/monitoring/metrics/query` | POST | 31 | Consulta de series temporales agregadas por `metric_id` y ventana temporal. |

## Patrones de consulta detectados

1. Autenticación contra `/arcgis/auth/token` con JSON `{ username, password, refresh_token, issue_refresh_token, exchange_refresh_token }`.
2. Consulta de componentes por colección usando `where = (name = 'Sample Collection')` y recurso incluido `components`.
3. Paginación por `resultRecordCount = 100` y `resultOffset = 0 / 100`.
4. Conteo con `returnCountOnly = true` antes de obtener registros con `returnCountOnly = false`.
5. Inclusión de recursos hijos: `metrics`, `metrics_data`, `alerts`, `labels`, `parents`, `agents`, `components_logs`, `observers`.
6. Agregación de métricas con `outStatistics` sobre el campo `value`.
7. Consulta de series temporales con agrupación `metric_id` y `observed_at:15m`.

## Tipos de componente observados

- `host`
- `database`
- `service`
- `storage`

## Métricas detectadas en filtros `name like '<métrica>%'`

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

## Consideraciones de serialización

- Las respuestas usan el patrón `features[] -> attributes`.
- En `/collections/query`, `components` puede venir como objeto `{ "count": n }` cuando se usa `returnCountOnly`, o como arreglo cuando se retornan registros. Por eso se generó `ComponentsResultJsonConverter`.
- Algunos campos numéricos aparecen unas veces como entero y otras como decimal; por seguridad se modelaron como `double?` cuando había mezcla.
- `components_logs` y `observers` aparecieron vacíos en la captura; se modelaron con `JsonExtensionData` para tolerar campos futuros sin romper deserialización.
- Las fechas se modelaron como `DateTimeOffset?`.

## Seguridad

La captura contiene credenciales y tokens. El código generado no incluye esos valores. Debe rotarse la contraseña capturada y revocarse cualquier token asociado si el ambiente todavía es accesible.
