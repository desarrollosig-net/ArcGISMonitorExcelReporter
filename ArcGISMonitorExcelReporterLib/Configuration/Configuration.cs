// Ignore Spelling: Ssl

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
                FromLocal = fromLocal,
                ToLocal = toLocal,
                FromUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(fromLocal.DateTime, timezone), TimeSpan.Zero),
                ToUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(toLocal.DateTime, timezone), TimeSpan.Zero),
                PageSize = Report.PageSize,
                MetricBucket = Report.MetricBucket,
                IncludeMetricTimeSeries = Report.IncludeMetricTimeSeries,
                MaxMetricIdsForTimeSeries = Report.MaxMetricIdsForTimeSeries
            };
        }
    }

    /// <summary>
    /// Server connection configuration for ArcGIS Monitor.
    /// </summary>
    /// <remarks>
    /// This class encapsulates all parameters required to connect and authenticate with an ArcGIS Monitor instance.
    /// 
    /// Password handling supports two modes:
    /// <list type="bullet">
    /// <item><description><see cref="PasswordEncoding"/> = false: Password is stored as plain text (default)</description></item>
    /// <item><description><see cref="PasswordEncoding"/> = true: Password is Base64-encoded and will be decoded during authentication</description></item>
    /// </list>
    /// 
    /// SSL error tolerance can be configured via <see cref="IgnoreSslErrors"/> for development or self-signed certificate scenarios.
    /// 
    /// Default timeout is 300 seconds (5 minutes). Adjust <see cref="TimeoutSeconds"/> for slow networks or large data transfers.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple configuration with plain text password
    /// var serverConfig = new ServerConfiguration
    /// {
    ///     Url = "https://monitor.example.com:30443/arcgis",
    ///     Username = "admin",
    ///     Password = "mypassword",
    ///     PasswordEncoding = false,
    ///     TimeoutSeconds = 300
    /// };
    /// 
    /// // Configuration with encoded password
    /// var encodedConfig = new ServerConfiguration
    /// {
    ///     Url = "https://monitor.example.com:30443/arcgis",
    ///     Username = "admin",
    ///     Password = "bXlwYXNzd29yZA==",  // Base64-encoded "mypassword"
    ///     PasswordEncoding = true,
    ///     TimeoutSeconds = 600
    /// };
    /// </code>
    /// </example>
    public sealed class ServerConfiguration
    {
        /// <summary>
        /// Gets or sets the base URL of the ArcGIS Monitor server.
        /// Must be a valid absolute URL including protocol (http or https).
        /// </summary>
        /// <example>
        /// https://monitor.example.com:30443/arcgis
        /// </example>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username for authentication.
        /// Required and must not be empty.
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password for authentication.
        /// Required and must not be empty.
        /// When <see cref="PasswordEncoding"/> is true, this must be a valid Base64-encoded string.
        /// </summary>
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the password is Base64-encoded.
        /// When true, the password will be decoded from Base64 during the <see cref="GetPassword"/> call.
        /// Default is false (plain text password).
        /// </summary>
        [JsonPropertyName("password_encoding")]
        public bool PasswordEncoding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL/TLS certificate errors should be ignored.
        /// Set to true for development environments with self-signed certificates.
        /// Default is true. Should be false in production environments.
        /// </summary>
        [JsonPropertyName("ignore_ssl_errors")]
        public bool IgnoreSslErrors { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout in seconds for HTTP requests to the ArcGIS Monitor server.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Gets the password, decoding it from Base64 if <see cref="PasswordEncoding"/> is enabled.
        /// </summary>
        /// <returns>The decoded password, or the plain text password if encoding is disabled.</returns>
        /// <exception cref="InvalidOperationException">Thrown if password encoding is enabled but the password is not valid Base64.</exception>
        /// <example>
        /// <code>
        /// var serverConfig = new ServerConfiguration
        /// {
        ///     Password = "bXlwYXNzd29yZA==",
        ///     PasswordEncoding = true
        /// };
        /// 
        /// var decodedPassword = serverConfig.GetPassword();  // Returns "mypassword"
        /// </code>
        /// </example>
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
    /// <item><description>Specific name (e.g., "Production"): Query only that collection</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Time period:</b> Defined by <see cref="EndTime"/>, <see cref="PastDays"/>, and <see cref="PastHours"/>.
    /// The report covers the period from (EndTime - PastDays - PastHours) to EndTime.
    /// </para>
    /// <para>
    /// <b>Resource types:</b> Specify which component types to include in the report
    /// (e.g., "host", "storage", "service", "database").
    /// </para>
    /// <para>
    /// <b>Metrics filtering:</b> Use <see cref="Metrics"/> to control which metrics are included.
    /// </para>
    /// <para>
    /// <b>Pagination and bucketing:</b> <see cref="PageSize"/> and <see cref="MetricBucket"/> control
    /// how data is retrieved and aggregated from the server.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var reportConfig = new ReportConfiguration
    /// {
    ///     Collection = "Production",  // Specific collection
    ///     Timezone = "America/New_York",
    ///     EndTime = new EndTimeConfiguration { Now = true },
    ///     PastDays = 30,
    ///     PastHours = 0,
    ///     Types = new List&lt;string&gt; { "host", "storage", "database" },
    ///     Metrics = new MetricsConfiguration
    ///     {
    ///         AlertingOnOnly = false,  // Include all metrics
    ///         IncludeOnly = new List&lt;string&gt; { "cpu", "memory" },
    ///         ExcludeMetrics = new List&lt;string&gt;()
    ///     },
    ///     PageSize = 100,
    ///     MetricBucket = "observed_at:1h"  // Hourly aggregation
    /// };
    /// </code>
    /// </example>
    public sealed class ReportConfiguration
    {
        /// <summary>
        /// Gets or sets the collection name to query.
        /// Use <c>null</c>, empty string, or <c>"*"</c> to query all collections.
        /// </summary>
        /// <remarks>
        /// Default behavior (empty or null) queries all collections.
        /// </remarks>
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

        /// <summary>
        /// Gets or sets the timezone for report data interpretation.
        /// Should be a valid IANA timezone identifier (e.g., "America/Bogota", "Europe/London", "Asia/Tokyo").
        /// Default is "UTC".
        /// </summary>
        /// <remarks>
        /// The timezone is used to resolve dates and times in <see cref="EndTime"/> and to calculate
        /// UTC equivalents for the query sent to ArcGIS Monitor.
        /// </remarks>
        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the end time configuration for the report period.
        /// If <see cref="EndTimeConfiguration.Now"/> is true, uses the current time.
        /// Otherwise, uses the specified year/month/day/hour/minute/second.
        /// </summary>
        [JsonPropertyName("end_time")]
        public EndTimeConfiguration EndTime { get; set; } = new();

        /// <summary>
        /// Gets or sets the number of past days to include in the report.
        /// Combined with <see cref="PastHours"/>, defines how far back the report extends.
        /// </summary>
        [JsonPropertyName("past_days")]
        public int PastDays { get; set; }

        /// <summary>
        /// Gets or sets the number of past hours to include in the report (in addition to <see cref="PastDays"/>).
        /// </summary>
        [JsonPropertyName("past_hours")]
        public int PastHours { get; set; }

        /// <summary>
        /// Gets or sets the list of component types to include in the report.
        /// Must contain at least one type. Common types include "host", "storage", "service", "database".
        /// </summary>
        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = [];

        /// <summary>
        /// Gets or sets the metrics configuration controlling which metrics are included/excluded.
        /// </summary>
        [JsonPropertyName("metrics")]
        public MetricsConfiguration Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the page size for paginated queries to ArcGIS Monitor.
        /// Larger values reduce the number of requests but consume more memory.
        /// Default is 100.
        /// </summary>
        [JsonPropertyName("page_size")]
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the metric bucket specification for time-series aggregation.
        /// Format is "observed_at:&lt;duration&gt;" (e.g., "observed_at:15m" for 15-minute buckets).
        /// Default is "observed_at:15m".
        /// </summary>
        [JsonPropertyName("metric_bucket")]
        public string MetricBucket { get; set; } = "observed_at:15m";

        /// <summary>
        /// Gets or sets a value indicating whether to include metric time-series data.
        /// When true, time-series data for metrics is included in the report.
        /// Default is true.
        /// </summary>
        [JsonPropertyName("include_metric_time_series")]
        public bool IncludeMetricTimeSeries { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of metric IDs to include in time-series queries.
        /// Limits the scope of time-series data to prevent overwhelming large reports.
        /// Default is 5000. Set to null for unlimited.
        /// </summary>
        [JsonPropertyName("max_metric_ids_for_time_series")]
        public int? MaxMetricIdsForTimeSeries { get; set; } = 5000;
    }

    /// <summary>
    /// Configuration for specifying the end time of the report period.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class supports two modes for specifying the end time:
    /// </para>
    /// <para>
    /// <b>1. Using the current time:</b> Set <see cref="Now"/> to true.
    /// Other properties are ignored.
    /// Useful for "up-to-now" reports.
    /// </para>
    /// <para>
    /// <b>2. Specifying an explicit time:</b> Set <see cref="Now"/> to false and provide
    /// <see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>, <see cref="Hour"/>,
    /// <see cref="Minute"/>, and <see cref="Second"/>.
    /// Year, Month, and Day must be provided; Hour, Minute, Second default to 0.
    /// </para>
    /// <para>
    /// Times are interpreted in the timezone specified in <see cref="ReportConfiguration.Timezone"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use current time
    /// var endTime1 = new EndTimeConfiguration { Now = true };
    /// 
    /// // Use specific date and time
    /// var endTime2 = new EndTimeConfiguration 
    /// { 
    ///     Now = false,
    ///     Year = 2024,
    ///     Month = 12,
    ///     Day = 31,
    ///     Hour = 23,
    ///     Minute = 59,
    ///     Second = 59
    /// };
    /// </code>
    /// </example>
    public sealed class EndTimeConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use the current time as the end time.
        /// When true, <see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>,
        /// <see cref="Hour"/>, <see cref="Minute"/>, and <see cref="Second"/> are ignored.
        /// </summary>
        [JsonPropertyName("now")]
        public bool Now { get; set; }

        /// <summary>
        /// Gets or sets the year component of the end time (ignored if <see cref="Now"/> is true).
        /// Required if <see cref="Now"/> is false.
        /// </summary>
        [JsonPropertyName("year")]
        public int Year { get; set; }

        /// <summary>
        /// Gets or sets the month component of the end time (1-12, ignored if <see cref="Now"/> is true).
        /// Required if <see cref="Now"/> is false.
        /// </summary>
        [JsonPropertyName("month")]
        public int Month { get; set; }

        /// <summary>
        /// Gets or sets the day component of the end time (1-31, ignored if <see cref="Now"/> is true).
        /// Required if <see cref="Now"/> is false.
        /// </summary>
        [JsonPropertyName("day")]
        public int Day { get; set; }

        /// <summary>
        /// Gets or sets the hour component of the end time (0-23).
        /// Default is 0 (midnight). Ignored if <see cref="Now"/> is true.
        /// </summary>
        [JsonPropertyName("hour")]
        public int Hour { get; set; }

        /// <summary>
        /// Gets or sets the minute component of the end time (0-59).
        /// Default is 0. Ignored if <see cref="Now"/> is true.
        /// </summary>
        [JsonPropertyName("minute")]
        public int Minute { get; set; }

        /// <summary>
        /// Gets or sets the second component of the end time (0-59).
        /// Default is 0. Ignored if <see cref="Now"/> is true.
        /// </summary>
        [JsonPropertyName("second")]
        public int Second { get; set; }

        /// <summary>
        /// Resolves the end time to a <see cref="DateTimeOffset"/> in the specified timezone.
        /// </summary>
        /// <param name="timezone">The timezone to use for interpreting the time components.</param>
        /// <returns>A <see cref="DateTimeOffset"/> representing the end time in the specified timezone.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Now"/> is false and Year, Month, or Day are invalid or not provided.</exception>
        /// <example>
        /// <code>
        /// var endTime = new EndTimeConfiguration { Now = true };
        /// var bogotas TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        /// var resolved = endTime.Resolve(bogotas TimeZone);
        /// </code>
        /// </example>
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

    /// <summary>
    /// Configuration for filtering and controlling which metrics are included in the report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class allows fine-grained control over metric selection:
    /// </para>
    /// <para>
    /// <b>Alerting Filter:</b> Use <see cref="AlertingOnOnly"/> to restrict to metrics that have alerting enabled.
    /// Default is false (include all metrics regardless of alerting status).
    /// </para>
    /// <para>
    /// <b>Include Filter:</b> Use <see cref="IncludeOnly"/> to explicitly list metrics to include.
    /// If non-empty, only metrics in this list are included (case-insensitive matching).
    /// Empty list means no inclusion restrictions apply.
    /// </para>
    /// <para>
    /// <b>Exclude Filter:</b> Use <see cref="ExcludeMetrics"/> to exclude specific metrics from the report.
    /// If non-empty, metrics in this list are excluded (case-insensitive matching).
    /// Empty list means no metrics are excluded.
    /// </para>
    /// <para>
    /// <b>Precedence:</b> If both include and exclude lists are specified, a metric must be in the include list
    /// AND not in the exclude list to be included in the report.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Include all metrics
    /// var metrics1 = new MetricsConfiguration
    /// {
    ///     AlertingOnOnly = false,
    ///     IncludeOnly = new List&lt;string&gt;(),
    ///     ExcludeMetrics = new List&lt;string&gt;()
    /// };
    /// 
    /// // Only metrics with alerting enabled
    /// var metrics2 = new MetricsConfiguration
    /// {
    ///     AlertingOnOnly = true
    /// };
    /// 
    /// // Only include specific metrics
    /// var metrics3 = new MetricsConfiguration
    /// {
    ///     IncludeOnly = new List&lt;string&gt; { "cpu", "memory", "disk" }
    /// };
    /// 
    /// // Include most metrics except a few
    /// var metrics4 = new MetricsConfiguration
    /// {
    ///     ExcludeMetrics = new List&lt;string&gt; { "network_io", "swap" }
    /// };
    /// 
    /// // Include specific metrics but exclude some
    /// var metrics5 = new MetricsConfiguration
    /// {
    ///     IncludeOnly = new List&lt;string&gt; { "cpu", "memory", "disk", "network", "processes" },
    ///     ExcludeMetrics = new List&lt;string&gt; { "network_errors" }
    /// };
    /// </code>
    /// </example>
    public sealed class MetricsConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to include only metrics with alerting enabled.
        /// When true, metrics without alerting rules are excluded from the report.
        /// Default is false (include all metrics regardless of alerting status).
        /// </summary>
        [JsonPropertyName("alerting_on_only")]
        public bool AlertingOnOnly { get; set; }

        /// <summary>
        /// Gets or sets the list of metric names to explicitly include in the report.
        /// When non-empty, only metrics matching these names (case-insensitive) are included.
        /// Empty list means no inclusion restrictions (include all that match other filters).
        /// Whitespace-only entries are ignored.
        /// </summary>
        [JsonPropertyName("include_only")]
        public List<string> IncludeOnly { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of metric names to exclude from the report.
        /// Metrics matching these names (case-insensitive) are excluded.
        /// Empty list means no metrics are excluded.
        /// Whitespace-only entries are ignored.
        /// </summary>
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
