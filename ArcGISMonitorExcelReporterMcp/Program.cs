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
//   --http     -> streamable HTTP transport, for remote/network clients
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
    var remainingArgs = args.Where(a => a != "--http").ToArray();
    var builder = WebApplication.CreateBuilder(remainingArgs);

    builder.Services.AddMcpServer()
        .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
        .WithTools<MonitorReportTools>()
        .WithTools<ComponentMetricTools>();

    var app = builder.Build();
    app.MapMcp("/mcp");

    await app.RunAsync();
}
