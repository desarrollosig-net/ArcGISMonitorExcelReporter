# Exportación a Excel

El proyecto incluye una capa de reporte para guardar las salidas de ArcGIS Monitor en un archivo `.xlsx` usando ClosedXML.

## Clases agregadas

- `Reporting/MonitorReportRequest.cs` *(incluido en `MonitorExcelReportModels.cs`)*: parámetros de entrada del reporte.
- `Reporting/MonitorExcelReport`: contenedor normalizado de salidas.
- `Reporting/MonitorReportService`: ejecuta las llamadas HTTP ya estructuradas y arma el modelo tabular.
- `Reporting/MonitorExcelReportWriter`: escribe el archivo Excel físico.

## Estructura del Excel

El archivo generado contiene:

- `Resumen`: hoja inicial con metadatos, conteos e índice con vínculos internos.
- `Colecciones`: resumen tabular de colecciones consultadas.
- `Componentes`: inventario consolidado de componentes.
- `Metricas`: catálogo consolidado de métricas.
- `Datos_Metricas`: datos agregados o series temporales de métricas.
- `Alertas`: alertas asociadas a métricas.
- `COL_*`: hojas específicas por colección y tipo de componente.
- `MET_*`: hojas específicas por nombre de métrica.

Los nombres de hoja se sanitizan para cumplir las restricciones de Excel: máximo 31 caracteres y exclusión de caracteres no válidos como `[]:*?/\`.

## Ejemplo

```csharp
using ArcGISMonitorExcelReporterLib.Client;
using ArcGISMonitorExcelReporterLib.Reporting;

using var client = new ArcGisMonitorClient(new Uri("https://servidor-monitor:30443/"));
await client.AuthenticateAsync(username, password);

var queries = new ArcGisMonitorQueryService(client);
var reportService = new MonitorReportService(queries);

var request = new MonitorReportRequest
{
    CollectionNames = ["Sample Collection"],
    ComponentTypes = ["host", "arcgis-server", "portal"],
    MetricNameLikes = ["CPU Utilized", "Memory Utilized"],
    FromUtc = DateTimeOffset.UtcNow.AddDays(-5),
    ToUtc = DateTimeOffset.UtcNow,
    IncludeMetricTimeSeries = true
};

await reportService.BuildAndSaveExcelAsync(request, @"C:\Reportes\monitor.xlsx");
```
