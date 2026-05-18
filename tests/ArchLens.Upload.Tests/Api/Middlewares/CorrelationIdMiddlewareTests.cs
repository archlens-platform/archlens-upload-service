using ArchLens.Upload.Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ArchLens.Upload.Tests.Api.Middlewares;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithExistingCorrelationId_ShouldPreserveIt()
    {
        var existingId = "my-custom-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = existingId;

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be(existingId);
        context.Items["X-Correlation-Id"]!.ToString().Should().Be(existingId);
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationId_ShouldGenerateOne()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Correlation-Id"].ToString().Should().NotBeNullOrWhiteSpace();
        context.Items["X-Correlation-Id"]!.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyCorrelationId_ShouldGenerateNew()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "";

        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var generated = context.Response.Headers["X-Correlation-Id"].ToString();
        generated.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(generated, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithWhitespaceCorrelationId_ShouldGenerateNew()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "   ";

        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var generated = context.Response.Headers["X-Correlation-Id"].ToString();
        generated.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;

        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GeneratedId_ShouldBeValidGuid()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }
}
