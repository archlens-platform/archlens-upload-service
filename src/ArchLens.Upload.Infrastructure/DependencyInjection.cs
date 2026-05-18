using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Infrastructure.BackgroundServices;
using ArchLens.Upload.Infrastructure.Consumers;
using ArchLens.Upload.Infrastructure.Persistence;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Repositories.AnalysisProcessRepositories;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Repositories.DiagramUploadRepositories;
using ArchLens.Upload.Infrastructure.Storage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace ArchLens.Upload.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddStorage(configuration);
        services.AddMessaging(configuration);

        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is required");

        services.AddDbContext<UploadDbContext>(options =>
            options.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(UploadDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDiagramUploadRepository, DiagramUploadRepository>();
        services.AddScoped<IAnalysisProcessRepository, AnalysisProcessRepository>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<DataRetentionProcessor>();
    }

    private static void AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var minioSection = configuration.GetRequiredSection("MinIO");
        var endpoint = minioSection["Endpoint"] ?? throw new InvalidOperationException("Configuration 'MinIO:Endpoint' is required");
        var accessKey = minioSection["AccessKey"] ?? throw new InvalidOperationException("Configuration 'MinIO:AccessKey' is required");
        var secretKey = minioSection["SecretKey"] ?? throw new InvalidOperationException("Configuration 'MinIO:SecretKey' is required");

        services.AddMinio(configureClient => configureClient
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(bool.Parse(minioSection["UseSSL"] ?? "false"))
            .Build());

        services.AddScoped<IFileStorageService, MinioStorageService>();
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitSection = configuration.GetRequiredSection("RabbitMQ");
        var host = rabbitSection["Host"] ?? throw new InvalidOperationException("Configuration 'RabbitMQ:Host' is required");
        var username = rabbitSection["Username"] ?? throw new InvalidOperationException("Configuration 'RabbitMQ:Username' is required");
        var password = rabbitSection["Password"] ?? throw new InvalidOperationException("Configuration 'RabbitMQ:Password' is required");

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumer<UserAccountDeletedConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
