using System.Reflection;
using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using ArchLens.Upload.Infrastructure.BackgroundServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ArchLens.Upload.Tests.Infrastructure.BackgroundServices;

public class DataRetentionProcessorTests
{
    private readonly IDiagramUploadRepository _uploadRepo = Substitute.For<IDiagramUploadRepository>();
    private readonly IAnalysisProcessRepository _analysisRepo = Substitute.For<IAnalysisProcessRepository>();
    private readonly IFileStorageService _storage = Substitute.For<IFileStorageService>();
    private readonly DataRetentionProcessor _processor;

    public DataRetentionProcessorTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDiagramUploadRepository)).Returns(_uploadRepo);
        serviceProvider.GetService(typeof(IAnalysisProcessRepository)).Returns(_analysisRepo);
        serviceProvider.GetService(typeof(IFileStorageService)).Returns(_storage);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _processor = new DataRetentionProcessor(
            scopeFactory,
            NullLogger<DataRetentionProcessor>.Instance);
    }

    private static DiagramUpload CreateExpiredUpload(DiagramStatus status, string storagePath = "bucket/old.png")
    {
        var hash = FileHash.Create(System.Text.Encoding.UTF8.GetBytes($"content-{Guid.NewGuid()}"));
        var upload = DiagramUpload.Create("old.png", "image/png", 1024, hash, storagePath, "user-1");

        SetPrivateProperty(upload, "CreatedAt", DateTime.UtcNow.AddDays(-100));
        SetPrivateProperty(upload, "Status", status);

        return upload;
    }

    private static DiagramUpload CreateRecentUpload(DiagramStatus status)
    {
        var hash = FileHash.Create(System.Text.Encoding.UTF8.GetBytes($"content-{Guid.NewGuid()}"));
        var upload = DiagramUpload.Create("new.png", "image/png", 1024, hash, "bucket/new.png", "user-1");
        SetPrivateProperty(upload, "Status", status);
        return upload;
    }

    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        var backingField = obj.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        backingField?.SetValue(obj, value);
    }

    private Task InvokeRunRetentionPolicyAsync(CancellationToken ct = default)
    {
        var method = typeof(DataRetentionProcessor)
            .GetMethod("RunRetentionPolicyAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        return (Task)method!.Invoke(_processor, [ct])!;
    }

    [Fact]
    public async Task RunRetentionPolicy_ShouldDeleteExpiredAnalyzedUploads()
    {
        var expiredUpload = CreateExpiredUpload(DiagramStatus.Analyzed, "bucket/expired.png");
        var analysis = AnalysisProcess.Create(expiredUpload.Id);

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { expiredUpload } as IReadOnlyList<DiagramUpload>, 1));

        _analysisRepo.GetByDiagramIdAsync(expiredUpload.Id, Arg.Any<CancellationToken>())
            .Returns(analysis);

        await InvokeRunRetentionPolicyAsync();

        await _storage.Received(1).DeleteAsync("bucket/expired.png", Arg.Any<CancellationToken>());
        await _analysisRepo.Received(1).DeleteAsync(analysis, Arg.Any<CancellationToken>());
        await _uploadRepo.Received(1).DeleteAsync(expiredUpload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_ShouldDeleteExpiredErrorUploads()
    {
        var expiredUpload = CreateExpiredUpload(DiagramStatus.Error, "bucket/error.png");

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { expiredUpload } as IReadOnlyList<DiagramUpload>, 1));

        _analysisRepo.GetByDiagramIdAsync(expiredUpload.Id, Arg.Any<CancellationToken>())
            .Returns((AnalysisProcess?)null);

        await InvokeRunRetentionPolicyAsync();

        await _storage.Received(1).DeleteAsync("bucket/error.png", Arg.Any<CancellationToken>());
        await _uploadRepo.Received(1).DeleteAsync(expiredUpload, Arg.Any<CancellationToken>());
        await _analysisRepo.DidNotReceive().DeleteAsync(Arg.Any<AnalysisProcess>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_ShouldSkipRecentUploads()
    {
        var recentUpload = CreateRecentUpload(DiagramStatus.Analyzed);

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { recentUpload } as IReadOnlyList<DiagramUpload>, 1));

        await InvokeRunRetentionPolicyAsync();

        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uploadRepo.DidNotReceive().DeleteAsync(Arg.Any<DiagramUpload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_ShouldSkipReceivedAndProcessingStatuses()
    {
        var receivedUpload = CreateExpiredUpload(DiagramStatus.Received, "bucket/received.png");
        var processingUpload = CreateExpiredUpload(DiagramStatus.Processing, "bucket/processing.png");

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { receivedUpload, processingUpload } as IReadOnlyList<DiagramUpload>, 2));

        await InvokeRunRetentionPolicyAsync();

        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uploadRepo.DidNotReceive().DeleteAsync(Arg.Any<DiagramUpload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_WhenStorageDeleteFails_ShouldContinueDeletingFromDb()
    {
        var expiredUpload = CreateExpiredUpload(DiagramStatus.Analyzed, "bucket/fail.png");

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { expiredUpload } as IReadOnlyList<DiagramUpload>, 1));

        _storage.DeleteAsync("bucket/fail.png", Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Storage unavailable"));

        _analysisRepo.GetByDiagramIdAsync(expiredUpload.Id, Arg.Any<CancellationToken>())
            .Returns((AnalysisProcess?)null);

        await InvokeRunRetentionPolicyAsync();

        await _uploadRepo.Received(1).DeleteAsync(expiredUpload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_WithNoUploads_ShouldNotDeleteAnything()
    {
        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload>() as IReadOnlyList<DiagramUpload>, 0));

        await InvokeRunRetentionPolicyAsync();

        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uploadRepo.DidNotReceive().DeleteAsync(Arg.Any<DiagramUpload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_ShouldPaginateThroughAllUploads()
    {
        var expiredUploads = Enumerable.Range(0, 100)
            .Select(_ => CreateExpiredUpload(DiagramStatus.Analyzed, $"bucket/{Guid.NewGuid()}.png"))
            .ToList();

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((expiredUploads as IReadOnlyList<DiagramUpload>, 150));

        var secondBatchUpload = CreateExpiredUpload(DiagramStatus.Analyzed, "bucket/last.png");
        _uploadRepo.GetPagedAsync(100, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { secondBatchUpload } as IReadOnlyList<DiagramUpload>, 150));

        _analysisRepo.GetByDiagramIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AnalysisProcess?)null);

        await InvokeRunRetentionPolicyAsync();

        await _uploadRepo.Received(1).GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>());
        await _uploadRepo.Received(1).GetPagedAsync(100, 100, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCancelGracefully_DuringInitialDelay()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await _processor.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await _processor.StopAsync(CancellationToken.None);

        await _uploadRepo.DidNotReceive().GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_WithUploadWithAssociatedAnalysis_ShouldDeleteBoth()
    {
        var expiredUpload = CreateExpiredUpload(DiagramStatus.Analyzed, "bucket/both.png");
        var analysis = AnalysisProcess.Create(expiredUpload.Id);

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { expiredUpload } as IReadOnlyList<DiagramUpload>, 1));

        _analysisRepo.GetByDiagramIdAsync(expiredUpload.Id, Arg.Any<CancellationToken>())
            .Returns(analysis);

        await InvokeRunRetentionPolicyAsync();

        await _analysisRepo.Received(1).DeleteAsync(analysis, Arg.Any<CancellationToken>());
        await _uploadRepo.Received(1).DeleteAsync(expiredUpload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunRetentionPolicy_WithUploadWithoutAnalysis_ShouldNotDeleteAnalysis()
    {
        var expiredUpload = CreateExpiredUpload(DiagramStatus.Error, "bucket/noanalysis.png");

        _uploadRepo.GetPagedAsync(0, 100, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { expiredUpload } as IReadOnlyList<DiagramUpload>, 1));

        _analysisRepo.GetByDiagramIdAsync(expiredUpload.Id, Arg.Any<CancellationToken>())
            .Returns((AnalysisProcess?)null);

        await InvokeRunRetentionPolicyAsync();

        await _analysisRepo.DidNotReceive().DeleteAsync(Arg.Any<AnalysisProcess>(), Arg.Any<CancellationToken>());
    }
}
