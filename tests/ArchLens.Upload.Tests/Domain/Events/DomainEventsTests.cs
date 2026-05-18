using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Events;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Domain.Events;

public class DomainEventsTests
{
    private static FileHash CreateHash() => FileHash.Create("test-content"u8.ToArray());

    // ─── DiagramUploadCreatedEvent ───────────────────────────────────────

    [Fact]
    public void DiagramUploadCreatedEvent_ShouldContainCorrectProperties()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "bucket/path", "user-1");

        var evt = diagram.DomainEvents
            .OfType<DiagramUploadCreatedEvent>()
            .Single();

        evt.DiagramId.Should().Be(diagram.Id);
        evt.FileName.Should().Be("test.png");
        evt.FileHash.Should().Be(diagram.FileHash.Value);
        evt.StoragePath.Should().Be("bucket/path");
        evt.UserId.Should().Be("user-1");
    }

    [Fact]
    public void DiagramUploadCreatedEvent_ShouldHaveEventIdAndOccurredAt()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 512, CreateHash(), "bucket/key");

        var evt = diagram.DomainEvents
            .OfType<DiagramUploadCreatedEvent>()
            .Single();

        evt.EventId.Should().NotBeEmpty();
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DiagramUploadCreatedEvent_WithNullUserId_ShouldHaveNullUserId()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 512, CreateHash(), "bucket/key");

        var evt = diagram.DomainEvents
            .OfType<DiagramUploadCreatedEvent>()
            .Single();

        evt.UserId.Should().BeNull();
    }

    // ─── DiagramStatusChangedEvent ───────────────────────────────────────

    [Fact]
    public void DiagramStatusChangedEvent_MarkAsProcessing_ShouldContainCorrectStatuses()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.PopDomainEvents(); // clear create event

        diagram.MarkAsProcessing();

        var evt = diagram.DomainEvents
            .OfType<DiagramStatusChangedEvent>()
            .Single();

        evt.DiagramId.Should().Be(diagram.Id);
        evt.OldStatus.Should().Be("Received");
        evt.NewStatus.Should().Be("Processing");
    }

    [Fact]
    public void DiagramStatusChangedEvent_MarkAsAnalyzed_ShouldContainCorrectStatuses()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.MarkAsProcessing();
        diagram.PopDomainEvents();

        diagram.MarkAsAnalyzed();

        var evt = diagram.DomainEvents
            .OfType<DiagramStatusChangedEvent>()
            .Single();

        evt.OldStatus.Should().Be("Processing");
        evt.NewStatus.Should().Be("Analyzed");
    }

    [Fact]
    public void DiagramStatusChangedEvent_MarkAsError_ShouldContainCorrectStatuses()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.PopDomainEvents();

        diagram.MarkAsError();

        var evt = diagram.DomainEvents
            .OfType<DiagramStatusChangedEvent>()
            .Single();

        evt.OldStatus.Should().Be("Received");
        evt.NewStatus.Should().Be("Error");
    }

    [Fact]
    public void DiagramStatusChangedEvent_ShouldHaveEventIdAndOccurredAt()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.PopDomainEvents();

        diagram.MarkAsProcessing();

        var evt = diagram.DomainEvents
            .OfType<DiagramStatusChangedEvent>()
            .Single();

        evt.EventId.Should().NotBeEmpty();
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ─── PopDomainEvents ─────────────────────────────────────────────────

    [Fact]
    public void PopDomainEvents_ShouldReturnEventsAndClearList()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");

        var events = diagram.PopDomainEvents();

        events.Should().HaveCount(1);
        diagram.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PopDomainEvents_CalledTwice_SecondCallShouldReturnEmpty()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");

        diagram.PopDomainEvents();
        var secondPop = diagram.PopDomainEvents();

        secondPop.Should().BeEmpty();
    }
}
