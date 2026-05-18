using ArchLens.SharedKernel.Application;
using ArchLens.Upload.Api.Controllers;
using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using ArchLens.Upload.Application.UseCases.Diagrams.Commands.Upload;
using ArchLens.Upload.Application.UseCases.Diagrams.Queries.GetStatus;
using ArchLens.Upload.Application.UseCases.Diagrams.Queries.List;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArchLens.SharedKernel.Domain;
using NSubstitute;

namespace ArchLens.Upload.Tests.Api.Controllers;

public class DiagramsControllerUnitTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IDiagramUploadRepository _diagramRepo = Substitute.For<IDiagramUploadRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly DiagramsController _controller;

    public DiagramsControllerUnitTests()
    {
        _controller = new DiagramsController(_sender, _diagramRepo, _unitOfWork, _fileStorage);
    }

    // ─── Upload ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_Success_ShouldReturnCreatedAtAction()
    {
        var response = new UploadDiagramResponse(Guid.NewGuid(), "test.png", "Received", DateTime.UtcNow);
        _sender.Send(Arg.Any<UploadDiagramCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var file = CreateFormFile("test.png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var result = await _controller.Upload(file, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(response);
        created.ActionName.Should().Be(nameof(DiagramsController.GetStatus));
    }

    [Fact]
    public async Task Upload_Failure_ShouldReturnBadRequest()
    {
        var error = new Error("Upload.Invalid", "Invalid file");
        _sender.Send(Arg.Any<UploadDiagramCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UploadDiagramResponse>(error));

        var file = CreateFormFile("test.exe", "application/octet-stream", new byte[] { 0x00 });

        var result = await _controller.Upload(file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_ShouldSanitizeFileName()
    {
        var response = new UploadDiagramResponse(Guid.NewGuid(), "test.png", "Received", DateTime.UtcNow);
        UploadDiagramCommand? capturedCommand = null;
        _sender.Send(Arg.Do<UploadDiagramCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var file = CreateFormFile("../../../etc/passwd.png", "image/png", new byte[] { 0x89 });

        await _controller.Upload(file, CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.FileName.Should().NotContain("..");
    }

    [Fact]
    public async Task Upload_WithSpecialCharsInFileName_ShouldSanitize()
    {
        var response = new UploadDiagramResponse(Guid.NewGuid(), "test.png", "Received", DateTime.UtcNow);
        UploadDiagramCommand? capturedCommand = null;
        _sender.Send(Arg.Do<UploadDiagramCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var file = CreateFormFile("my file (1).png", "image/png", new byte[] { 0x89 });

        await _controller.Upload(file, CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.FileName.Should().NotContain(" ");
    }

    [Fact]
    public async Task Upload_WithSpecialOnlyStemFileName_ShouldSanitizeToUnderscores()
    {
        var response = new UploadDiagramResponse(Guid.NewGuid(), "__.png", "Received", DateTime.UtcNow);
        UploadDiagramCommand? capturedCommand = null;
        _sender.Send(Arg.Do<UploadDiagramCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var file = CreateFormFile("!!.png", "image/png", new byte[] { 0x89 });

        await _controller.Upload(file, CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.FileName.Should().EndWith(".png");
        capturedCommand.FileName.Should().NotContain("!");
    }

    // ─── GetStatus ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_Success_ShouldReturnOk()
    {
        var id = Guid.NewGuid();
        var response = new DiagramStatusResponse(id, "test.png", "image/png", 1024, "Received", DateTime.UtcNow, null);
        _sender.Send(Arg.Any<GetDiagramStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.GetStatus(id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetStatus_NotFound_ShouldReturnNotFound()
    {
        _sender.Send(Arg.Any<GetDiagramStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<DiagramStatusResponse>(Error.NotFound));

        var result = await _controller.GetStatus(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetById ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Success_ShouldReturnOk()
    {
        var id = Guid.NewGuid();
        var response = new DiagramStatusResponse(id, "arch.png", "image/png", 2048, "Processing", DateTime.UtcNow, "user-1");
        _sender.Send(Arg.Any<GetDiagramStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetById_NotFound_ShouldReturnNotFound()
    {
        _sender.Send(Arg.Any<GetDiagramStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<DiagramStatusResponse>(Error.NotFound));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── List ────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ShouldReturnOk()
    {
        var items = new List<DiagramStatusResponse>();
        var pagedResponse = new PagedResponse<DiagramStatusResponse>(items, 1, 20, 0);
        _sender.Send(Arg.Any<ListDiagramsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(pagedResponse));

        var result = await _controller.List(1, 20, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Delete ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingDiagram_ShouldReturnNoContent()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024,
            FileHash.Create("test"u8.ToArray()), "bucket/path");

        _diagramRepo.GetByIdAsync(diagram.Id, Arg.Any<CancellationToken>())
            .Returns(diagram);

        var result = await _controller.Delete(diagram.Id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _diagramRepo.Received(1).DeleteAsync(diagram, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_NonExistingDiagram_ShouldReturnNotFound()
    {
        _diagramRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
        await _diagramRepo.DidNotReceive().DeleteAsync(Arg.Any<DiagramUpload>(), Arg.Any<CancellationToken>());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);
        file.Length.Returns(content.Length);
        file.OpenReadStream().Returns(new MemoryStream(content));
        return file;
    }
}
