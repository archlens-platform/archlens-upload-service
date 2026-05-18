using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Application.UseCases.Diagrams.Commands.Upload;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ArchLens.Upload.Tests.Application.UseCases.Diagrams;

public class UploadDiagramHandlerTests
{
    private readonly IDiagramUploadRepository _repository = Substitute.For<IDiagramUploadRepository>();
    private readonly IFileStorageService _storage = Substitute.For<IFileStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UploadDiagramHandler _handler;

    public UploadDiagramHandlerTests()
    {
        _handler = new UploadDiagramHandler(
            _repository,
            _storage,
            _unitOfWork,
            NullLogger<UploadDiagramHandler>.Instance);
    }

    private static Stream CreateStream(string content = "fake-content") =>
        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task Handle_NewFile_ShouldUploadAndPersist()
    {
        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("bucket/diagrams/test.png");

        var command = new UploadDiagramCommand(CreateStream(), "test.png", "image/png", 1024);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("test.png");
        result.Value.Status.Should().Be("Received");
        await _repository.Received(1).AddAsync(Arg.Any<DiagramUpload>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateFile_ShouldReturnExistingWithoutReuploading()
    {
        var existing = DiagramUpload.Create("existing.png", "image/png", 512,
            FileHash.Create("fake-content"u8.ToArray()), "bucket/existing.png");

        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UploadDiagramCommand(CreateStream(), "test.png", "image/png", 512);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DiagramId.Should().Be(existing.Id);
        await _storage.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<DiagramUpload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUserId_ShouldPersistUserId()
    {
        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("bucket/path.png");

        var command = new UploadDiagramCommand(CreateStream(), "arch.png", "image/png", 2048, "user-123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<DiagramUpload>(d => d.UserId == "user-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutUserId_ShouldCreateAnonymousUpload()
    {
        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("bucket/path.png");

        var command = new UploadDiagramCommand(CreateStream(), "arch.png", "image/png", 2048);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<DiagramUpload>(d => d.UserId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDeriveStoragePath_FromStorageService()
    {
        const string expectedPath = "diagrams/2026/03/abc123.png";
        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedPath);

        var result = await _handler.Handle(
            new UploadDiagramCommand(CreateStream(), "d.png", "image/png", 1000),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<DiagramUpload>(d => d.StoragePath == expectedPath),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCalculateSameHash_ForSameContent()
    {
        var contentA = "identical-content";
        var contentB = "identical-content";

        string? capturedHashA = null;
        string? capturedHashB = null;

        _repository.GetByFileHashAsync(Arg.Do<string>(h => capturedHashA = h), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("path");

        await _handler.Handle(new UploadDiagramCommand(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentA)), "a.png", "image/png", 100), CancellationToken.None);

        _repository.GetByFileHashAsync(Arg.Do<string>(h => capturedHashB = h), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);

        await _handler.Handle(new UploadDiagramCommand(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentB)), "b.png", "image/png", 100), CancellationToken.None);

        capturedHashA.Should().Be(capturedHashB);
    }

    [Fact]
    public async Task Handle_ShouldCalculateDifferentHash_ForDifferentContent()
    {
        var hashes = new List<string>();

        _repository.GetByFileHashAsync(
            Arg.Do<string>(h => hashes.Add(h)), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("path");

        await _handler.Handle(new UploadDiagramCommand(
            new MemoryStream("content-alpha"u8.ToArray()), "a.png", "image/png", 50), CancellationToken.None);

        await _handler.Handle(new UploadDiagramCommand(
            new MemoryStream("content-beta"u8.ToArray()), "b.png", "image/png", 50), CancellationToken.None);

        hashes.Should().HaveCount(2);
        hashes[0].Should().NotBe(hashes[1]);
    }

    [Fact]
    public async Task Handle_NewFile_ShouldCallStorageWithCorrectFileName()
    {
        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("path");

        await _handler.Handle(
            new UploadDiagramCommand(CreateStream(), "microservices.pdf", "application/pdf", 5000),
            CancellationToken.None);

        await _storage.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            "microservices.pdf",
            "application/pdf",
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("diagram.jpg", "image/jpeg")]
    [InlineData("schema.webp", "image/webp")]
    [InlineData("arch.pdf", "application/pdf")]
    public async Task Handle_SupportedFormats_ShouldSucceed(string fileName, string contentType)
    {
        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("path");

        var result = await _handler.Handle(
            new UploadDiagramCommand(CreateStream(), fileName, contentType, 1024),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be(fileName);
    }

    [Fact]
    public async Task Handle_ResponseShouldContain_GeneratedId()
    {
        _repository.GetByFileHashAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("path");

        var result = await _handler.Handle(
            new UploadDiagramCommand(CreateStream(), "test.png", "image/png", 1024),
            CancellationToken.None);

        result.Value.DiagramId.Should().NotBeEmpty();
    }
}
