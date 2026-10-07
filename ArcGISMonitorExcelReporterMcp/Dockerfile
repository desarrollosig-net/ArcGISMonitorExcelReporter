FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, on just the project files, so dependency restore is cached across builds that
# only change source code.
COPY ArcGISMonitorExcelReporterMcp/ArcGISMonitorExcelReporterMcp.csproj ArcGISMonitorExcelReporterMcp/
COPY ArcGISMonitorExcelReporterLib/ArcGISMonitorExcelReporterLib.csproj ArcGISMonitorExcelReporterLib/
RUN dotnet restore ArcGISMonitorExcelReporterMcp/ArcGISMonitorExcelReporterMcp.csproj

COPY ArcGISMonitorExcelReporterMcp/ ArcGISMonitorExcelReporterMcp/
COPY ArcGISMonitorExcelReporterLib/ ArcGISMonitorExcelReporterLib/
RUN dotnet publish ArcGISMonitorExcelReporterMcp/ArcGISMonitorExcelReporterMcp.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Azure Container Apps routes external traffic to this port (set as --target-port on the app).
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# --http selects the streamable HTTP transport; see Program.cs. Requires ARCGIS_MCP_API_KEYS to
# be set at runtime (the server refuses to start in --http mode without it).
ENTRYPOINT ["dotnet", "ArcGISMonitorExcelReporterMcp.dll", "--http"]
