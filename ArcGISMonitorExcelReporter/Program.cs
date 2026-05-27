using ArcGISMonitorExcelReporterLib;
using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

var cancellationToken = CancellationToken.None;
var filePath = "D:\\ExcelReport\\dist\\agm2023x.json";

var configuration = await ReporterConfiguration.LoadAsync(filePath, cancellationToken);
var reporter = new ArcGISMonitorExcelReporter();

await reporter.GenerateExcelAsync(
    configuration,
    "ArcGISMonitorReport.xlsx",
    cancellationToken);
