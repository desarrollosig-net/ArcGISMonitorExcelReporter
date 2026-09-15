using ArcGISMonitorExcelReporterMcp.Security;

using Microsoft.AspNetCore.Http;

namespace ArcGISMonitorExcelReporterMcp.Tests
{
    public sealed class ApiKeyAuthMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WithoutApiKeyHeader_Returns401AndDoesNotCallNext()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var middleware = new ApiKeyAuthMiddleware(next, new HashSet<string> { "correct-key" });

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            Assert.False(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WithWrongApiKey_Returns401AndDoesNotCallNext()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var middleware = new ApiKeyAuthMiddleware(next, new HashSet<string> { "correct-key" });

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Headers[ApiKeyAuthMiddleware.HeaderName] = "wrong-key";

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            Assert.False(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WithCorrectApiKey_CallsNext()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var middleware = new ApiKeyAuthMiddleware(next, new HashSet<string> { "correct-key", "another-key" });

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Headers[ApiKeyAuthMiddleware.HeaderName] = "another-key";

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }
}
