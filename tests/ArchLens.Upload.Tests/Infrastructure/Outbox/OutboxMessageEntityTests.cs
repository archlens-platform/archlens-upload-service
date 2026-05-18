using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Infrastructure.Outbox;

public class OutboxMessageEntityTests
{
    [Fact]
    public void OutboxMessage_ShouldInitialize_WithDefaultValues()
    {
        var message = new OutboxMessage();

        message.Type.Should().BeEmpty();
        message.Content.Should().BeEmpty();
        message.ProcessedAt.Should().BeNull();
        message.Error.Should().BeNull();
        message.RetryCount.Should().Be(0);
    }

    [Fact]
    public void OutboxMessage_ShouldAllowSettingAllProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var processedAt = DateTime.UtcNow.AddSeconds(5);

        var message = new OutboxMessage
        {
            Id = id,
            Type = "ArchLens.Contracts.Events.DiagramUploadedEvent, ArchLens.Contracts",
            Content = "{\"DiagramId\":\"00000000-0000-0000-0000-000000000001\"}",
            CreatedAt = createdAt,
            ProcessedAt = processedAt,
            Error = "Some error",
            RetryCount = 3
        };

        message.Id.Should().Be(id);
        message.Type.Should().Be("ArchLens.Contracts.Events.DiagramUploadedEvent, ArchLens.Contracts");
        message.Content.Should().Be("{\"DiagramId\":\"00000000-0000-0000-0000-000000000001\"}");
        message.CreatedAt.Should().Be(createdAt);
        message.ProcessedAt.Should().Be(processedAt);
        message.Error.Should().Be("Some error");
        message.RetryCount.Should().Be(3);
    }

    [Fact]
    public void OutboxMessage_IsProcessed_WhenProcessedAtIsNotNull()
    {
        var message = new OutboxMessage
        {
            ProcessedAt = DateTime.UtcNow
        };

        message.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void OutboxMessage_IsUnprocessed_WhenProcessedAtIsNull()
    {
        var message = new OutboxMessage();

        message.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void OutboxMessage_RetryCount_CanBeIncremented()
    {
        var message = new OutboxMessage { RetryCount = 2 };

        message.RetryCount++;

        message.RetryCount.Should().Be(3);
    }

    [Fact]
    public void OutboxMessage_Error_CanBeSetToNonNull()
    {
        var message = new OutboxMessage();

        message.Error = "Connection refused";

        message.Error.Should().Be("Connection refused");
    }

    [Fact]
    public void OutboxMessage_TwoInstances_WithSameValues_ShouldHaveEqualProperties()
    {
        var id = Guid.NewGuid();
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var m1 = new OutboxMessage { Id = id, Type = "TestType", CreatedAt = created };
        var m2 = new OutboxMessage { Id = id, Type = "TestType", CreatedAt = created };

        m1.Id.Should().Be(m2.Id);
        m1.Type.Should().Be(m2.Type);
        m1.CreatedAt.Should().Be(m2.CreatedAt);
    }
}
