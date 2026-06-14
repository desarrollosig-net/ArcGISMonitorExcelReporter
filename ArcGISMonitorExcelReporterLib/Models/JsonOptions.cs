// Ignore Spelling: Json

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcGISMonitorExcelReporterLib.Models
{
    /// <summary>
    /// Provides centralized JSON serialization options for ArcGIS Monitor API communication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class defines a shared <see cref="JsonSerializerOptions"/> configuration used consistently
    /// across all JSON serialization and deserialization operations when communicating with the
    /// ArcGIS Monitor REST API.
    /// </para>
    /// <para>
    /// The configuration is optimized for:
    /// <list type="bullet">
    /// <item><description><b>Web API compatibility:</b> Uses <see cref="JsonSerializerDefaults.Web"/> as the base configuration</description></item>
    /// <item><description><b>Flexible naming:</b> Case-insensitive property name matching for robust API integration</description></item>
    /// <item><description><b>Minimal payload:</b> Null values are omitted from serialized output</description></item>
    /// <item><description><b>Compact format:</b> Non-indented JSON for efficient network transmission</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This shared configuration ensures consistency across all API requests and responses,
    /// preventing deserialization errors due to casing differences and reducing payload sizes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Using the shared JSON options for serialization:
    /// </para>
    /// <code>
    /// var request = new TokenRequest { Username = "admin", Password = "password" };
    /// var json = JsonSerializer.Serialize(request, MonitorJson.Options);
    /// // Output: {"username":"admin","password":"password"}
    /// </code>
    /// <para>
    /// Using the shared JSON options for deserialization:
    /// </para>
    /// <code>
    /// var json = @"{""access_token"":""abc123"",""expires_in"":3600}";
    /// var response = JsonSerializer.Deserialize&lt;TokenResponse&gt;(json, MonitorJson.Options);
    /// // Works even if property names have different casing
    /// </code>
    /// <para>
    /// Using with HttpClient:
    /// </para>
    /// <code>
    /// var content = JsonContent.Create(request, options: MonitorJson.Options);
    /// var response = await httpClient.PostAsync(endpoint, content);
    /// </code>
    /// </example>
    public static class MonitorJson
    {
        /// <summary>
        /// Gets the shared JSON serialization options configured for ArcGIS Monitor API communication.
        /// </summary>
        /// <value>
        /// A <see cref="JsonSerializerOptions"/> instance with the following settings:
        /// <list type="table">
        /// <listheader>
        /// <term>Property</term>
        /// <description>Value</description>
        /// <description>Purpose</description>
        /// </listheader>
        /// <item>
        /// <term><see cref="JsonSerializerDefaults.Web"/></term>
        /// <description>Base defaults</description>
        /// <description>Provides camelCase property naming and other web-friendly defaults</description>
        /// </item>
        /// <item>
        /// <term><see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/></term>
        /// <description><c>true</c></description>
        /// <description>Allows matching property names regardless of casing (e.g., "AccessToken" matches "access_token")</description>
        /// </item>
        /// <item>
        /// <term><see cref="JsonSerializerOptions.DefaultIgnoreCondition"/></term>
        /// <description><see cref="JsonIgnoreCondition.WhenWritingNull"/></description>
        /// <description>Omits null properties from serialized JSON to reduce payload size</description>
        /// </item>
        /// <item>
        /// <term><see cref="JsonSerializerOptions.WriteIndented"/></term>
        /// <description><c>false</c></description>
        /// <description>Produces compact JSON without whitespace for efficient network transmission</description>
        /// </item>
        /// </list>
        /// </value>
        /// <remarks>
        /// <para>
        /// This is a read-only static field that should be used throughout the library
        /// for all JSON operations to ensure consistency.
        /// </para>
        /// <para>
        /// <b>Thread Safety:</b> <see cref="JsonSerializerOptions"/> instances are thread-safe
        /// for read operations after configuration is complete, making this safe for concurrent use.
        /// </para>
        /// <para>
        /// <b>Performance:</b> Reusing the same options instance improves performance by
        /// avoiding repeated creation and configuration of serializer settings.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>
        /// Serialize an object:
        /// </para>
        /// <code>
        /// var query = new CollectionQueryRequest 
        /// { 
        ///     Where = "1=1", 
        ///     OutFields = "*",
        ///     ReturnGeometry = false
        /// };
        /// var json = JsonSerializer.Serialize(query, MonitorJson.Options);
        /// </code>
        /// <para>
        /// Deserialize a response:
        /// </para>
        /// <code>
        /// var responseBody = await httpResponse.Content.ReadAsStringAsync();
        /// var result = JsonSerializer.Deserialize&lt;QueryResponse&lt;CollectionFeature&gt;&gt;(
        ///     responseBody, 
        ///     MonitorJson.Options);
        /// </code>
        /// </example>
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }
}
