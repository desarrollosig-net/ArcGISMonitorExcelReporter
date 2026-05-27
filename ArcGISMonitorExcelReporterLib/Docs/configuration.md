# Configuración

La biblioteca usa la clase `ArcGISMonitorExcelReporterLib.Configuration.Configuration` como contrato principal de entrada. Este contrato corresponde a la estructura del archivo `agm2023x.json`.

## Carga

```csharp
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json");
```

## Ejecución

```csharp
var reporter = new ArcGISMonitorExcelReporter();
await reporter.GenerateExcelAsync(configuration, "ArcGISMonitorReport.xlsx");
```

## Normalización de URL

Si `server.url` termina en `/arcgis`, la biblioteca elimina ese sufijo internamente porque las llamadas del cliente ya usan rutas relativas como `arcgis/auth/token`, `arcgis/monitoring/collections/query` y `arcgis/monitoring/metrics/query`.

## Seguridad

No se debe almacenar el archivo real de configuración con credenciales en un repositorio. Para control de código fuente debe usarse `Samples/agm2023x.sample.json` con valores sustitutos.
