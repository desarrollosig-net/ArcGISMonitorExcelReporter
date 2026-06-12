using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArcGISMonitorExcelReporterLib.Models;
using ArcGISMonitorExcelReporterLib.Reporting;

namespace ArcGISMonitorExcelReporterLib.Configuration
{
    /// <summary>
    /// Root configuration class for ArcGIS Monitor Excel Reporter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class represents the complete configuration loaded from JSON files.
    /// It contains server connection settings and report parameters.
    /// </para>
    /// <para>
    /// <b>Collection filtering:</b> The <see cref="ReportConfiguration.Collection"/> property supports:
    /// <list type="bullet">
    /// <item><description><c>null</c> or empty string: Queries all collections</description></item>
    /// <item><description><c>"*"</c>: Queries all collections</description></item>
    /// <item><description>Specific name: Queries only that collection</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Load configuration from JSON
    /// var config = await Configuration.LoadAsync("config.json");
    /// config.Validate();
    /// 
    /// // Convert to report request
    /// var request = config.ToReportRequest();
    /// </code>
    /// </example>
    public sealed class Configuration
    {
        [JsonPropertyName("server")]
        public ServerConfiguration Server { get; set; } = new();

        [JsonPropertyName("report")]
        public ReportConfiguration Report { get; set; } = new();

        /// <summary>
        /// Loads configuration asynchronously from a JSON file.
        /// </summary>
        /// <param name="path">Path to the JSON configuration file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Parsed configuration object.</returns>
        /// <exception cref="JsonException">Thrown if the file cannot be deserialized.</exception>
        /// <example>
        /// <code>
        /// var config = await Configuration.LoadAsync("config.json");
        /// </code>
        /// </example>
        public static async Task<Configuration> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            await using var stream = File.OpenRead(path);
            var configuration = await JsonSerializer.DeserializeAsync<Configuration>(stream, MonitorJson.Options, cancellationToken).ConfigureAwait(false);
            return configuration ?? throw new JsonException($"Unable to deserialize configuration from '{path}'.");
        }

        /// <summary>
        /// Loads configuration synchronously from a JSON file.
        /// </summary>
        /// <param name="path">Path to the JSON configuration file.</param>
        /// <returns>Parsed configuration object.</returns>
        /// <exception cref="JsonException">Thrown if the file cannot be deserialized.</exception>
        /// <example>
        /// <code>
        /// var config = Configuration.Load("config.json");
        /// </code>
        /// </example>
        public static Configuration Load(string path)
        {
            using var stream = File.OpenRead(path);
            var configuration = JsonSerializer.Deserialize<Configuration>(stream, MonitorJson.Options);
            return configuration ?? throw new JsonException($"Unable to deserialize configuration from '{path}'.");
        }

        /// <summary>
        /// Validates the configuration for required fields and valid values.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
        /// <remarks>
        /// <para>
        /// This method validates:
        /// <list type="bullet">
        /// <item><description>Server URL is a valid absolute URL</description></item>
        /// <item><description>Username and password are provided</description></item>
        /// <item><description>At least one component type is specified</description></item>
        /// <item><description>Time range values are non-negative</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Note:</b> Collection name is optional. Empty or <c>"*"</c> means query all collections.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var config = await Configuration.LoadAsync("config.json");
        /// config.Validate();  // Throws if invalid
        /// </code>
        /// </example>
        public void Validate()
        {
            if(Server is null)
            {
                throw new InvalidOperationException("Configuration must include the 'server' block.");
            }

            if(Report is null)
            {
                throw new InvalidOperationException("Configuration must include the 'report' block.");
            }

            if(string.IsNullOrWhiteSpace(Server.Url))
            {
                throw new InvalidOperationException("server.url is required.");
            }

            if(!Uri.TryCreate(Server.Url, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException("server.url must be a valid absolute URL.");
            }

            if(string.IsNullOrWhiteSpace(Server.Username))
            {
                throw new InvalidOperationException("server.username is required.");
            }

            if(string.IsNullOrWhiteSpace(Server.Password))
            {
                throw new InvalidOperationException("server.password is required.");
            }

            // Allow empty collection or "*" to query all collections
            // No validation needed - empty or "*" means all collections

            if(Report.Types.Count == 0)
            {
                throw new InvalidOperationException("report.types must contain at least one component type.");
            }

            if(Report.PastDays < 0 || Report.PastHours < 0)
            {
                throw new InvalidOperationException("report.past_days and report.past_hours cannot be negative.");
            }
        }

        /// <summary>
        /// Converts this configuration to a <see cref="MonitorReportRequest"/>.
        /// </summary>
        /// <returns>A report request ready to be passed to <see cref="MonitorReportService"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method:
        /// <list type="number">
        /// <item><description>Validates the configuration</description></item>
        /// <item><description>Resolves timezone and calculates UTC time range</description></item>
        /// <item><description>Processes metric filters (include/exclude)</description></item>
        /// <item><description>Handles collection name: empty or "*" becomes ["*"], specific name becomes [name]</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Collection handling:</b>
        /// <list type="bullet">
        /// <item><description><c>null</c>, <c>""</c>, or <c>"*"</c> → <c>CollectionNames = ["*"]</c> (all collections)</description></item>
        /// <item><description><c>"Production"</c> → <c>CollectionNames = ["Production"]</c> (specific collection)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var config = await Configuration.LoadAsync("config.json");
        /// var request = config.ToReportRequest();
        /// 
        /// var reportService = new MonitorReportService(queryService);
        /// var report = await reportService.BuildReportAsync(request);
        /// </code>
        /// </example>
        public MonitorReportRequest ToReportRequest()
        {
            Validate();

            var timezone = TimeZoneInfoResolver.Resolve(Report.Timezone);
            var toLocal = Report.EndTime.Resolve(timezone);
            var fromLocal = toLocal.AddDays(-Report.PastDays).AddHours(-Report.PastHours);

            var metrics = Report.Metrics ?? new MetricsConfiguration();
            var includeOnly = metrics.IncludeOnly.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var exclude = metrics.ExcludeMetrics.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Handle collection name: empty, "*", or specific name
            List<string> collectionNames = string.IsNullOrWhiteSpace(Report.Collection) || Report.Collection.Trim() == "*"
                ? ["*"]  // Query all collections
                : [Report.Collection];  // Query specific collection

            return new MonitorReportRequest
            {
                ServerUrl = Server.Url,
                Timezone = Report.Timezone,
                PastDays = Report.PastDays,
                PastHours = Report.PastHours,
                CollectionNames = collectionNames,
                ComponentTypes = [.. Report.Types.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase)],
                MetricNameLikes = includeOnly,
                IncludeOnlyMetricNames = includeOnly,
                ExcludeMetricNames = exclude,
                AlertingOnOnly = metrics.AlertingOnOnly,
                FromUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(fromLocal.DateTime, timezone), TimeSpan.Zero),
                ToUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(toLocal.DateTime, timezone), TimeSpan.Zero),
                PageSize = Report.PageSize,
                MetricBucket = Report.MetricBucket,
                IncludeMetricTimeSeries = Report.IncludeMetricTimeSeries,
                MaxMetricIdsForTimeSeries = Report.MaxMetricIdsForTimeSeries
            };
        }
    }

    public sealed class ServerConfiguration
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("password_encoding")]
        public bool PasswordEncoding { get; set; }

        [JsonPropertyName("ignore_ssl_errors")]
        public bool IgnoreSslErrors { get; set; } = true;

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 300;

        public string GetPassword()
        {
            if(!PasswordEncoding)
            {
                return Password;
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(Password));
            }
            catch(FormatException ex)
            {
                throw new InvalidOperationException("server.password_encoding is enabled, but server.password is not valid Base64.", ex);
            }
        }
    }

    /// <summary>
    /// Report-specific configuration parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class defines what data to query and how to format the report.
    /// </para>
    /// <para>
    /// <b>Collection filtering:</b> The <see cref="Collection"/> property supports:
    /// <list type="bullet">
    /// <item><description><c>null</c> or empty: Query all collections</description></item>
    /// <item><description><c>"*"</c>: Query all collections</description></item>
    /// <item><description>Specific name: Query only that collection</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class ReportConfiguration
    {
        /// <summary>
        /// Gets or sets the collection name to query.
        /// Use <c>null</c>, empty string, or <c>"*"</c> to query all collections.
        /// </summary>
        /// <example>
        /// <code>
        /// // Query specific collection
        /// Collection = "Production"
        /// 
        /// // Query all collections
        /// Collection = "*"
        /// Collection = ""
        /// Collection = null
        /// </code>
        /// </example>
        [JsonPropertyName("collection")]
        public string Collection { get; set; } = string.Empty;

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "UTC";

        [JsonPropertyName("end_time")]
        public EndTimeConfiguration EndTime { get; set; } = new();

        [JsonPropertyName("past_days")]
        public int PastDays { get; set; }

        [JsonPropertyName("past_hours")]
        public int PastHours { get; set; }

        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = [];

        [JsonPropertyName("metrics")]
        public MetricsConfiguration Metrics { get; set; } = new();

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; } = 100;

        [JsonPropertyName("metric_bucket")]
        public string MetricBucket { get; set; } = "observed_at:15m";

        [JsonPropertyName("include_metric_time_series")]
        public bool IncludeMetricTimeSeries { get; set; } = true;

        [JsonPropertyName("max_metric_ids_for_time_series")]
        public int? MaxMetricIdsForTimeSeries { get; set; } = 5000;
    }

    public sealed class EndTimeConfiguration
    {
        [JsonPropertyName("now")]
        public bool Now { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("day")]
        public int Day { get; set; }

        [JsonPropertyName("hour")]
        public int Hour { get; set; }

        [JsonPropertyName("minute")]
        public int Minute { get; set; }

        [JsonPropertyName("second")]
        public int Second { get; set; }

        public DateTimeOffset Resolve(TimeZoneInfo timezone)
        {
            if(Now)
            {
                return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone);
            }

            if(Year <= 0 || Month <= 0 || Day <= 0)
            {
                throw new InvalidOperationException("report.end_time must have now=true or valid year/month/day.");
            }

            var local = new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Unspecified);
            var offset = timezone.GetUtcOffset(local);
            return new DateTimeOffset(local, offset);
        }
    }

    public sealed class MetricsConfiguration
    {
        [JsonPropertyName("alerting_on_only")]
        public bool AlertingOnOnly { get; set; }

        [JsonPropertyName("include_only")]
        public List<string> IncludeOnly { get; set; } = [];

        [JsonPropertyName("exclude_metrics")]
        public List<string> ExcludeMetrics { get; set; } = [];
    }

    internal static class TimeZoneInfoResolver
    {
        public static TimeZoneInfo Resolve(string? timeZoneId)
        {
            if(string.IsNullOrWhiteSpace(timeZoneId))
            {
                return TimeZoneInfo.Utc;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch(TimeZoneNotFoundException) when(OperatingSystem.IsWindows())
            {
                if(string.Equals(timeZoneId, "America/Bogota", StringComparison.OrdinalIgnoreCase))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                }

                throw;
            }
        }
    }
}
