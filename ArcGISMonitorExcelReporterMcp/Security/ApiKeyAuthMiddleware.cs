using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Http;

namespace ArcGISMonitorExcelReporterMcp.Security
{
    /// <summary>
    /// Requires a valid API key on every request, via the <see cref="HeaderName"/> header.
    /// Applied only to the HTTP transport; stdio mode has no network exposure and needs no
    /// authentication, since the client and server share the same trust boundary there.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="validApiKeys">The set of API keys accepted as valid.</param>
    public sealed class ApiKeyAuthMiddleware(RequestDelegate next, IReadOnlySet<string> validApiKeys)
    {
        public const string HeaderName = "X-Api-Key";

        public async Task InvokeAsync(HttpContext context)
        {
            if(!context.Request.Headers.TryGetValue(HeaderName, out var providedKey) || !IsValidKey(providedKey.ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing or invalid API key.").ConfigureAwait(false);
                return;
            }

            await next(context).ConfigureAwait(false);
        }

        private bool IsValidKey(string providedKey)
        {
            if(string.IsNullOrEmpty(providedKey))
            {
                return false;
            }

            var providedBytes = Encoding.UTF8.GetBytes(providedKey);

            // Compare against every configured key with a fixed-time comparison so a caller can't
            // learn which key (or how much of it) matched from response timing.
            foreach(var validKey in validApiKeys)
            {
                if(CryptographicOperations.FixedTimeEquals(providedBytes, Encoding.UTF8.GetBytes(validKey)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
