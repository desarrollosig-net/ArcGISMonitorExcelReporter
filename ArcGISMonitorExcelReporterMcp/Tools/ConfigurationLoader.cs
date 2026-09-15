using System.Text.Json;

using ReporterConfiguration = ArcGISMonitorExcelReporterLib.Configuration.Configuration;

namespace ArcGISMonitorExcelReporterMcp.Tools
{
    /// <summary>
    /// Shared configuration-loading and JSON serialization settings used by all MCP tool classes.
    /// </summary>
    internal static class ConfigurationLoader
    {
        public static readonly JsonSerializerOptions ResponseJsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Whether <c>configPath</c> may be used to read a configuration file from the server's
        /// local disk. Set to <c>false</c> when running under the HTTP transport, since remote
        /// clients could otherwise read any file the server process can access. Stdio mode leaves
        /// this <c>true</c>, since the client and server share the same trust boundary there.
        /// </summary>
        public static bool AllowConfigPath { get; set; } = true;

        public static async Task<ReporterConfiguration> LoadConfigurationAsync(string? configPath, string? configJson, CancellationToken cancellationToken)
        {
            if(!string.IsNullOrWhiteSpace(configPath) && !string.IsNullOrWhiteSpace(configJson))
            {
                throw new ArgumentException("Provide either configPath or configJson, not both.");
            }

            if(!string.IsNullOrWhiteSpace(configPath))
            {
                if(!AllowConfigPath)
                {
                    throw new ArgumentException("configPath is not available over the HTTP transport (it would let a remote client read arbitrary files on the server). Provide configJson instead.");
                }

                return await ReporterConfiguration.LoadAsync(configPath, cancellationToken).ConfigureAwait(false);
            }

            if(!string.IsNullOrWhiteSpace(configJson))
            {
                var configuration = JsonSerializer.Deserialize<ReporterConfiguration>(configJson, ArcGISMonitorExcelReporterLib.Models.MonitorJson.Options);
                return configuration ?? throw new JsonException("Unable to deserialize configJson.");
            }

            throw new ArgumentException("Either configPath or configJson must be provided.");
        }
    }
}
