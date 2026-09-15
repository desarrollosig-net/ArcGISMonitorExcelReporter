using ArcGISMonitorExcelReporterMcp.Tools;

namespace ArcGISMonitorExcelReporterMcp.Tests
{
    public sealed class ComponentMetricToolsTests
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

        private const string BadPasswordConfigJson = """
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

        [Fact]
        public async Task ListComponentsAsync_WithBothConfigPathAndConfigJson_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => ComponentMetricTools.ListComponentsAsync(configPath: "some/path.json", configJson: ValidConfigJson));
        }

        [Fact]
        public async Task ListComponentsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ComponentMetricTools.ListComponentsAsync(configJson: BadPasswordConfigJson));
        }

        [Fact]
        public async Task GetComponentMetricsAsync_WithNeitherComponentIdNorComponentName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => ComponentMetricTools.GetComponentMetricsAsync(componentId: null, componentName: null, configJson: ValidConfigJson));
        }

        [Fact]
        public async Task GetComponentMetricsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ComponentMetricTools.GetComponentMetricsAsync(componentId: 42, configJson: BadPasswordConfigJson));
        }

        [Fact]
        public async Task GetMetricStatsAsync_WithEmptyMetricNameLike_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => ComponentMetricTools.GetMetricStatsAsync(metricNameLike: "  ", configJson: ValidConfigJson));
        }

        [Fact]
        public async Task GetMetricStatsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ComponentMetricTools.GetMetricStatsAsync(metricNameLike: "CPU", configJson: BadPasswordConfigJson));
        }

        [Fact]
        public async Task GetMetricTimeSeriesAsync_WithNoMetricIds_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => ComponentMetricTools.GetMetricTimeSeriesAsync(
                    metricIds: [],
                    fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
                    toUtc: DateTimeOffset.UtcNow,
                    configJson: ValidConfigJson));
        }

        [Fact]
        public async Task GetMetricTimeSeriesAsync_WithFromUtcAfterToUtc_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => ComponentMetricTools.GetMetricTimeSeriesAsync(
                    metricIds: [101],
                    fromUtc: DateTimeOffset.UtcNow,
                    toUtc: DateTimeOffset.UtcNow.AddDays(-1),
                    configJson: ValidConfigJson));
        }

        [Fact]
        public async Task GetMetricTimeSeriesAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ComponentMetricTools.GetMetricTimeSeriesAsync(
                    metricIds: [101],
                    fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
                    toUtc: DateTimeOffset.UtcNow,
                    configJson: BadPasswordConfigJson));
        }

        [Fact]
        public async Task ListOpenAlertsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ComponentMetricTools.ListOpenAlertsAsync(configJson: BadPasswordConfigJson));
        }
    }
}
