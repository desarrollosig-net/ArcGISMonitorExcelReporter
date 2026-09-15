using ArcGISMonitorExcelReporterMcp.Security;
using ArcGISMonitorExcelReporterMcp.Tools;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ModelContextProtocol.AspNetCore;

using Serilog;

// This server exposes ArcGIS Monitor Excel Reporter capabilities (configuration validation,
// report summaries, and Excel generation) as MCP tools, reusing ArcGISMonitorExcelReporterLib.
//
// Transport is selected via a command line flag:
//   (no args)  -> stdio transport, for local MCP clients (Claude Desktop, Claude Code, etc.)
//   --http     -> streamable HTTP transport, for remote/network clients (e.g. multiple AI
//                 platforms sharing one deployment). Requires API key authentication; see
//                 RunHttpServerAsync.
//
// IMPORTANT: in stdio mode, stdout is reserved for MCP JSON-RPC messages. Logging must never
// write to stdout in that mode, or MCP clients will fail to parse the protocol stream.
var useHttp = args.Contains("--http");

var logsFolder = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsFolder);

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(Path.Combine(logsFolder, "mcp-server-.log"), rollingInterval: RollingInterval.Day);

if(useHttp)
{
    loggerConfiguration.WriteTo.Console();
}

Log.Logger = loggerConfiguration.CreateLogger();

try
{
    Log.Information("Starting ArcGIS Monitor Excel Reporter MCP server (transport: {Transport})", useHttp ? "http" : "stdio");

    if(useHttp)
    {
        await RunHttpServerAsync(args);
    }
    else
    {
        await RunStdioServerAsync(args);
    }
}
catch(Exception ex)
{
    Log.Fatal(ex, "MCP server terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static async Task RunStdioServerAsync(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);

    // Host.CreateApplicationBuilder registers a console logging provider by default, which writes
    // to stdout and would corrupt the MCP JSON-RPC stream. Route all framework/SDK logging to
    // stderr instead; our own tool logging goes through the file-only Serilog logger above.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = Microsoft.Extensions.Logging.LogLevel.Trace);

    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<MonitorReportTools>()
        .WithTools<ComponentMetricTools>();

    await builder.Build().RunAsync();
}

static async Task RunHttpServerAsync(string[] args)
{
    // Over HTTP the server may be shared by several remote clients (different AI platforms,
    // different users), so configPath is disabled: it would let any caller read arbitrary files
    // on the server's disk. Clients pass credentials inline via configJson instead.
    ConfigurationLoader.AllowConfigPath = false;

    var apiKeys = LoadApiKeys();

    var remainingArgs = args.Where(a => a != "--http").ToArray();
    var builder = WebApplication.CreateBuilder(remainingArgs);

    builder.Services.AddMcpServer()
        .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
        .WithTools<MonitorReportTools>()
        .WithTools<ComponentMetricTools>();

    var app = builder.Build();

    // Every client (Claude, ChatGPT, Gemini, or any other MCP-compatible caller) must present one
    // of the configured API keys via the X-Api-Key header. TLS termination (HTTPS) is expected to
    // be handled by whatever sits in front of this process (reverse proxy, load balancer, etc.).
    app.UseMiddleware<ApiKeyAuthMiddleware>(apiKeys);

    // Unauthenticated on purpose: cloud platform health probes (e.g. Azure App Service) need to
    // reach this without an API key. See ApiKeyAuthMiddleware.HealthCheckPath.
    app.MapGet(ApiKeyAuthMiddleware.HealthCheckPath, () => Results.Ok("healthy"));

    app.MapMcp("/mcp");

    Log.Information("HTTP transport authenticated with {Count} configured API key(s)", apiKeys.Count);

    await app.RunAsync();
}

/// <summary>
/// Loads the set of accepted API keys from the ARCGIS_MCP_API_KEYS environment variable
/// (comma-separated). Fails fast if none are configured, since running the HTTP transport
/// without authentication would expose it to any client that can reach the endpoint.
/// </summary>
static HashSet<string> LoadApiKeys()
{
    var raw = Environment.GetEnvironmentVariable("ARCGIS_MCP_API_KEYS");
    var keys = (raw ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToHashSet(StringComparer.Ordinal);

    if(keys.Count == 0)
    {
        throw new InvalidOperationException(
            "The HTTP transport requires at least one API key. Set the ARCGIS_MCP_API_KEYS " +
            "environment variable to a comma-separated list of keys (one per client) before " +
            "starting the server with --http.");
    }

    return keys;
}
