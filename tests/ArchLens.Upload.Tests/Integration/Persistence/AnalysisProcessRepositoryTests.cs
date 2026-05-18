using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Repositories.AnalysisProcessRepositories;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Integration.Persistence;

public class AnalysisProcessRepositoryTests : PersistenceTestBase
{
    private readonly AnalysisProcessRepository _repository;

    public AnalysisProcessRepositoryTests()
    {
        _repository = new AnalysisProcessRepository(Context);
    }

    private async Task<DiagramUpload> SeedDiagramAsync()
    {
        var hash = FileHash.Create(System.Text.Encoding.UTF8.GetBytes($"content-{Guid.NewGuid()}"));
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024, hash, "bucket/path", "user-1");
        Context.DiagramUploads.Add(diagram);
        await Context.SaveChangesAsync();
        return diagram;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAnalysisProcess()
    {
        var diagram = await SeedDiagramAsync();
        var process = AnalysisProcess.Create(diagram.Id);

        await _repository.AddAsync(process);
        await Context.SaveChangesAsync();

        var found = await Context.AnalysisProcesses.FindAsync(process.Id);
        found.Should().NotBeNull();
        found!.DiagramUploadId.Should().Be(diagram.Id);
        found.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnProcess()
    {
        var diagram = await SeedDiagramAsync();
        var process = AnalysisProcess.Create(diagram.Id);
        await _repository.AddAsync(process);
        await Context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(process.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(process.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var diagram = await SeedDiagramAsync();
        var process = AnalysisProcess.Create(diagram.Id);
        await _repository.AddAsync(process);
        await Context.SaveChangesAsync();

        process.MarkStarted();
        await _repository.UpdateAsync(process);
        await Context.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(process.Id);
        updated!.Status.Should().Be("Processing");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProcess()
    {
        var diagram = await SeedDiagramAsync();
        var process = AnalysisProcess.Create(diagram.Id);
        await _repository.AddAsync(process);
        await Context.SaveChangesAsync();

        await _repository.DeleteAsync(process);
        await Context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(process.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByDiagramIdAsync_WhenExists_ShouldReturnProcess()
    {
        var diagram = await SeedDiagramAsync();
        var process = AnalysisProcess.Create(diagram.Id);
        await _repository.AddAsync(process);
        await Context.SaveChangesAsync();

        var result = await _repository.GetByDiagramIdAsync(diagram.Id);

        result.Should().NotBeNull();
        result!.DiagramUploadId.Should().Be(diagram.Id);
    }

    [Fact]
    public async Task GetByDiagramIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _repository.GetByDiagramIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}
