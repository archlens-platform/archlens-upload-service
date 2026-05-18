using ArchLens.Upload.Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ArchLens.Upload.Tests.Api.Middlewares;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldSetXContentTypeOptions()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetXFrameOptions()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetXXSSProtection()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetReferrerPolicy()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetPermissionsPolicy()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["Permissions-Policy"].ToString().Should().Be("camera=(), microphone=(), geolocation=()");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCacheControl()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["Cache-Control"].ToString().Should().Be("no-store");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;

        var middleware = new SecurityHeadersMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetAllSixHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        context.Response.Headers.Should().ContainKey("X-Frame-Options");
        context.Response.Headers.Should().ContainKey("X-XSS-Protection");
        context.Response.Headers.Should().ContainKey("Referrer-Policy");
        context.Response.Headers.Should().ContainKey("Permissions-Policy");
        context.Response.Headers.Should().ContainKey("Cache-Control");
    }
}
