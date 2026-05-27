# ArcGISMonitorExcelReporterLib

Biblioteca .NET 8 para consultar ArcGIS Monitor, estructurar las llamadas HTTP de autenticación, colecciones y métricas, y exportar las salidas a un archivo Excel.

## Objetivo

El proyecto expone un punto de entrada único mediante la clase `ArcGISMonitorExcelReporter`. El consumidor puede invocarlo con un objeto `Configuration` cargado desde un archivo JSON con la estructura de `agm2023x.json`.

## Estructura principal

```text
ArcGISMonitorExcelReporterLib/
├─ ArcGISMonitorExcelReporter.cs
├─ ArcGISMonitorExcelReporterLib.csproj
├─ Configuration/
│  └─ Configuration.cs
├─ Client/
│  ├─ ArcGisMonitorClient.cs
│  └─ ArcGisMonitorQueryService.cs
├─ Builders/
│  └─ MonitorQueryBuilders.cs
├─ Models/
│  ├─ AuthModels.cs
│  ├─ JsonOptions.cs
│  ├─ QueryModels.cs
│  └─ ResponseModels.cs
├─ Reporting/
│  ├─ MonitorExcelReportModels.cs
│  ├─ MonitorExcelReportWriter.cs
│  └─ MonitorReportService.cs
└─ Samples/
   ├─ ExampleUsage.cs
   └─ agm2023x.sample.json
```

## Dependencia principal

```xml
<PackageReference Include="ClosedXML" Version="0.104.2" />
```

## Uso desde archivo JSON

```csharp
using ArcGISMonitorExcelReporterLib;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var configuration = await ReporterConfiguration.LoadAsync("agm2023x.json");
var reporter = new ArcGISMonitorExcelReporter();

await reporter.GenerateExcelAsync(
    configuration,
    "ArcGISMonitorReport.xlsx");
```

## Uso directo con objeto Configuration

```csharp
using ArcGISMonitorExcelReporterLib;
using ArcGISMonitorExcelReporterLib.Configuration;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var configuration = new ReporterConfiguration
{
    Server = new ServerConfiguration
    {
        Url = "https://monitor.example.com:30443/arcgis",
        Username = "user",
        Password = "password",
        PasswordEncoding = false
    },
    Report = new ReportConfiguration
    {
        Collection = "Sample Collection",
        Timezone = "America/Bogota",
        EndTime = new EndTimeConfiguration { Now = true },
        PastDays = 5,
        PastHours = 0,
        Types = ["host", "storage", "service", "database"],
        Metrics = new MetricsConfiguration
        {
            AlertingOnOnly = false,
            IncludeOnly = [],
            ExcludeMetrics = []
        }
    }
};

var reporter = new ArcGISMonitorExcelReporter();
await reporter.GenerateExcelAsync(configuration, "ArcGISMonitorReport.xlsx");
```

## Contrato de configuración

El archivo JSON debe contener estos bloques:

- `server.url`: URL base de ArcGIS Monitor. Puede terminar en `/arcgis`; la biblioteca normaliza la URL para evitar duplicar el segmento.
- `server.username`: usuario de autenticación.
- `server.password`: contraseña. No debe versionarse en repositorios.
- `server.password_encoding`: si es `true`, la contraseña se interpreta como Base64 UTF-8.
- `report.collection`: nombre de la colección.
- `report.timezone`: zona horaria usada para calcular el rango temporal.
- `report.end_time`: fecha final o `now=true`.
- `report.past_days` y `report.past_hours`: ventana hacia atrás desde `end_time`.
- `report.types`: tipos de componente a consultar.
- `report.metrics.alerting_on_only`: conserva solo métricas con alertamiento activo.
- `report.metrics.include_only`: lista de nombres o prefijos de métricas a incluir.
- `report.metrics.exclude_metrics`: lista de nombres o fragmentos de métricas a excluir.

## Salida Excel

El archivo Excel generado contiene:

- `Resumen`: índice general con conteos y vínculos internos.
- `Colecciones`: resumen por colección y tipo de componente.
- `Componentes`: componentes consultados.
- `Metricas`: métricas asociadas a los componentes.
- `Datos_Metricas`: series o agregados de métricas.
- `Alertas`: alertas asociadas.
- hojas `COL_*`: separación por colección/tipo.
- hojas `MET_*`: separación por métrica.

Los nombres de hojas se sanitizan para cumplir las restricciones de Excel: máximo 31 caracteres y eliminación de caracteres inválidos.
