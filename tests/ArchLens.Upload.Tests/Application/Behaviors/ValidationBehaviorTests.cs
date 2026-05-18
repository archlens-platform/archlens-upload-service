using ArchLens.Upload.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace ArchLens.Upload.Tests.Application.Behaviors;

public record TestRequest(string Value) : IRequest<string>;

public class AlwaysPassValidator : AbstractValidator<TestRequest> { }

public class RequireNonEmptyValidator : AbstractValidator<TestRequest>
{
    public RequireNonEmptyValidator()
    {
        RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required");
    }
}

public class RequireMinLengthValidator : AbstractValidator<TestRequest>
{
    public RequireMinLengthValidator()
    {
        RuleFor(x => x.Value).MinimumLength(5).WithMessage("Value too short");
    }
}

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        var result = await behavior.Handle(new TestRequest("ok"), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("result");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new AlwaysPassValidator()]);
        var nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShouldThrowValidationException()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new RequireNonEmptyValidator()]);

        RequestHandlerDelegate<string> next = () => Task.FromResult("should not reach");

        var act = async () => await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Value is required*");
    }

    [Fact]
    public async Task Handle_MultipleValidators_AllPass_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([
            new AlwaysPassValidator(),
            new AlwaysPassValidator()
        ]);
        var nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new TestRequest("good value"), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MultipleValidators_OneFails_ShouldThrow()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([
            new AlwaysPassValidator(),
            new RequireNonEmptyValidator()
        ]);

        RequestHandlerDelegate<string> next = () => Task.FromResult("no");

        var act = async () => await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_ShouldCollectAll()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([
            new RequireNonEmptyValidator(),
            new RequireMinLengthValidator()
        ]);

        RequestHandlerDelegate<string> next = () => Task.FromResult("no");

        var act = async () => await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
