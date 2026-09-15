namespace ArcGISMonitorExcelReporterLib.Client
{
    /// <summary>
    /// An authenticated connection to ArcGIS Monitor, pairing the underlying HTTP client with a
    /// ready-to-use <see cref="ArcGisMonitorQueryService"/>. Dispose to release the HTTP client.
    /// </summary>
    /// <param name="client">The authenticated client backing this connection.</param>
    public sealed class ArcGisMonitorConnection(ArcGisMonitorClient client) : IDisposable
    {
        /// <summary>
        /// Query service bound to the authenticated client, ready for low-level queries
        /// (components, metrics, time series, etc.) without re-authenticating.
        /// </summary>
        public ArcGisMonitorQueryService Queries { get; } = new(client);

        public void Dispose() => client.Dispose();
    }
}
