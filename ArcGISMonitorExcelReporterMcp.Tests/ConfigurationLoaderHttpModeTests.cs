using ArcGISMonitorExcelReporterMcp.Tools;

namespace ArcGISMonitorExcelReporterMcp.Tests
{
    /// <summary>
    /// Verifies that ConfigurationLoader.AllowConfigPath (set to false by the server's HTTP
    /// transport startup) actually blocks configPath, so a remote client can't read arbitrary
    /// files on the server's disk. Restores the flag afterward since it's process-wide state
    /// shared with every other test class (see AssemblyInfo.cs for parallelization).
    /// </summary>
    public sealed class ConfigurationLoaderHttpModeTests : IDisposable
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

        public void Dispose() => ConfigurationLoader.AllowConfigPath = true;

        [Fact]
        public async Task LoadConfigurationAsync_WithConfigPath_WhenAllowConfigPathIsFalse_ThrowsArgumentException()
        {
            ConfigurationLoader.AllowConfigPath = false;

            await Assert.ThrowsAsync<ArgumentException>(
                () => ConfigurationLoader.LoadConfigurationAsync(configPath: "some/path.json", configJson: null, CancellationToken.None));
        }

        [Fact]
        public async Task LoadConfigurationAsync_WithConfigJson_WhenAllowConfigPathIsFalse_StillSucceeds()
        {
            ConfigurationLoader.AllowConfigPath = false;

            var configuration = await ConfigurationLoader.LoadConfigurationAsync(configPath: null, configJson: ValidConfigJson, CancellationToken.None);

            Assert.Equal("https://monitor.example.com:30443/arcgis", configuration.Server.Url);
        }
    }
}
