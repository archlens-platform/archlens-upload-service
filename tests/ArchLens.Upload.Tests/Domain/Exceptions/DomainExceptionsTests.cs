using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Exceptions;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Domain.Exceptions;

public class DomainExceptionsTests
{
    [Fact]
    public void FileTooLargeException_ShouldContainCorrectCodeAndMessage()
    {
        var ex = new FileTooLargeException(30_000_000, 20_971_520);

        ex.Code.Should().Be("Upload.FileTooLarge");
        ex.Message.Should().Contain("30000000");
        ex.Message.Should().Contain("20971520");
    }

    [Fact]
    public void FileTooLargeException_ShouldBeDomainException()
    {
        var ex = new FileTooLargeException(100, 50);

        ex.Should().BeAssignableTo<DomainException>();
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void InvalidFileTypeException_ShouldContainCorrectCodeAndMessage()
    {
        var ex = new InvalidFileTypeException(".exe");

        ex.Code.Should().Be("Upload.InvalidFileType");
        ex.Message.Should().Contain(".exe");
        ex.Message.Should().Contain("not supported");
    }

    [Fact]
    public void InvalidFileTypeException_ShouldBeDomainException()
    {
        var ex = new InvalidFileTypeException(".zip");

        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidStatusTransitionException_ShouldContainCorrectCodeAndMessage()
    {
        var ex = new InvalidStatusTransitionException("Analyzed", "Processing");

        ex.Code.Should().Be("Upload.InvalidStatusTransition");
        ex.Message.Should().Contain("Analyzed");
        ex.Message.Should().Contain("Processing");
    }

    [Fact]
    public void InvalidStatusTransitionException_ShouldBeDomainException()
    {
        var ex = new InvalidStatusTransitionException("A", "B");

        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void DomainException_ShouldExposeCode()
    {
        var ex = new TestDomainException("MY_CODE", "my message");

        ex.Code.Should().Be("MY_CODE");
        ex.Message.Should().Be("my message");
    }

    [Fact]
    public void DomainException_WithInnerException_ShouldExposeInnerException()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = new TestDomainExceptionWithInner("CODE", "msg", inner);

        ex.InnerException.Should().BeSameAs(inner);
        ex.Code.Should().Be("CODE");
    }

    private sealed class TestDomainException : DomainException
    {
        public TestDomainException(string code, string message) : base(code, message) { }
    }

    private sealed class TestDomainExceptionWithInner : DomainException
    {
        public TestDomainExceptionWithInner(string code, string message, Exception inner)
            : base(code, message, inner) { }
    }
}
