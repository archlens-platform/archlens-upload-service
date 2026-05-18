using ArchLens.Upload.Application;
using ArchLens.Upload.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLens.Upload.Tests.Application;

public class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterMediatR()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddApplication();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ShouldRegisterValidationBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddApplication();

        var descriptors = services.Where(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>) &&
            s.ImplementationType == typeof(ValidationBehavior<,>));

        descriptors.Should().NotBeEmpty();
    }

    [Fact]
    public void AddApplication_ShouldRegisterLoggingBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddApplication();

        var descriptors = services.Where(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>) &&
            s.ImplementationType == typeof(LoggingBehavior<,>));

        descriptors.Should().NotBeEmpty();
    }

    [Fact]
    public void AddApplication_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }
}
