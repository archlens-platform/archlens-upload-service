using System.Net;
using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Api.ExceptionHandlers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ArchLens.Upload.Tests.Api.ExceptionHandlers;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _handler = new GlobalExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_ShouldReturn400()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var failures = new List<ValidationFailure>
        {
            new("FileName", "File name is required"),
            new("FileSize", "File size must be positive")
        };
        var exception = new ValidationException(failures);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_ShouldReturn422()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new TestDomainException("Test.Error", "Something went wrong in the domain");

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_ShouldReturn500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("Something unexpected");

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_ShouldLogError()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new Exception("Unexpected failure");

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_ShouldNotLogError()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new TestDomainException("Domain.Error", "Expected domain error");

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        _logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_AlwaysReturnsTrue()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var result1 = await _handler.TryHandleAsync(context, new Exception(), CancellationToken.None);
        context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result2 = await _handler.TryHandleAsync(context, new ValidationException("test"), CancellationToken.None);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    private sealed class TestDomainException : DomainException
    {
        public TestDomainException(string code, string message) : base(code, message) { }
    }
}
