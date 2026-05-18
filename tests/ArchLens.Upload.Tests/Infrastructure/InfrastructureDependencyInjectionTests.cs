using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Infrastructure;
using ArchLens.Upload.Infrastructure.Persistence;
using ArchLens.Upload.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLens.Upload.Tests.Infrastructure;

public class InfrastructureDependencyInjectionTests
{
    private static IConfiguration CreateValidConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test;Username=user;Password=pass",
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "minioadmin",
                ["MinIO:SecretKey"] = "minioadmin",
                ["MinIO:UseSSL"] = "false",
                ["MinIO:BucketName"] = "test-bucket",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

    [Fact]
    public void AddInfrastructure_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config = CreateValidConfiguration();

        var result = services.AddInfrastructure(config);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateValidConfiguration());

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IUnitOfWork) &&
            sd.ImplementationType == typeof(UnitOfWork));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterRepositories()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateValidConfiguration());

        services.Should().Contain(sd => sd.ServiceType == typeof(IDiagramUploadRepository));
        services.Should().Contain(sd => sd.ServiceType == typeof(IAnalysisProcessRepository));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterStorageService()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateValidConfiguration());

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IFileStorageService) &&
            sd.ImplementationType == typeof(MinioStorageService));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterHostedServices()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateValidConfiguration());

        var hostedServiceDescriptors = services
            .Where(sd => sd.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
            .ToList();

        hostedServiceDescriptors.Should().HaveCountGreaterOrEqualTo(2,
            "OutboxProcessor and DataRetentionProcessor should be registered as hosted services");
    }

    [Fact]
    public void AddInfrastructure_WithoutPostgresConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "key",
                ["MinIO:SecretKey"] = "secret",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PostgreSQL*");
    }

    [Fact]
    public void AddInfrastructure_WithoutMinioEndpoint_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["MinIO:AccessKey"] = "key",
                ["MinIO:SecretKey"] = "secret",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MinIO:Endpoint*");
    }

    [Fact]
    public void AddInfrastructure_WithoutMinioAccessKey_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:SecretKey"] = "secret",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MinIO:AccessKey*");
    }

    [Fact]
    public void AddInfrastructure_WithoutMinioSecretKey_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "key",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MinIO:SecretKey*");
    }

    [Fact]
    public void AddInfrastructure_WithoutRabbitMqHost_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "key",
                ["MinIO:SecretKey"] = "secret",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RabbitMQ:Host*");
    }

    [Fact]
    public void AddInfrastructure_WithoutRabbitMqUsername_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "key",
                ["MinIO:SecretKey"] = "secret",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RabbitMQ:Username*");
    }

    [Fact]
    public void AddInfrastructure_WithoutRabbitMqPassword_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "key",
                ["MinIO:SecretKey"] = "secret",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RabbitMQ:Password*");
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterDbContext()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateValidConfiguration());

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ArchLens.Upload.Infrastructure.Persistence.EFCore.Context.UploadDbContext));
    }
}
