using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using ArchLens.Upload.Infrastructure.Persistence;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Tests.Integration.Persistence;

public class UnitOfWorkTests : PersistenceTestBase
{
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _unitOfWork = new UnitOfWork(Context);
    }

    private static DiagramUpload CreateDiagram(string? userId = "user-1")
    {
        var hash = FileHash.Create(System.Text.Encoding.UTF8.GetBytes($"content-{Guid.NewGuid()}"));
        return DiagramUpload.Create("test.png", "image/png", 1024, hash, "bucket/path", userId);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldConvertDomainEventsToOutboxMessages()
    {
        var diagram = CreateDiagram();
        await Context.DiagramUploads.AddAsync(diagram);

        await _unitOfWork.SaveChangesAsync();

        var outboxMessages = await Context.OutboxMessages.ToListAsync();
        outboxMessages.Should().ContainSingle();
        outboxMessages[0].Type.Should().Contain("DiagramUploadedEvent");
        outboxMessages[0].Content.Should().NotBeNullOrEmpty();
        outboxMessages[0].ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldClearDomainEventsAfterConversion()
    {
        var diagram = CreateDiagram();
        await Context.DiagramUploads.AddAsync(diagram);

        await _unitOfWork.SaveChangesAsync();

        diagram.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoDomainEvents_ShouldNotCreateOutboxMessages()
    {
        var diagram = CreateDiagram();
        diagram.PopDomainEvents();
        await Context.DiagramUploads.AddAsync(diagram);

        await _unitOfWork.SaveChangesAsync();

        var outboxMessages = await Context.OutboxMessages.ToListAsync();
        outboxMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleAggregates_ShouldConvertAllEvents()
    {
        var diagram1 = CreateDiagram();
        var diagram2 = CreateDiagram(userId: "user-2");
        await Context.DiagramUploads.AddAsync(diagram1);
        await Context.DiagramUploads.AddAsync(diagram2);

        await _unitOfWork.SaveChangesAsync();

        var outboxMessages = await Context.OutboxMessages.ToListAsync();
        outboxMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveChangesAsync_OutboxMessage_ShouldContainSerializedEventData()
    {
        var diagram = CreateDiagram(userId: "user-test");
        await Context.DiagramUploads.AddAsync(diagram);

        await _unitOfWork.SaveChangesAsync();

        var outbox = await Context.OutboxMessages.FirstAsync();
        outbox.Content.Should().Contain(diagram.Id.ToString());
        outbox.Content.Should().Contain("test.png");
        outbox.Content.Should().Contain(diagram.FileHash.Value);
        outbox.Content.Should().Contain("bucket/path");
    }

    [Fact]
    public async Task SaveChangesAsync_StatusChangedEvent_ShouldNotCreateOutboxMessage()
    {
        var diagram = CreateDiagram();
        await Context.DiagramUploads.AddAsync(diagram);
        await _unitOfWork.SaveChangesAsync();

        var initialCount = await Context.OutboxMessages.CountAsync();

        diagram.MarkAsProcessing();
        Context.DiagramUploads.Update(diagram);
        await _unitOfWork.SaveChangesAsync();

        var finalCount = await Context.OutboxMessages.CountAsync();
        finalCount.Should().Be(initialCount, "DiagramStatusChangedEvent is not mapped to an integration event");
    }
}
