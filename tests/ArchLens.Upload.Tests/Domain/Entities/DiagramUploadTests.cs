using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Events;
using ArchLens.Upload.Domain.Exceptions;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Domain.Entities;

public class DiagramUploadTests
{
    private static FileHash CreateHash() => FileHash.Create("test-content"u8.ToArray());

    [Theory]
    [InlineData("diagram.png", "image/png")]
    [InlineData("arch.jpg", "image/jpeg")]
    [InlineData("doc.pdf", "application/pdf")]
    [InlineData("screen.webp", "image/webp")]
    public void Create_WithValidFile_ShouldSucceed(string fileName, string contentType)
    {
        var hash = CreateHash();

        var diagram = DiagramUpload.Create(fileName, contentType, 1024, hash, "bucket/path");

        diagram.FileName.Should().Be(fileName);
        diagram.FileType.Should().Be(contentType);
        diagram.FileSize.Should().Be(1024);
        diagram.FileHash.Should().Be(hash);
        diagram.StoragePath.Should().Be("bucket/path");
        diagram.Status.Should().Be(DiagramStatus.Received);
        diagram.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldRaise_DiagramUploadCreatedEvent()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 512, CreateHash(), "bucket/key");

        diagram.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DiagramUploadCreatedEvent>();
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".txt")]
    [InlineData(".docx")]
    [InlineData(".zip")]
    public void Create_WithInvalidExtension_ShouldThrow(string extension)
    {
        var act = () => DiagramUpload.Create($"file{extension}", "application/octet-stream", 1024, CreateHash(), "path");

        act.Should().Throw<InvalidFileTypeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21_000_000)]
    public void Create_WithInvalidSize_ShouldThrow(long size)
    {
        var act = () => DiagramUpload.Create("test.png", "image/png", size, CreateHash(), "path");

        act.Should().Throw<FileTooLargeException>();
    }

    [Fact]
    public void MarkAsProcessing_FromReceived_ShouldSucceed()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.PopDomainEvents();

        diagram.MarkAsProcessing();

        diagram.Status.Should().Be(DiagramStatus.Processing);
        diagram.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DiagramStatusChangedEvent>();
    }

    [Fact]
    public void MarkAsProcessing_FromAnalyzed_ShouldThrow()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.MarkAsProcessing();
        diagram.MarkAsAnalyzed();

        var act = () => diagram.MarkAsProcessing();

        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void MarkAsAnalyzed_FromProcessing_ShouldSucceed()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");
        diagram.MarkAsProcessing();
        diagram.PopDomainEvents();

        diagram.MarkAsAnalyzed();

        diagram.Status.Should().Be(DiagramStatus.Analyzed);
    }

    [Fact]
    public void MarkAsError_FromAnyState_ShouldSucceed()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, CreateHash(), "path");

        diagram.MarkAsError();

        diagram.Status.Should().Be(DiagramStatus.Error);
    }

    [Fact]
    public void Create_ShouldSanitize_FileName()
    {
        var diagram = DiagramUpload.Create("path/to/diagram.png", "image/png", 1024, CreateHash(), "bucket/key");

        diagram.FileName.Should().Be("diagram.png");
    }
}
