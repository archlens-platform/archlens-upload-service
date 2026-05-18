using ArchLens.Contracts.Events;
using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using ArchLens.Upload.Infrastructure.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ArchLens.Upload.Tests.Infrastructure.Consumers;

public class UserAccountDeletedConsumerTests
{
    private readonly IDiagramUploadRepository _uploadRepository = Substitute.For<IDiagramUploadRepository>();
    private readonly IAnalysisProcessRepository _analysisProcessRepository = Substitute.For<IAnalysisProcessRepository>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UserAccountDeletedConsumer _consumer;

    public UserAccountDeletedConsumerTests()
    {
        _unitOfWork.ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var work = callInfo.Arg<Func<CancellationToken, Task>>();
                return work(CancellationToken.None);
            });

        _consumer = new UserAccountDeletedConsumer(
            _uploadRepository,
            _analysisProcessRepository,
            _fileStorageService,
            _unitOfWork,
            NullLogger<UserAccountDeletedConsumer>.Instance);
    }

    private static ConsumeContext<UserAccountDeletedEvent> CreateContext(Guid userId)
    {
        var context = Substitute.For<ConsumeContext<UserAccountDeletedEvent>>();
        context.Message.Returns(new UserAccountDeletedEvent
        {
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private static DiagramUpload CreateUpload(string storagePath, string? userId = null)
    {
        return DiagramUpload.Create(
            "test.png",
            "image/png",
            1024,
            FileHash.Create("test-content"u8.ToArray()),
            storagePath,
            userId);
    }

    [Fact]
    public async Task Consume_WithUploads_ShouldDeleteFilesAndRecords()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var upload1 = CreateUpload("path/file1.png", userId.ToString());
        var upload2 = CreateUpload("path/file2.png", userId.ToString());
        var uploads = new List<DiagramUpload> { upload1, upload2 };

        _uploadRepository.GetAllByUserIdAsync(userId.ToString(), Arg.Any<CancellationToken>())
            .Returns(uploads);

        var analysis1 = AnalysisProcess.Create(upload1.Id);
        _analysisProcessRepository.GetByDiagramIdAsync(upload1.Id, Arg.Any<CancellationToken>())
            .Returns(analysis1);
        _analysisProcessRepository.GetByDiagramIdAsync(upload2.Id, Arg.Any<CancellationToken>())
            .Returns((AnalysisProcess?)null);

        var context = CreateContext(userId);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _fileStorageService.Received(1).DeleteAsync("path/file1.png", Arg.Any<CancellationToken>());
        await _fileStorageService.Received(1).DeleteAsync("path/file2.png", Arg.Any<CancellationToken>());
        await _analysisProcessRepository.Received(1).DeleteAsync(analysis1, Arg.Any<CancellationToken>());
        await _uploadRepository.Received(1).DeleteAsync(upload1, Arg.Any<CancellationToken>());
        await _uploadRepository.Received(1).DeleteAsync(upload2, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithNoUploads_ShouldReturnEarlyWithoutExecutingTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _uploadRepository.GetAllByUserIdAsync(userId.ToString(), Arg.Any<CancellationToken>())
            .Returns(new List<DiagramUpload>());

        var context = CreateContext(userId);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _unitOfWork.DidNotReceive().ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await _fileStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uploadRepository.DidNotReceive().DeleteAsync(Arg.Any<DiagramUpload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenFileDeleteFails_ShouldContinueWithOtherUploads()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var upload1 = CreateUpload("path/fail.png", userId.ToString());
        var upload2 = CreateUpload("path/success.png", userId.ToString());
        var uploads = new List<DiagramUpload> { upload1, upload2 };

        _uploadRepository.GetAllByUserIdAsync(userId.ToString(), Arg.Any<CancellationToken>())
            .Returns(uploads);

        _fileStorageService.DeleteAsync("path/fail.png", Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Storage unavailable"));
        _fileStorageService.DeleteAsync("path/success.png", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _analysisProcessRepository.GetByDiagramIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AnalysisProcess?)null);

        var context = CreateContext(userId);

        // Act
        await _consumer.Consume(context);

        // Assert - both uploads should still be deleted from DB even if file delete fails
        await _uploadRepository.Received(1).DeleteAsync(upload1, Arg.Any<CancellationToken>());
        await _uploadRepository.Received(1).DeleteAsync(upload2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithUploadHavingAnalysisProcess_ShouldDeleteAnalysisProcess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var upload = CreateUpload("path/file.png", userId.ToString());
        var uploads = new List<DiagramUpload> { upload };
        var analysisProcess = AnalysisProcess.Create(upload.Id);

        _uploadRepository.GetAllByUserIdAsync(userId.ToString(), Arg.Any<CancellationToken>())
            .Returns(uploads);
        _analysisProcessRepository.GetByDiagramIdAsync(upload.Id, Arg.Any<CancellationToken>())
            .Returns(analysisProcess);

        var context = CreateContext(userId);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _analysisProcessRepository.Received(1).DeleteAsync(analysisProcess, Arg.Any<CancellationToken>());
        await _uploadRepository.Received(1).DeleteAsync(upload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithUploadWithoutAnalysisProcess_ShouldNotDeleteAnalysisProcess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var upload = CreateUpload("path/file.png", userId.ToString());
        var uploads = new List<DiagramUpload> { upload };

        _uploadRepository.GetAllByUserIdAsync(userId.ToString(), Arg.Any<CancellationToken>())
            .Returns(uploads);
        _analysisProcessRepository.GetByDiagramIdAsync(upload.Id, Arg.Any<CancellationToken>())
            .Returns((AnalysisProcess?)null);

        var context = CreateContext(userId);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _analysisProcessRepository.DidNotReceive().DeleteAsync(Arg.Any<AnalysisProcess>(), Arg.Any<CancellationToken>());
        await _uploadRepository.Received(1).DeleteAsync(upload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_ShouldPassCorrectUserIdToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _uploadRepository.GetAllByUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DiagramUpload>());

        var context = CreateContext(userId);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _uploadRepository.Received(1).GetAllByUserIdAsync(userId.ToString(), Arg.Any<CancellationToken>());
    }
}
