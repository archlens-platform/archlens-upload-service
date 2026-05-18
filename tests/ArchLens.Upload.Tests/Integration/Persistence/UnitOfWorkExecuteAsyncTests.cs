using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using ArchLens.Upload.Infrastructure.Persistence;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Tests.Integration.Persistence;

public class UnitOfWorkExecuteAsyncTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly UploadDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkExecuteAsyncTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<UploadDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new UploadDbContext(options);
        _context.Database.EnsureCreated();
        _unitOfWork = new UnitOfWork(_context);
    }

    private static DiagramUpload CreateDiagram(string? userId = "user-1")
    {
        var hash = FileHash.Create(System.Text.Encoding.UTF8.GetBytes($"content-{Guid.NewGuid()}"));
        return DiagramUpload.Create("test.png", "image/png", 1024, hash, "bucket/path", userId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCommitTransaction_WhenWorkSucceeds()
    {
        var diagram = CreateDiagram();

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await _context.DiagramUploads.AddAsync(diagram, ct);
        });

        var saved = await _context.DiagramUploads.FirstOrDefaultAsync(d => d.Id == diagram.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRollbackTransaction_WhenWorkThrows()
    {
        var diagram = CreateDiagram();

        var act = () => _unitOfWork.ExecuteAsync(async ct =>
        {
            await _context.DiagramUploads.AddAsync(diagram, ct);
            throw new InvalidOperationException("Simulated failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        using var verifyConnection = new SqliteConnection(_connection.ConnectionString);
        verifyConnection.Open();
        var verifyOptions = new DbContextOptionsBuilder<UploadDbContext>()
            .UseSqlite(_connection)
            .Options;

        var count = await _context.DiagramUploads.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_ShouldReturnResult_WhenWorkSucceeds()
    {
        var diagram = CreateDiagram();

        var result = await _unitOfWork.ExecuteAsync<Guid>(async ct =>
        {
            await _context.DiagramUploads.AddAsync(diagram, ct);
            return diagram.Id;
        });

        result.Should().Be(diagram.Id);
        var saved = await _context.DiagramUploads.FirstOrDefaultAsync(d => d.Id == diagram.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Generic_ShouldRollbackAndThrow_WhenWorkFails()
    {
        var act = () => _unitOfWork.ExecuteAsync<int>(async _ =>
        {
            await _context.DiagramUploads.AddAsync(CreateDiagram());
            throw new InvalidOperationException("Simulated failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await _context.DiagramUploads.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSaveChanges_BeforeCommit()
    {
        var diagram = CreateDiagram();

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await _context.DiagramUploads.AddAsync(diagram, ct);
        });

        _context.ChangeTracker.Clear();
        var fromDb = await _context.DiagramUploads.AsNoTracking().FirstOrDefaultAsync(d => d.Id == diagram.Id);
        fromDb.Should().NotBeNull();
        fromDb!.FileName.Should().Be("test.png");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSupportCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _unitOfWork.ExecuteAsync(async ct =>
        {
            ct.ThrowIfCancellationRequested();
        }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
