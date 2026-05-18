using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Repositories.DiagramUploadRepositories;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Integration.Persistence;

public class DiagramUploadRepositoryTests : PersistenceTestBase
{
    private readonly DiagramUploadRepository _repository;

    public DiagramUploadRepositoryTests()
    {
        _repository = new DiagramUploadRepository(Context);
    }

    private static DiagramUpload CreateDiagram(string? userId = "user-1", string fileName = "diagram.png")
    {
        var hash = FileHash.Create(System.Text.Encoding.UTF8.GetBytes($"content-{Guid.NewGuid()}"));
        return DiagramUpload.Create(fileName, "image/png", 1024, hash, $"bucket/{Guid.NewGuid()}",  userId);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistDiagram()
    {
        var diagram = CreateDiagram();

        await _repository.AddAsync(diagram);
        await Context.SaveChangesAsync();

        var found = await Context.DiagramUploads.FindAsync(diagram.Id);
        found.Should().NotBeNull();
        found!.FileName.Should().Be("diagram.png");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnDiagram()
    {
        var diagram = CreateDiagram();
        await _repository.AddAsync(diagram);
        await Context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(diagram.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(diagram.Id);
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
        var diagram = CreateDiagram();
        await _repository.AddAsync(diagram);
        await Context.SaveChangesAsync();

        diagram.MarkAsProcessing();
        await _repository.UpdateAsync(diagram);
        await Context.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(diagram.Id);
        updated!.Status.Should().Be(DiagramStatus.Processing);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDiagram()
    {
        var diagram = CreateDiagram();
        await _repository.AddAsync(diagram);
        await Context.SaveChangesAsync();

        await _repository.DeleteAsync(diagram);
        await Context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(diagram.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByFileHashAsync_WhenExists_ShouldReturnDiagram()
    {
        var diagram = CreateDiagram();
        await _repository.AddAsync(diagram);
        await Context.SaveChangesAsync();

        var result = await _repository.GetByFileHashAsync(diagram.FileHash.Value, diagram.UserId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(diagram.Id);
    }

    [Fact]
    public async Task GetByFileHashAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _repository.GetByFileHashAsync("nonexistent-hash", "user-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByFileHashAsync_WhenHashMatchesButUserDiffers_ShouldReturnNull()
    {
        var diagram = CreateDiagram(userId: "user-A");
        await _repository.AddAsync(diagram);
        await Context.SaveChangesAsync();

        var result = await _repository.GetByFileHashAsync(diagram.FileHash.Value, "user-B");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage()
    {
        for (var i = 0; i < 5; i++)
        {
            await _repository.AddAsync(CreateDiagram());
        }
        await Context.SaveChangesAsync();

        var (items, totalCount) = await _repository.GetPagedAsync(skip: 1, take: 2);

        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithUserIdFilter_ShouldFilterResults()
    {
        await _repository.AddAsync(CreateDiagram(userId: "user-A"));
        await _repository.AddAsync(CreateDiagram(userId: "user-A"));
        await _repository.AddAsync(CreateDiagram(userId: "user-B"));
        await Context.SaveChangesAsync();

        var (items, totalCount) = await _repository.GetPagedAsync(skip: 0, take: 10, userId: "user-A");

        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(d => d.UserId.Should().Be("user-A"));
    }

    [Fact]
    public async Task GetPagedAsync_WithNoUserIdFilter_ShouldReturnAll()
    {
        await _repository.AddAsync(CreateDiagram(userId: "user-A"));
        await _repository.AddAsync(CreateDiagram(userId: "user-B"));
        await Context.SaveChangesAsync();

        var (items, totalCount) = await _repository.GetPagedAsync(skip: 0, take: 10);

        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ShouldReturnOnlyUserDiagrams()
    {
        await _repository.AddAsync(CreateDiagram(userId: "user-X"));
        await _repository.AddAsync(CreateDiagram(userId: "user-X"));
        await _repository.AddAsync(CreateDiagram(userId: "user-Y"));
        await Context.SaveChangesAsync();

        var result = await _repository.GetAllByUserIdAsync("user-X");

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.UserId.Should().Be("user-X"));
    }

    [Fact]
    public async Task GetAllByUserIdAsync_WhenNoMatches_ShouldReturnEmpty()
    {
        await _repository.AddAsync(CreateDiagram(userId: "user-Z"));
        await Context.SaveChangesAsync();

        var result = await _repository.GetAllByUserIdAsync("nonexistent-user");

        result.Should().BeEmpty();
    }
}
