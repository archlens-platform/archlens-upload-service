using System.Security.Claims;
using System.Text.Encodings.Web;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Reqnroll;

namespace ArchLens.Upload.Tests.BDD.Hooks;

[Binding]
public sealed class TestHooks
{
    private static BddWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__PostgreSQL",
            "Host=localhost;Database=upload_bdd_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("RabbitMQ__Host", "localhost");
        Environment.SetEnvironmentVariable("RabbitMQ__Username", "guest");
        Environment.SetEnvironmentVariable("RabbitMQ__Password", "guest");
        Environment.SetEnvironmentVariable("MinIO__Endpoint", "localhost:9000");
        Environment.SetEnvironmentVariable("MinIO__AccessKey", "minioadmin");
        Environment.SetEnvironmentVariable("MinIO__SecretKey", "minioadmin");
        Environment.SetEnvironmentVariable("MinIO__UseSSL", "false");
        Environment.SetEnvironmentVariable("Jwt__Key", "bdd-test-jwt-secret-key-minimum-32-characters!");

        _factory = new BddWebApplicationFactory();
        _client = _factory.CreateClient();

        // Ensure InMemory DB schema is created
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UploadDbContext>();
        db.Database.EnsureCreated();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        BddTestAuthHandler.SetAuthenticated();

        // Clean InMemory DB before each scenario
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UploadDbContext>();
            db.DiagramUploads.RemoveRange(db.DiagramUploads);
            db.AnalysisProcesses.RemoveRange(db.AnalysisProcesses);
            db.OutboxMessages.RemoveRange(db.OutboxMessages);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        scenarioContext.Set(_client, "HttpClient");
        scenarioContext.Set(_factory, "Factory");
    }
}

public sealed class BddWebApplicationFactory : WebApplicationFactory<ArchLens.Upload.Api.Program>
{
    public static IFileStorageService FileStorageMock { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext and EF provider registrations
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<UploadDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || (d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition().FullName?.Contains("EntityFrameworkCore") == true)
                    || d.ServiceType.FullName?.Contains("Npgsql") == true
                    || d.ServiceType.FullName?.Contains("EntityFrameworkCore.Relational") == true
                    || d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Add InMemory DB
            services.AddDbContext<UploadDbContext>(options =>
                options.UseInMemoryDatabase("UploadBddTests"));

            // Replace MassTransit with TestHarness
            var massTransitDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("MassTransit") == true)
                .ToList();
            foreach (var descriptor in massTransitDescriptors)
                services.Remove(descriptor);

            services.AddMassTransitTestHarness();

            // Remove HostedServices
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                .ToList();
            foreach (var descriptor in hostedServices)
                services.Remove(descriptor);

            // Replace MinIO storage with a mock
            var storageDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IFileStorageService));
            if (storageDescriptor is not null)
                services.Remove(storageDescriptor);

            // Remove Minio client registrations
            var minioDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Minio") == true)
                .ToList();
            foreach (var descriptor in minioDescriptors)
                services.Remove(descriptor);

            FileStorageMock = Substitute.For<IFileStorageService>();
            FileStorageMock.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo => $"diagrams/{Guid.NewGuid()}/{callInfo.ArgAt<string>(1)}");

            services.AddScoped<IFileStorageService>(_ => FileStorageMock);

            // Replace auth with test handler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, BddTestAuthHandler>("Test", _ => { });

            services.AddAuthorization();
        });
    }
}

public sealed class BddTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private static bool _isAuthenticated = true;

    public BddTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    public static void SetAuthenticated(bool authenticated = true) => _isAuthenticated = authenticated;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_isAuthenticated)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "BDD Test User"),
            new Claim(ClaimTypes.Email, "bdd@test.com"),
            new Claim(ClaimTypes.Role, "User"),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
