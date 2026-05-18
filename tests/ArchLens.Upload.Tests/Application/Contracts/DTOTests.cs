using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Application.Contracts;

public class DTOTests
{
    [Fact]
    public void UploadDiagramResponse_ShouldSetDefaultIsDuplicateFalse()
    {
        var response = new UploadDiagramResponse(
            Guid.NewGuid(), "test.png", "Received", DateTime.UtcNow);

        response.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public void UploadDiagramResponse_WithIsDuplicate_ShouldSetTrue()
    {
        var response = new UploadDiagramResponse(
            Guid.NewGuid(), "test.png", "Received", DateTime.UtcNow, IsDuplicate: true);

        response.IsDuplicate.Should().BeTrue();
    }

    [Fact]
    public void UploadDiagramResponse_ShouldExposeAllProperties()
    {
        var id = Guid.NewGuid();
        var created = DateTime.UtcNow;
        var response = new UploadDiagramResponse(id, "arch.png", "Processing", created, true);

        response.DiagramId.Should().Be(id);
        response.FileName.Should().Be("arch.png");
        response.Status.Should().Be("Processing");
        response.CreatedAt.Should().Be(created);
        response.IsDuplicate.Should().BeTrue();
    }

    [Fact]
    public void DiagramStatusResponse_ShouldExposeAllProperties()
    {
        var id = Guid.NewGuid();
        var created = DateTime.UtcNow;
        var response = new DiagramStatusResponse(id, "diagram.pdf", "application/pdf", 5000, "Analyzed", created, "user-42");

        response.DiagramId.Should().Be(id);
        response.FileName.Should().Be("diagram.pdf");
        response.FileType.Should().Be("application/pdf");
        response.FileSize.Should().Be(5000);
        response.Status.Should().Be("Analyzed");
        response.CreatedAt.Should().Be(created);
        response.UserId.Should().Be("user-42");
    }

    [Fact]
    public void DiagramStatusResponse_WithNullUserId_ShouldBeNull()
    {
        var response = new DiagramStatusResponse(Guid.NewGuid(), "x.png", "image/png", 1024, "Received", DateTime.UtcNow, null);

        response.UserId.Should().BeNull();
    }

    [Fact]
    public void UploadDiagramResponse_RecordEquality_ShouldWork()
    {
        var id = Guid.NewGuid();
        var created = DateTime.UtcNow;
        var r1 = new UploadDiagramResponse(id, "test.png", "Received", created);
        var r2 = new UploadDiagramResponse(id, "test.png", "Received", created);

        r1.Should().Be(r2);
    }

    [Fact]
    public void DiagramStatusResponse_RecordEquality_ShouldWork()
    {
        var id = Guid.NewGuid();
        var created = DateTime.UtcNow;
        var r1 = new DiagramStatusResponse(id, "test.png", "image/png", 1024, "Received", created, null);
        var r2 = new DiagramStatusResponse(id, "test.png", "image/png", 1024, "Received", created, null);

        r1.Should().Be(r2);
    }
}
