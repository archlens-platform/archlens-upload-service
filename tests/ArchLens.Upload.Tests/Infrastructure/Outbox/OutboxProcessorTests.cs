using System.Text.Json;
using ArchLens.Contracts.Events;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ArchLens.Upload.Tests.Infrastructure.Outbox;

public class OutboxProcessorTests : IDisposable
{
    private readonly UploadDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly OutboxProcessor _processor;
    private readonly CancellationTokenSource _cts = new();

    public OutboxProcessorTests()
    {
        var options = new DbContextOptionsBuilder<UploadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UploadDbContext(options);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(UploadDbContext)).Returns(_dbContext);
        serviceProvider.GetService(typeof(IPublishEndpoint)).Returns(_publishEndpoint);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _processor = new OutboxProcessor(scopeFactory, NullLogger<OutboxProcessor>.Instance);
    }

    private OutboxMessage CreateOutboxMessage(
        string? type = null, string? content = null, DateTime? createdAt = null,
        DateTime? processedAt = null, int retryCount = 0)
    {
        var eventType = typeof(DiagramUploadedEvent).AssemblyQualifiedName!;
        var eventContent = JsonSerializer.Serialize(new DiagramUploadedEvent
        {
            DiagramId = Guid.NewGuid(),
            FileName = "test.png",
            FileHash = "abc123",
            StoragePath = "bucket/test.png",
            UserId = "user-1",
            Timestamp = DateTime.UtcNow
        });

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type ?? eventType,
            Content = content ?? eventContent,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ProcessedAt = processedAt,
            RetryCount = retryCount
        };
    }

    [Fact]
    public async Task ProcessOutboxMessages_ShouldPublishAndMarkAsProcessed()
    {
        var message = CreateOutboxMessage();
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        await RunSingleIteration();

        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.ProcessedAt.Should().NotBeNull();
        updated.Error.Should().BeNull();
        await _publishEndpoint.Received(1)
            .Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithUnknownType_ShouldSetError()
    {
        var message = CreateOutboxMessage(type: "Some.Unknown.Type, SomeAssembly");
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        await RunSingleIteration();

        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.ProcessedAt.Should().NotBeNull();
        updated.Error.Should().Contain("Unknown event type");
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithInvalidContent_ShouldSetErrorAndIncrementRetry()
    {
        var message = CreateOutboxMessage(content: "not-valid-json{{{");
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        await RunSingleIteration();

        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.RetryCount.Should().Be(1);
        updated.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessOutboxMessages_ShouldSkipAlreadyProcessedMessages()
    {
        var processed = CreateOutboxMessage(processedAt: DateTime.UtcNow);
        var unprocessed = CreateOutboxMessage();
        _dbContext.OutboxMessages.AddRange(processed, unprocessed);
        await _dbContext.SaveChangesAsync();

        await RunSingleIteration();

        await _publishEndpoint.Received(1)
            .Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_WhenPublishFails_ShouldIncrementRetryCount()
    {
        _publishEndpoint.Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new TimeoutException("Broker unreachable"));

        var message = CreateOutboxMessage();
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        await RunSingleIteration();

        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.RetryCount.Should().Be(1);
        updated.Error.Should().Contain("Broker unreachable");
        updated.ProcessedAt.Should().BeNull("retry count < 5 should not mark as processed");
    }

    [Fact]
    public async Task ProcessOutboxMessages_WhenRetryCountReachesFive_ShouldMarkAsProcessed()
    {
        _publishEndpoint.Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("Permanent failure"));

        var message = CreateOutboxMessage(retryCount: 4);
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        await RunSingleIteration();

        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.RetryCount.Should().Be(5);
        updated.ProcessedAt.Should().NotBeNull("retry count >= 5 should mark as processed");
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithNoMessages_ShouldNotCallPublish()
    {
        await RunSingleIteration();

        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_ShouldProcessInCreatedAtOrder()
    {
        var older = CreateOutboxMessage(createdAt: DateTime.UtcNow.AddMinutes(-10));
        var newer = CreateOutboxMessage(createdAt: DateTime.UtcNow);
        _dbContext.OutboxMessages.AddRange(newer, older);
        await _dbContext.SaveChangesAsync();

        var publishedTypes = new List<object>();
        _publishEndpoint.Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => publishedTypes.Add(ci.Arg<object>()));

        await RunSingleIteration();

        publishedTypes.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleExceptionsGracefully()
    {
        var failingScopeFactory = Substitute.For<IServiceScopeFactory>();
        failingScopeFactory.CreateScope().Returns<IServiceScope>(_ => throw new InvalidOperationException("DB down"));

        var processor = new OutboxProcessor(failingScopeFactory, NullLogger<OutboxProcessor>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await processor.StartAsync(CancellationToken.None);

        await Task.Delay(300);
        await processor.StopAsync(CancellationToken.None);
    }

    private async Task RunSingleIteration()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await _processor.StartAsync(CancellationToken.None);
        await Task.Delay(500, CancellationToken.None);
        await _processor.StopAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
