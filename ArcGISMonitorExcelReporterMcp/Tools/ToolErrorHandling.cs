using System.Text.RegularExpressions;

using ModelContextProtocol;

using Serilog;

namespace ArcGISMonitorExcelReporterMcp.Tools
{
    /// <summary>
    /// Surfaces the real cause of a tool failure to the MCP client. The MCP SDK replaces any
    /// exception other than <see cref="McpException"/> with a generic "An error occurred invoking
    /// '...'" message, which hides actionable details such as an HTTP 404 from a misconfigured
    /// server URL or a rejected login. Tools run their body through <see cref="RunAsync"/> so those
    /// details reach the client, with access tokens redacted.
    /// </summary>
    internal static partial class ToolErrorHandling
    {
        private const string Redacted = "[REDACTED]";

        /// <summary>
        /// Runs a tool body, converting unexpected exceptions into an <see cref="McpException"/>
        /// whose message describes the failure. Cancellation and existing <see cref="McpException"/>s
        /// propagate unchanged.
        /// </summary>
        /// <param name="toolName">The MCP tool name, used for logging.</param>
        /// <param name="body">The tool implementation.</param>
        /// <returns>The tool result produced by <paramref name="body"/>.</returns>
        public static async Task<string> RunAsync(string toolName, Func<Task<string>> body)
        {
            try
            {
                return await body().ConfigureAwait(false);
            }
            catch(Exception ex) when(ex is not OperationCanceledException and not McpException)
            {
                Log.Error(ex, "Tool {ToolName} failed", toolName);
                throw new McpException(Describe(ex), ex);
            }
        }

        /// <summary>
        /// Builds a client-facing description of an exception: its type and message, followed by the
        /// messages of any inner exceptions (e.g. the socket error behind an HttpRequestException).
        /// Bearer tokens and access_token values are redacted.
        /// </summary>
        /// <param name="exception">The exception to describe.</param>
        /// <returns>A single-line, sanitized description.</returns>
        public static string Describe(Exception exception)
        {
            var parts = new List<string> { $"{exception.GetType().Name}: {exception.Message}" };

            for(var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
            {
                if(!parts.Contains(inner.Message))
                {
                    parts.Add(inner.Message);
                }
            }

            return Sanitize(string.Join(" ---> ", parts));
        }

        /// <summary>
        /// Removes credentials that may appear in exception messages, such as a JWT echoed in an
        /// ArcGIS Monitor response body.
        /// </summary>
        /// <param name="message">The raw message.</param>
        /// <returns>The message with sensitive values replaced by <c>[REDACTED]</c>.</returns>
        public static string Sanitize(string message)
        {
            message = AccessTokenPattern().Replace(message, m => $"{m.Groups["prefix"].Value}{Redacted}");
            message = BearerPattern().Replace(message, $"Bearer {Redacted}");
            return JwtPattern().Replace(message, Redacted);
        }

        [GeneratedRegex(@"(?<prefix>""?(access_token|refresh_token|token|password)""?\s*[:=]\s*""?)[^""&,\s}]+", RegexOptions.IgnoreCase)]
        private static partial Regex AccessTokenPattern();

        [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-_.=]+", RegexOptions.IgnoreCase)]
        private static partial Regex BearerPattern();

        [GeneratedRegex(@"eyJ[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+")]
        private static partial Regex JwtPattern();
    }
}
