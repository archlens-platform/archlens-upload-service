using ArchLens.Upload.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ArchLens.Upload.Tests.Application.Behaviors;

public record LogTestRequest(string Data) : IRequest<string>;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<LogTestRequest, string>> _logger =
        Substitute.For<ILogger<LoggingBehavior<LogTestRequest, string>>>();

    private readonly LoggingBehavior<LogTestRequest, string> _behavior;

    public LoggingBehaviorTests()
    {
        _behavior = new LoggingBehavior<LogTestRequest, string>(_logger);
    }

    [Fact]
    public async Task Handle_ShouldCallNextAndReturnResult()
    {
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("response");
        };

        var result = await _behavior.Handle(new LogTestRequest("test"), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("response");
    }

    [Fact]
    public async Task Handle_ShouldLogBeforeAndAfterHandling()
    {
        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        await _behavior.Handle(new LogTestRequest("test"), next, CancellationToken.None);

        _logger.Received(2).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenNextThrows_ShouldPropagateException()
    {
        RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("boom");

        var act = async () => await _behavior.Handle(new LogTestRequest("test"), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task Handle_ShouldReturnSameResultAsNext()
    {
        var expected = "specific-result-value";
        RequestHandlerDelegate<string> next = () => Task.FromResult(expected);

        var result = await _behavior.Handle(new LogTestRequest("data"), next, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}
