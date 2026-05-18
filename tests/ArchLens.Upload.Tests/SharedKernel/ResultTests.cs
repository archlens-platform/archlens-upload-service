using ArchLens.SharedKernel.Application;
using FluentAssertions;

namespace ArchLens.Upload.Tests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Success_ShouldHaveIsSuccessTrue()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldHaveIsFailureTrue()
    {
        var error = new Error("TEST", "Test error");
        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_Generic_ShouldContainValue()
    {
        var result = Result.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Failure_Generic_ShouldThrowOnValueAccess()
    {
        var result = Result.Failure<string>(Error.NotFound);

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccessResult()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Success_WithErrorNone_ShouldWork()
    {
        var result = Result.Success();

        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_WithNotFoundError_ShouldWork()
    {
        var result = Result.Failure(Error.NotFound);

        result.Error.Code.Should().Be("Error.NotFound");
        result.Error.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void Error_Predefined_ShouldHaveCorrectValues()
    {
        Error.None.Code.Should().BeEmpty();
        Error.NullValue.Code.Should().Be("Error.NullValue");
        Error.NotFound.Code.Should().Be("Error.NotFound");
        Error.Conflict.Code.Should().Be("Error.Conflict");
        Error.Validation.Code.Should().Be("Error.Validation");
    }

    [Fact]
    public void Error_Equality_ShouldWork()
    {
        var error1 = new Error("CODE", "Desc");
        var error2 = new Error("CODE", "Desc");

        error1.Should().Be(error2);
    }

    [Fact]
    public void Failure_Generic_AccessingValue_ShouldThrow()
    {
        var result = Result.Failure<int>(Error.Validation);

        var act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void Success_Generic_IsFailure_ShouldBeFalse()
    {
        var result = Result.Success(42);

        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_Error_ShouldNotBeNone()
    {
        var result = Result.Failure(Error.Conflict);

        result.Error.Should().NotBe(Error.None);
    }
}
