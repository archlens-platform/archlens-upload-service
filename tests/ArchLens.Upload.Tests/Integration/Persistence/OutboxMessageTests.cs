using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Tests.Integration.Persistence;

public class OutboxMessageTests : PersistenceTestBase
{
    [Fact]
    public async Task OutboxMessage_ShouldPersistAllProperties()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "ArchLens.Contracts.Events.DiagramUploadedEvent, ArchLens.Contracts",
            Content = "{\"DiagramId\":\"00000000-0000-0000-0000-000000000001\"}",
            CreatedAt = DateTime.UtcNow
        };

        Context.OutboxMessages.Add(message);
        await Context.SaveChangesAsync();

        var found = await Context.OutboxMessages.FindAsync(message.Id);
        found.Should().NotBeNull();
        found!.Type.Should().Be(message.Type);
        found.Content.Should().Be(message.Content);
        found.ProcessedAt.Should().BeNull();
        found.Error.Should().BeNull();
        found.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task OutboxMessage_ShouldTrackProcessedState()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "TestEvent",
            Content = "{}",
            CreatedAt = DateTime.UtcNow
        };

        Context.OutboxMessages.Add(message);
        await Context.SaveChangesAsync();

        message.ProcessedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        var found = await Context.OutboxMessages.FindAsync(message.Id);
        found!.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OutboxMessage_ShouldTrackRetryCountAndError()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "TestEvent",
            Content = "{}",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 3,
            Error = "Connection timeout"
        };

        Context.OutboxMessages.Add(message);
        await Context.SaveChangesAsync();

        var found = await Context.OutboxMessages.FindAsync(message.Id);
        found!.RetryCount.Should().Be(3);
        found.Error.Should().Be("Connection timeout");
    }

    [Fact]
    public async Task OutboxMessage_UnprocessedQuery_ShouldReturnOnlyUnprocessed()
    {
        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "TestEvent",
            Content = "{}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = DateTime.UtcNow
        };

        var unprocessed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "TestEvent",
            Content = "{}",
            CreatedAt = DateTime.UtcNow
        };

        Context.OutboxMessages.AddRange(processed, unprocessed);
        await Context.SaveChangesAsync();

        var pending = await Context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .ToListAsync();

        pending.Should().ContainSingle();
        pending[0].Id.Should().Be(unprocessed.Id);
    }
}
