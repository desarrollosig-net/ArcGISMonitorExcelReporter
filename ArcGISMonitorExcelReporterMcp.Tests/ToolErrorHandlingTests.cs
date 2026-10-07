using ArcGISMonitorExcelReporterMcp.Tools;

using ModelContextProtocol;

namespace ArcGISMonitorExcelReporterMcp.Tests
{
    public sealed class ToolErrorHandlingTests
    {
        [Fact]
        public async Task RunAsync_WhenBodySucceeds_ReturnsResult()
        {
            var result = await ToolErrorHandling.RunAsync("test_tool", () => Task.FromResult("ok"));

            Assert.Equal("ok", result);
        }

        [Fact]
        public async Task RunAsync_WhenBodyThrowsHttpRequestException_ThrowsMcpExceptionWithServerDetails()
        {
            const string serverError = "Error HTTP 404 Not Found. Body: {\"success\":false,\"error\":{\"code\":\"E_ROUTE_NOT_FOUND\"}}";

            var exception = await Assert.ThrowsAsync<McpException>(
                () => ToolErrorHandling.RunAsync<string>("test_tool", () => throw new HttpRequestException(serverError)));

            Assert.Contains("HttpRequestException", exception.Message);
            Assert.Contains("E_ROUTE_NOT_FOUND", exception.Message);
            Assert.IsType<HttpRequestException>(exception.InnerException);
        }

        [Fact]
        public async Task RunAsync_WhenBodyThrowsMcpException_PropagatesItUnchanged()
        {
            var original = new McpException("already client-facing");

            var exception = await Assert.ThrowsAsync<McpException>(
                () => ToolErrorHandling.RunAsync<string>("test_tool", () => throw original));

            Assert.Same(original, exception);
        }

        [Fact]
        public async Task RunAsync_WhenBodyIsCancelled_PropagatesCancellation()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => ToolErrorHandling.RunAsync<string>("test_tool", () => throw new OperationCanceledException()));
        }

        [Fact]
        public void Describe_IncludesInnerExceptionMessages()
        {
            var exception = new HttpRequestException(
                "No such host is known. (monitor.invalid:443)",
                new System.Net.Sockets.SocketException(11001));

            var description = ToolErrorHandling.Describe(exception);

            Assert.StartsWith("HttpRequestException: No such host is known.", description);
            Assert.Contains(" ---> ", description);
        }

        [Theory]
        [InlineData("Body: {\"access_token\":\"abc123\",\"success\":true}", "abc123")]
        [InlineData("Authorization: Bearer abc.def.ghi", "abc.def.ghi")]
        [InlineData("token eyJhbGciOiJIUzI1NiJ9.eyJkYXRhIjp7fX0.sig-value_1 rejected", "eyJhbGciOiJIUzI1NiJ9")]
        [InlineData("password=s3cret&username=me", "s3cret")]
        public void Sanitize_RedactsCredentials(string message, string secret)
        {
            var sanitized = ToolErrorHandling.Sanitize(message);

            Assert.DoesNotContain(secret, sanitized);
            Assert.Contains("[REDACTED]", sanitized);
        }
    }
}
