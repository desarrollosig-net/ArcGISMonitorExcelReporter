using System.Text.Json;

using ArcGISMonitorExcelReporterMcp.Tools;

namespace ArcGISMonitorExcelReporterMcp.Tests
{
    public sealed class MonitorReportToolsTests : IDisposable
    {
        private const string ValidConfigJson = """
            {
              "server": {
                "url": "https://monitor.example.com:30443/arcgis",
                "username": "usuario",
                "password": "cambiar_por_password_o_base64",
                "password_encoding": false,
                "ignore_ssl_errors": false,
                "timeout_seconds": 300
              },
              "report": {
                "collection": "*",
                "timezone": "UTC",
                "end_time": { "now": true },
                "past_days": 15,
                "past_hours": 0,
                "types": [ "host", "service" ]
              }
            }
            """;

        private readonly List<string> _tempFiles = [];

        public void Dispose()
        {
            foreach(var file in _tempFiles)
            {
                File.Delete(file);
            }
        }

        private string WriteTempConfig(string json)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
            File.WriteAllText(path, json);
            _tempFiles.Add(path);
            return path;
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithValidConfigJson_ReturnsValid()
        {
            var result = await MonitorReportTools.ValidateConfigurationAsync(configJson: ValidConfigJson);

            using var document = JsonDocument.Parse(result);
            Assert.True(document.RootElement.GetProperty("valid").GetBoolean());
            Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("error").ValueKind);
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithValidConfigPath_ReturnsValid()
        {
            var path = WriteTempConfig(ValidConfigJson);

            var result = await MonitorReportTools.ValidateConfigurationAsync(configPath: path);

            using var document = JsonDocument.Parse(result);
            Assert.True(document.RootElement.GetProperty("valid").GetBoolean());
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithMissingRequiredFields_ReturnsInvalid()
        {
            const string invalidConfigJson = """
                {
                  "server": { "url": "https://monitor.example.com" },
                  "report": { "types": [] }
                }
                """;

            var result = await MonitorReportTools.ValidateConfigurationAsync(configJson: invalidConfigJson);

            using var document = JsonDocument.Parse(result);
            Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("error").GetString()));
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithMalformedJson_ReturnsInvalid()
        {
            var result = await MonitorReportTools.ValidateConfigurationAsync(configJson: "{ not valid json");

            using var document = JsonDocument.Parse(result);
            Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithNonexistentConfigPath_ReturnsInvalid()
        {
            var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

            var result = await MonitorReportTools.ValidateConfigurationAsync(configPath: missingPath);

            using var document = JsonDocument.Parse(result);
            Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithBothConfigPathAndConfigJson_ReturnsInvalid()
        {
            var path = WriteTempConfig(ValidConfigJson);

            var result = await MonitorReportTools.ValidateConfigurationAsync(configPath: path, configJson: ValidConfigJson);

            using var document = JsonDocument.Parse(result);
            Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
            Assert.Contains("not both", document.RootElement.GetProperty("error").GetString());
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithNeitherConfigPathNorConfigJson_ReturnsInvalid()
        {
            var result = await MonitorReportTools.ValidateConfigurationAsync();

            using var document = JsonDocument.Parse(result);
            Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
            Assert.Contains("must be provided", document.RootElement.GetProperty("error").GetString());
        }

        [Fact]
        public async Task BuildReportSummaryAsync_WithBothConfigPathAndConfigJson_ThrowsArgumentException()
        {
            var path = WriteTempConfig(ValidConfigJson);

            await Assert.ThrowsAsync<ArgumentException>(
                () => MonitorReportTools.BuildReportSummaryAsync(configPath: path, configJson: ValidConfigJson));
        }

        [Fact]
        public async Task BuildReportSummaryAsync_WithInvalidConfiguration_ThrowsBeforeContactingServer()
        {
            const string invalidConfigJson = """
                {
                  "server": { "url": "https://monitor.example.com" },
                  "report": { "types": [] }
                }
                """;

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => MonitorReportTools.BuildReportSummaryAsync(configJson: invalidConfigJson));
        }

        [Fact]
        public async Task GenerateExcelReportAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            const string badPasswordConfigJson = """
                {
                  "server": {
                    "url": "https://monitor.example.com:30443/arcgis",
                    "username": "usuario",
                    "password": "not-valid-base64!!",
                    "password_encoding": true
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

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => MonitorReportTools.GenerateExcelReportAsync(configJson: badPasswordConfigJson));
        }
    }
}
