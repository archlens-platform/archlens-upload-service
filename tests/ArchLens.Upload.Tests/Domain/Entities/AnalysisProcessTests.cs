using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Domain.Entities;

public class AnalysisProcessTests
{
    [Fact]
    public void Create_ShouldSetInitialState()
    {
        var diagramId = Guid.NewGuid();

        var process = AnalysisProcess.Create(diagramId);

        process.Id.Should().NotBeEmpty();
        process.DiagramUploadId.Should().Be(diagramId);
        process.Status.Should().Be("Pending");
        process.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        process.CompletedAt.Should().BeNull();
        process.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_TwoProcesses_ShouldHaveUniqueIds()
    {
        var p1 = AnalysisProcess.Create(Guid.NewGuid());
        var p2 = AnalysisProcess.Create(Guid.NewGuid());

        p1.Id.Should().NotBe(p2.Id);
    }

    [Fact]
    public void MarkStarted_ShouldTransitionToProcessing()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());

        process.MarkStarted();

        process.Status.Should().Be("Processing");
        process.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void MarkCompleted_ShouldSetStatusAndCompletedAt()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());
        process.MarkStarted();

        process.MarkCompleted();

        process.Status.Should().Be("Completed");
        process.CompletedAt.Should().NotBeNull();
        process.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        process.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_ShouldSetStatusAndErrorMessage()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());
        process.MarkStarted();

        process.MarkFailed("AI provider timeout");

        process.Status.Should().Be("Failed");
        process.ErrorMessage.Should().Be("AI provider timeout");
        process.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_FromPending_ShouldSetStatusAndError()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());

        process.MarkFailed("Immediate failure");

        process.Status.Should().Be("Failed");
        process.ErrorMessage.Should().Be("Immediate failure");
    }

    [Fact]
    public void MarkCompleted_AfterFailed_ShouldOverrideStatus()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());
        process.MarkFailed("error");

        process.MarkCompleted();

        process.Status.Should().Be("Completed");
    }

    [Fact]
    public void MarkFailed_WithLongMessage_ShouldPreserveFullMessage()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());
        var longMessage = new string('x', 500);

        process.MarkFailed(longMessage);

        process.ErrorMessage.Should().Be(longMessage);
    }
}
