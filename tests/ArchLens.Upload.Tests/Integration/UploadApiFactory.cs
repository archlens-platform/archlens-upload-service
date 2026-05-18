using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ArchLens.Upload.Tests.Integration;

public class UploadApiFactory : WebApplicationFactory<ArchLens.Upload.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "test",
                ["MinIO:SecretKey"] = "test",
                ["MinIO:UseSSL"] = "false",
                ["MinIO:Bucket"] = "test",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "test",
                ["RabbitMQ:Password"] = "test",
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<UploadDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<UploadDbContext>(options =>
                options.UseInMemoryDatabase("UploadTests_" + Guid.NewGuid()));

            // Remove real health checks that need actual DB connections
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var hc in healthCheckDescriptors)
                services.Remove(hc);
            services.AddHealthChecks();

            services.RemoveAll<IHostedService>();
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService, InMemoryStorageService>();

            services.AddMassTransitTestHarness();
        });
    }
}

internal sealed class InMemoryStorageService : IFileStorageService
{
    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        => Task.FromResult($"test-bucket/{Guid.NewGuid()}/{fileName}");

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(new MemoryStream([0x00]));

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
