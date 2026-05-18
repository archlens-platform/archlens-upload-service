using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Exceptions;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Domain.Entities;

public class DiagramUploadAdditionalTests
{
    private static FileHash CreateHash() => FileHash.Create("test-content"u8.ToArray());

    [Fact]
    public void Create_WithUserId_ShouldSetUserId()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path", "user-123");

        diagram.UserId.Should().Be("user-123");
    }

    [Fact]
    public void Create_WithoutUserId_ShouldHaveNullUserId()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");

        diagram.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetCreatedAtToNow()
    {
        var before = DateTime.UtcNow;
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        var after = DateTime.UtcNow;

        diagram.CreatedAt.Should().BeOnOrAfter(before);
        diagram.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithJpeg_ShouldSucceed()
    {
        var diagram = DiagramUpload.Create("photo.jpeg", "image/jpeg", 2048, CreateHash(), "path");

        diagram.FileName.Should().Be("photo.jpeg");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var d1 = DiagramUpload.Create("a.png", "image/png", 100, CreateHash(), "p1");
        var d2 = DiagramUpload.Create("b.png", "image/png", 100, CreateHash(), "p2");

        d1.Id.Should().NotBe(d2.Id);
    }

    [Fact]
    public void MarkAsAnalyzed_FromReceived_ShouldThrow()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");

        var act = () => diagram.MarkAsAnalyzed();

        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void MarkAsError_FromProcessing_ShouldSucceed()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.MarkAsProcessing();

        diagram.MarkAsError();

        diagram.Status.Should().Be(DiagramStatus.Error);
    }

    [Fact]
    public void MarkAsError_FromAnalyzed_ShouldSucceed()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.MarkAsProcessing();
        diagram.MarkAsAnalyzed();

        diagram.MarkAsError();

        diagram.Status.Should().Be(DiagramStatus.Error);
    }

    [Fact]
    public void Create_WithMaxAllowedSize_ShouldSucceed()
    {
        var maxSize = 20 * 1024 * 1024;
        var diagram = DiagramUpload.Create("test.png", "image/png", maxSize, CreateHash(), "path");

        diagram.FileSize.Should().Be(maxSize);
    }

    [Fact]
    public void Create_WithExactlyOverMaxSize_ShouldThrow()
    {
        var overMax = 20 * 1024 * 1024 + 1;
        var act = () => DiagramUpload.Create("test.png", "image/png", overMax, CreateHash(), "path");

        act.Should().Throw<FileTooLargeException>();
    }

    [Fact]
    public void FullLifecycle_ShouldProduceCorrectDomainEvents()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.MarkAsProcessing();
        diagram.MarkAsAnalyzed();

        diagram.DomainEvents.Should().HaveCount(3);
    }
}
