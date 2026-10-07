using ArcGISMonitorExcelReporterMcp.Tools;

using ModelContextProtocol;

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
        public async Task ListComponentsAsync_WithBothConfigPathAndConfigJson_ThrowsMcpExceptionWrappingArgumentException()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.ListComponentsAsync(configPath: "some/path.json", configJson: ValidConfigJson));

            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        [Fact]
        public async Task ListComponentsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.ListComponentsAsync(configJson: BadPasswordConfigJson));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public async Task GetComponentMetricsAsync_WithNeitherComponentIdNorComponentName_ThrowsMcpExceptionWrappingArgumentException()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetComponentMetricsAsync(componentId: null, componentName: null, configJson: ValidConfigJson));

            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        [Fact]
        public async Task GetComponentMetricsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetComponentMetricsAsync(componentId: 42, configJson: BadPasswordConfigJson));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public async Task GetMetricStatsAsync_WithEmptyMetricNameLike_ThrowsMcpExceptionWrappingArgumentException()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetMetricStatsAsync(metricNameLike: "  ", configJson: ValidConfigJson));

            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        [Fact]
        public async Task GetMetricStatsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetMetricStatsAsync(metricNameLike: "CPU", configJson: BadPasswordConfigJson));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public async Task GetMetricTimeSeriesAsync_WithNoMetricIds_ThrowsMcpExceptionWrappingArgumentException()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetMetricTimeSeriesAsync(
                    metricIds: [],
                    fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
                    toUtc: DateTimeOffset.UtcNow,
                    configJson: ValidConfigJson));

            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        [Fact]
        public async Task GetMetricTimeSeriesAsync_WithFromUtcAfterToUtc_ThrowsMcpExceptionWrappingArgumentException()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetMetricTimeSeriesAsync(
                    metricIds: [101],
                    fromUtc: DateTimeOffset.UtcNow,
                    toUtc: DateTimeOffset.UtcNow.AddDays(-1),
                    configJson: ValidConfigJson));

            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        [Fact]
        public async Task GetMetricTimeSeriesAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetMetricTimeSeriesAsync(
                    metricIds: [101],
                    fromUtc: DateTimeOffset.UtcNow.AddDays(-1),
                    toUtc: DateTimeOffset.UtcNow,
                    configJson: BadPasswordConfigJson));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public async Task ListOpenAlertsAsync_WithInvalidBase64Password_ThrowsBeforeContactingServer()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.ListOpenAlertsAsync(configJson: BadPasswordConfigJson));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Theory]
        [InlineData("observed_at:15m", "observed_at:15m")]
        [InlineData("5m", "observed_at:5m")]
        [InlineData("observed_at:hour", "observed_at:hour")]
        [InlineData("1h", "observed_at:hour")]
        [InlineData("observed_at:60m", "observed_at:hour")]
        [InlineData("DAY", "observed_at:day")]
        [InlineData("observed_at:1d", "observed_at:day")]
        [InlineData("", "observed_at:15m")]
        public void NormalizeBucket_WithSupportedInterval_ReturnsMonitorBucket(string bucket, string expected)
        {
            Assert.Equal(expected, ComponentMetricTools.NormalizeBucket(bucket));
        }

        [Theory]
        [InlineData("observed_at:30m")]
        [InlineData("2h")]
        [InlineData("week")]
        public void NormalizeBucket_WithUnsupportedInterval_ThrowsArgumentException(string bucket)
        {
            Assert.Throws<ArgumentException>(() => ComponentMetricTools.NormalizeBucket(bucket));
        }

        [Fact]
        public async Task GetMetricTimeSeriesAsync_WithUnsupportedBucket_ThrowsBeforeContactingServer()
        {
            var exception = await Assert.ThrowsAsync<McpException>(
                () => ComponentMetricTools.GetMetricTimeSeriesAsync(
                    metricIds: [101],
                    fromUtc: DateTimeOffset.UtcNow.AddHours(-1),
                    toUtc: DateTimeOffset.UtcNow,
                    configJson: ValidConfigJson,
                    bucket: "30m"));

            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Contains("Unsupported bucket", exception.Message);
        }
    }
}
