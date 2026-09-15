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

        public static async Task<ReporterConfiguration> LoadConfigurationAsync(string? configPath, string? configJson, CancellationToken cancellationToken)
        {
            if(!string.IsNullOrWhiteSpace(configPath) && !string.IsNullOrWhiteSpace(configJson))
            {
                throw new ArgumentException("Provide either configPath or configJson, not both.");
            }

            if(!string.IsNullOrWhiteSpace(configPath))
            {
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
