using System.Text.Json;

using ArcGISMonitorExcelReporterMcp.Tools;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace ArcGISMonitorExcelReporterMcp.Tests
{
    /// <summary>
    /// Verifies generate_excel_report's HTTP-mode behavior (MonitorReportTools.EmbedGeneratedReport):
    /// the workbook is returned inline and temporary files are cleaned up, and outputPath is
    /// rejected. Restores the process-wide flag afterward (see AssemblyInfo.cs for parallelization).
    /// </summary>
    public sealed class GenerateExcelReportHttpModeTests : IDisposable
    {
        private const string ValidConfigJson = """
            {
              "server": {
                "url": "https://monitor.example.com:30443/arcgis",
                "username": "usuario",
                "password": "cambiar_por_password_o_base64"
              },
              "report": {
                "collection": "*",
                "timezone": "UTC",
                "end_time": { "now": true },
                "past_days": 1,
                "past_hours": 0,
                "types": [ "host" ]
              }
            }
            """;

        public void Dispose() => MonitorReportTools.EmbedGeneratedReport = false;

        [Fact]
        public async Task GenerateExcelReportAsync_WithOutputPath_WhenEmbedding_ThrowsBeforeContactingServer()
        {
            MonitorReportTools.EmbedGeneratedReport = true;

            var exception = await Assert.ThrowsAsync<McpException>(
                () => MonitorReportTools.GenerateExcelReportAsync(configJson: ValidConfigJson, outputPath: "/tmp/report.xlsx"));

            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Contains("outputPath is not available", exception.Message);
        }

        [Fact]
        public async Task EmbedReportAsync_ReturnsSummaryAndXlsxBlob_AndDeletesTemporaryFolder()
        {
            var folder = Path.Combine(Path.GetTempPath(), "arcgis-mcp-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "Report_test.xlsx");
            byte[] content = [0x50, 0x4B, 0x03, 0x04, 1, 2, 3];
            await File.WriteAllBytesAsync(path, content);

            var result = await MonitorReportTools.EmbedReportAsync(path, "00:00:01", CancellationToken.None);

            Assert.False(Directory.Exists(folder));
            Assert.Equal(2, result.Content.Count);

            var summary = JsonDocument.Parse(Assert.IsType<TextContentBlock>(result.Content[0]).Text).RootElement;
            Assert.Equal("Report_test.xlsx", summary.GetProperty("fileName").GetString());
            Assert.Equal(content.Length, summary.GetProperty("sizeBytes").GetInt64());

            var blob = Assert.IsType<BlobResourceContents>(Assert.IsType<EmbeddedResourceBlock>(result.Content[1]).Resource);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", blob.MimeType);
            Assert.Equal(content, blob.DecodedData.ToArray());
        }
    }
}
